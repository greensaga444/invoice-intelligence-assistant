using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Official MOF merchant API client aligned to:
/// 電子發票營業人應用 API 規格(V1.9), method 5
/// (營業人個別化主題-營業人電子發票統計查詢).
/// </summary>
public sealed class MofOfficialHttpInvoiceApiClient : IMofOfficialInvoiceApiClient
{
    private const string ApiVersion = "1.0";
    private readonly HttpClient _httpClient;
    private readonly Uri _queryUri;

    public MofOfficialHttpInvoiceApiClient(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedBaseUri))
        {
            throw new ArgumentException("官方 API Base URL 必須為絕對網址。", nameof(baseUrl));
        }

        var path = parsedBaseUri.AbsoluteUri.TrimEnd('/');
        _queryUri = new Uri($"{path}/ods-main/ODS374I/query", UriKind.Absolute);
    }

    public async Task<OfficialInvoicePageResult> QueryInvoicesAsync(
        OfficialInvoicePageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Direction == InvoiceDirection.Purchase)
        {
            throw new InvalidOperationException(
                "目前官方模式依 V1.9 規格對齊『營業人電子發票統計查詢』(ODS374I)，僅支援銷項資料。請關閉進項下載。"
            );
        }

        if (string.IsNullOrWhiteSpace(request.OfficialApiAppId)
            || string.IsNullOrWhiteSpace(request.OfficialApiAccount)
            || string.IsNullOrWhiteSpace(request.OfficialApiPassword))
        {
            throw new InvalidOperationException(
                "官方 API 呼叫缺少必要認證參數。請確認 appId/account/password 均有設定。"
            );
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _queryUri);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Spec reference: V1.9 PDF (fileId=5161), method 5 request table.
        // Parameter names must match the document exactly: version/appID/ban/account/password/invoiceDateS/invoiceDateE.
        // Spec requires application/x-www-form-urlencoded payload.
        httpRequest.Content = new FormUrlEncodedContent(
        [
            KeyValuePair.Create("version", ApiVersion),
            KeyValuePair.Create("appID", request.OfficialApiAppId),
            KeyValuePair.Create("ban", request.TaxId),
            KeyValuePair.Create("account", request.OfficialApiAccount),
            KeyValuePair.Create("password", request.OfficialApiPassword),
            KeyValuePair.Create("invoiceDateS", request.DateFrom.ToString("yyyyMMdd", CultureInfo.InvariantCulture)),
            KeyValuePair.Create("invoiceDateE", request.DateTo.ToString("yyyyMMdd", CultureInfo.InvariantCulture))
        ]);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            int? statusCode = ex.StatusCode is null ? null : (int)ex.StatusCode.Value;
            throw new MofOfficialInvoiceApiException(
                message: $"呼叫官方 API 失敗: {ex.Message}",
                statusCode: statusCode,
                isTransient: statusCode is null || IsTransientStatusCode(statusCode.Value),
                innerException: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MofOfficialInvoiceApiException(
                message: "呼叫官方 API 逾時。",
                statusCode: (int)HttpStatusCode.RequestTimeout,
                isTransient: true,
                innerException: ex);
        }

        using var _ = response;

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await SafeReadResponseContentAsync(response, cancellationToken);
            var statusCode = (int)response.StatusCode;
            throw new MofOfficialInvoiceApiException(
                message: $"官方 API 回應失敗: {(int)response.StatusCode} {response.ReasonPhrase}。{errorBody}",
                statusCode: statusCode,
                isTransient: IsTransientStatusCode(statusCode));
        }

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var code = ReadString(root, "code") ?? string.Empty;
            var msg = ReadString(root, "msg") ?? "官方 API 未提供訊息。";
            if (!string.Equals(code, "200", StringComparison.OrdinalIgnoreCase))
            {
                throw new MofOfficialInvoiceApiException(
                    message: $"官方 API 業務錯誤: code={code}, msg={msg}",
                    statusCode: (int)response.StatusCode,
                    isTransient: false);
            }

            var items = ParseItems(root, request.TaxId, request.Direction);

            // Spec/PDF method 5 response does not define multi-page controls; treat result as single page.
            // ODS374I is not paged by page/pageSize; always return a single page.
            return new OfficialInvoicePageResult(items, HasMore: false);
        }
        catch (JsonException ex)
        {
            throw new MofOfficialInvoiceApiException(
                message: $"官方 API JSON 格式不符預期: {ex.Message}",
                statusCode: (int)response.StatusCode,
                isTransient: false,
                innerException: ex);
        }
    }

    private static IReadOnlyList<OfficialInvoiceRecord> ParseItems(
        JsonElement root,
        string taxId,
        InvoiceDirection direction)
    {
        var detailsElement = TryGetProperty(root, "details");
        if (detailsElement is null || detailsElement.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var records = new List<OfficialInvoiceRecord>();

        foreach (var item in detailsElement.Value.EnumerateArray())
        {
            var invoiceDate = ReadRequiredDate(item, "invoiceDate");
            var b2bAmount = ReadDecimal(item, "b2bCrtAvgAmount") ?? 0m;
            var b2cAmount = ReadDecimal(item, "b2cCrtAvgAmount") ?? 0m;
            var totalAmount = b2bAmount + b2cAmount;

            // ODS374I returns daily statistics only, so we synthesize a stable key-like invoice number.
            records.Add(new OfficialInvoiceRecord(
                InvoiceNumber: $"STAT-{invoiceDate:yyyyMMdd}",
                InvoiceDate: invoiceDate,
                SellerTaxId: taxId,
                BuyerTaxId: string.Empty,
                NetAmount: totalAmount,
                TaxAmount: 0m,
                TotalAmount: totalAmount,
                Direction: direction,
                Currency: "TWD"));
        }

        return records;
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var property = TryGetProperty(element, name);
            if (property is null)
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }

            if (property.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return property.Value.ToString();
            }
        }

        return null;
    }

    private static DateOnly ReadRequiredDate(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var raw = ReadString(element, name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
            {
                return dateOnly;
            }

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
            {
                return DateOnly.FromDateTime(dateTime);
            }
        }

        throw new JsonException($"缺少必要日期欄位: {string.Join("/", names)}");
    }

    private static decimal? ReadDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var property = TryGetProperty(element, name);
            if (property is null)
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.Number
                && property.Value.TryGetDecimal(out var decimalValue))
            {
                return decimalValue;
            }

            if (property.Value.ValueKind == JsonValueKind.String
                && decimal.TryParse(property.Value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDecimal))
            {
                return parsedDecimal;
            }
        }

        return null;
    }

    private static JsonElement? TryGetProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var exactProperty))
        {
            return exactProperty;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static bool IsTransientStatusCode(int statusCode)
    {
        return statusCode == 408 || statusCode == 429 || statusCode >= 500;
    }

    private static async Task<string> SafeReadResponseContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            return $" 回應內容: {body}";
        }
        catch
        {
            return string.Empty;
        }
    }
}
