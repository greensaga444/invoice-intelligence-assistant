using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InvoiceIntelligence.Core.Downloader;

public sealed class MofOfficialHttpInvoiceApiClient : IMofOfficialInvoiceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;

    public MofOfficialHttpInvoiceApiClient(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedBaseUri))
        {
            throw new ArgumentException("官方 API Base URL 必須為絕對網址。", nameof(baseUrl));
        }

        _baseUri = parsedBaseUri;
    }

    public async Task<OfficialInvoicePageResult> QueryInvoicesAsync(
        OfficialInvoicePageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.SessionToken))
        {
            throw new InvalidOperationException("MOF 工作階段無效，請重新驗證後再查詢。");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, BuildQueryUri(request));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.SessionToken);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Headers.TryAddWithoutValidation("X-Session-Token", request.SessionToken);

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
            var items = ParseItems(root, request.Direction);
            var hasMore = ResolveHasMore(root, request.PageSize, items.Count);

            return new OfficialInvoicePageResult(items, hasMore);
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

    private Uri BuildQueryUri(OfficialInvoicePageRequest request)
    {
        var directionText = request.Direction == InvoiceDirection.Sales ? "sales" : "purchase";

        var query = string.Join(
            "&",
            $"taxId={Uri.EscapeDataString(request.TaxId)}",
            $"dateFrom={request.DateFrom:yyyy-MM-dd}",
            $"dateTo={request.DateTo:yyyy-MM-dd}",
            $"direction={directionText}",
            $"page={request.PageNumber}",
            $"pageSize={request.PageSize}");

        var path = _baseUri.AbsoluteUri.TrimEnd('/');
        return new Uri($"{path}/v1/invoices?{query}", UriKind.Absolute);
    }

    private static IReadOnlyList<OfficialInvoiceRecord> ParseItems(JsonElement root, InvoiceDirection defaultDirection)
    {
        var itemsElement = TryGetProperty(root, "items")
            ?? TryGetProperty(root, "data")
            ?? TryGetProperty(root, "invoices")
            ?? TryGetProperty(root, "records");

        if (itemsElement is null || itemsElement.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<OfficialInvoiceRecord>();

        foreach (var item in itemsElement.Value.EnumerateArray())
        {
            result.Add(ParseInvoice(item, defaultDirection));
        }

        return result;
    }

    private static OfficialInvoiceRecord ParseInvoice(JsonElement item, InvoiceDirection defaultDirection)
    {
        var invoiceNumber = ReadRequiredString(item, "invoiceNumber", "invoiceNo", "invNum", "number");
        var invoiceDate = ReadRequiredDate(item, "invoiceDate", "invoiceDt", "date", "issueDate");
        var sellerTaxId = ReadString(item, "sellerTaxId", "sellerBan", "sellerId") ?? string.Empty;
        var buyerTaxId = ReadString(item, "buyerTaxId", "buyerBan", "buyerId") ?? string.Empty;
        var netAmount = ReadDecimal(item, "netAmount", "amount", "salesAmount") ?? 0m;
        var taxAmount = ReadDecimal(item, "taxAmount", "tax") ?? 0m;
        var totalAmount = ReadDecimal(item, "totalAmount", "grandTotal", "amountWithTax") ?? netAmount + taxAmount;
        var currency = ReadString(item, "currency", "currencyCode") ?? "TWD";
        var direction = ParseDirection(ReadString(item, "direction", "invoiceDirection"), defaultDirection);

        return new OfficialInvoiceRecord(
            InvoiceNumber: invoiceNumber,
            InvoiceDate: invoiceDate,
            SellerTaxId: sellerTaxId,
            BuyerTaxId: buyerTaxId,
            NetAmount: netAmount,
            TaxAmount: taxAmount,
            TotalAmount: totalAmount,
            Direction: direction,
            Currency: currency);
    }

    private static InvoiceDirection ParseDirection(string? value, InvoiceDirection fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "sales" or "sale" or "output" => InvoiceDirection.Sales,
            "purchase" or "input" => InvoiceDirection.Purchase,
            _ => fallback
        };
    }

    private static bool ResolveHasMore(JsonElement root, int pageSize, int itemCount)
    {
        var hasMore = ReadBoolean(root, "hasMore", "hasNext", "nextPage");
        if (hasMore is not null)
        {
            return hasMore.Value;
        }

        return itemCount >= pageSize;
    }

    private static bool? ReadBoolean(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var property = TryGetProperty(element, name);
            if (property is null)
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.Value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (property.Value.ValueKind == JsonValueKind.String
                && bool.TryParse(property.Value.GetString(), out var parsedBoolean))
            {
                return parsedBoolean;
            }
        }

        return null;
    }

    private static string ReadRequiredString(JsonElement element, params string[] names)
    {
        return ReadString(element, names)
            ?? throw new JsonException($"缺少必要欄位: {string.Join("/", names)}");
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
