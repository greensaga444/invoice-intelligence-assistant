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
    private static readonly string[] Raw1InvoiceNumberFieldAliases =
    [
        "發票號碼",
        "invoiceNo",
        "invoiceNumber",
        "invoiceNum"
    ];

    private static readonly string[] Raw1AllowanceNumberFieldAliases =
    [
        "折讓單號碼",
        "allowanceNo",
        "allowanceNumber"
    ];

    private static readonly string[] Raw1InvoiceDateFieldAliases =
    [
        "發票日期",
        "invoiceDate"
    ];

    private static readonly string[] Raw1AllowanceDateFieldAliases =
    [
        "折讓單日期",
        "allowanceDate"
    ];

    private static readonly string[] Raw1BuyerTaxIdFieldAliases =
    [
        "買方統一編號",
        "buyerTaxId",
        "buyerBan",
        "buyerIdentifier"
    ];

    private static readonly string[] Raw1SellerTaxIdFieldAliases =
    [
        "賣方統一編號",
        "sellerTaxId",
        "sellerBan",
        "sellerIdentifier"
    ];

    private static readonly string[] Raw1NetAmountFieldAliases =
    [
        "折讓金額(不含稅)",
        "allowanceAmount",
        "allowanceNetAmount",
        "netAmount",
        "itemNetAmount",
        "品項折讓金額(不含稅)"
    ];

    private static readonly string[] Raw1TaxAmountFieldAliases =
    [
        "折讓稅額",
        "allowanceTaxAmount",
        "taxAmount",
        "itemTaxAmount",
        "品項折讓稅額"
    ];

    private static readonly string[] Raw1TotalAmountFieldAliases =
    [
        "折讓總額",
        "totalAmount",
        "allowanceTotalAmount"
    ];

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

            var items = ParseItems(root, request.TaxId, request.Direction, request.DocumentType);

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
        InvoiceDirection direction,
        InvoiceDocumentType documentType)
    {
        var detailsElement = TryGetProperty(root, "details");
        if (detailsElement is null || detailsElement.Value.ValueKind != JsonValueKind.Array)
        {
            return BuildRecordsFromRaw1Items(ParseRaw1Items(root), taxId, direction, documentType);
        }

        var records = new List<OfficialInvoiceRecord>();

        foreach (var item in detailsElement.Value.EnumerateArray())
        {
            var raw1Items = ParseRaw1Items(item);
            var invoiceDate = ResolveInvoiceDate(item, raw1Items);
            var totalAmount = ResolveTotalAmount(item, direction, documentType, raw1Items);

            var invoiceNumber = ResolveRaw1Value(raw1Items, Raw1InvoiceNumberFieldAliases)
                ?? ResolveRaw1Value(raw1Items, Raw1AllowanceNumberFieldAliases)
                ?? BuildStatisticInvoiceNumber(invoiceDate, direction, documentType);

            var buyerTaxId = ResolveRaw1Value(raw1Items, Raw1BuyerTaxIdFieldAliases)
                ?? (direction == InvoiceDirection.Purchase ? taxId : string.Empty);

            var sellerTaxId = ResolveRaw1Value(raw1Items, Raw1SellerTaxIdFieldAliases)
                ?? (direction == InvoiceDirection.Sales ? taxId : string.Empty);

            var netAmount = ResolveRaw1Decimal(raw1Items, Raw1NetAmountFieldAliases) ?? totalAmount;
            var taxAmount = ResolveRaw1Decimal(raw1Items, Raw1TaxAmountFieldAliases) ?? 0m;
            var derivedTotalAmount = ResolveRaw1Decimal(raw1Items, Raw1TotalAmountFieldAliases) ?? (netAmount + taxAmount);

            // ODS374I returns daily statistics only, so we synthesize a stable key-like invoice number.
            records.Add(new OfficialInvoiceRecord(
                InvoiceNumber: invoiceNumber,
                InvoiceDate: invoiceDate,
                SellerTaxId: sellerTaxId,
                BuyerTaxId: buyerTaxId,
                NetAmount: netAmount,
                TaxAmount: taxAmount,
                TotalAmount: derivedTotalAmount,
                Direction: direction,
                DocumentType: documentType,
                Currency: "TWD",
                Raw1Items: raw1Items.Count > 0 ? raw1Items : null));
        }

        if (records.Count == 0)
        {
            return BuildRecordsFromRaw1Items(ParseRaw1Items(root), taxId, direction, documentType);
        }

        var topLevelRaw1Items = ParseRaw1Items(root);
        if (topLevelRaw1Items.Count > 0 && !HasAnyRaw1Items(records))
        {
            records[0] = records[0] with { Raw1Items = topLevelRaw1Items };
        }

        return records;
    }

    private static IReadOnlyList<OfficialInvoiceRecord> BuildRecordsFromRaw1Items(
        IReadOnlyList<IReadOnlyDictionary<string, string>> raw1Items,
        string taxId,
        InvoiceDirection direction,
        InvoiceDocumentType documentType)
    {
        if (raw1Items.Count == 0)
        {
            return [];
        }

        var records = new List<OfficialInvoiceRecord>(raw1Items.Count);

        foreach (var raw1Item in raw1Items)
        {
            var singleRaw1List = new List<IReadOnlyDictionary<string, string>> { raw1Item };

            var invoiceDate = ResolveRaw1Date(singleRaw1List, Raw1InvoiceDateFieldAliases)
                ?? ResolveRaw1Date(singleRaw1List, Raw1AllowanceDateFieldAliases)
                ?? DateOnly.FromDateTime(DateTime.Today);

            var invoiceNumber = ResolveRaw1Value(singleRaw1List, Raw1InvoiceNumberFieldAliases)
                ?? ResolveRaw1Value(singleRaw1List, Raw1AllowanceNumberFieldAliases)
                ?? BuildStatisticInvoiceNumber(invoiceDate, direction, documentType);

            var buyerTaxId = ResolveRaw1Value(singleRaw1List, Raw1BuyerTaxIdFieldAliases)
                ?? (direction == InvoiceDirection.Purchase ? taxId : string.Empty);

            var sellerTaxId = ResolveRaw1Value(singleRaw1List, Raw1SellerTaxIdFieldAliases)
                ?? (direction == InvoiceDirection.Sales ? taxId : string.Empty);

            var netAmount = ResolveRaw1Decimal(singleRaw1List, Raw1NetAmountFieldAliases) ?? 0m;
            var taxAmount = ResolveRaw1Decimal(singleRaw1List, Raw1TaxAmountFieldAliases) ?? 0m;
            var totalAmount = ResolveRaw1Decimal(singleRaw1List, Raw1TotalAmountFieldAliases) ?? (netAmount + taxAmount);

            records.Add(new OfficialInvoiceRecord(
                InvoiceNumber: invoiceNumber,
                InvoiceDate: invoiceDate,
                SellerTaxId: sellerTaxId,
                BuyerTaxId: buyerTaxId,
                NetAmount: netAmount,
                TaxAmount: taxAmount,
                TotalAmount: totalAmount,
                Direction: direction,
                DocumentType: documentType,
                Currency: "TWD",
                Raw1Items: singleRaw1List));
        }

        return records;
    }

    private static string BuildStatisticInvoiceNumber(
        DateOnly invoiceDate,
        InvoiceDirection direction,
        InvoiceDocumentType documentType)
    {
        var directionToken = direction == InvoiceDirection.Sales ? "S" : "P";
        var documentTypeToken = documentType == InvoiceDocumentType.Invoice ? "INV" : "ALW";

        return $"STAT-{invoiceDate:yyyyMMdd}-{directionToken}-{documentTypeToken}";
    }

    private static decimal ResolveTotalAmount(
        JsonElement item,
        InvoiceDirection direction,
        InvoiceDocumentType documentType,
        IReadOnlyList<IReadOnlyDictionary<string, string>> raw1Items)
    {
        // Prefer scope-specific fields when present.
        if (TryResolveScopeAmount(item, direction, documentType, out var scopedAmount))
        {
            return scopedAmount;
        }

        // Fallback to the documented ODS374I issuance fields so each selected scope still has a stable output row.
        var fallbackAmount = ResolveFallbackInvoiceAmount(item);
        if (fallbackAmount != 0m)
        {
            return fallbackAmount;
        }

        var raw1TotalAmount = ResolveRaw1Decimal(raw1Items, Raw1TotalAmountFieldAliases);
        if (raw1TotalAmount.HasValue)
        {
            return raw1TotalAmount.Value;
        }

        var raw1NetAmount = ResolveRaw1Decimal(raw1Items, Raw1NetAmountFieldAliases) ?? 0m;
        var raw1TaxAmount = ResolveRaw1Decimal(raw1Items, Raw1TaxAmountFieldAliases) ?? 0m;
        return raw1NetAmount + raw1TaxAmount;
    }

    private static DateOnly ResolveInvoiceDate(
        JsonElement item,
        IReadOnlyList<IReadOnlyDictionary<string, string>> raw1Items)
    {
        var raw1InvoiceDate = ResolveRaw1Date(raw1Items, Raw1InvoiceDateFieldAliases)
            ?? ResolveRaw1Date(raw1Items, Raw1AllowanceDateFieldAliases);
        if (raw1InvoiceDate.HasValue)
        {
            return raw1InvoiceDate.Value;
        }

        if (TryReadDate(item, out var invoiceDate, "invoiceDate"))
        {
            return invoiceDate;
        }

        throw new JsonException("缺少必要日期欄位: invoiceDate");
    }

    private static List<IReadOnlyDictionary<string, string>> ParseRaw1Items(JsonElement element)
    {
        var raw1Element = TryGetProperty(element, "raw1");
        if (raw1Element is null || raw1Element.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var raw1Items = new List<IReadOnlyDictionary<string, string>>();
        foreach (var raw1ItemElement in raw1Element.Value.EnumerateArray())
        {
            if (raw1ItemElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var raw1Item = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in raw1ItemElement.EnumerateObject())
            {
                raw1Item[property.Name] = ToRawString(property.Value);
            }

            if (raw1Item.Count > 0)
            {
                raw1Items.Add(raw1Item);
            }
        }

        return raw1Items;
    }

    private static string ToRawString(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
        {
            return value.ToString();
        }

        return string.Empty;
    }

    private static bool HasAnyRaw1Items(IReadOnlyList<OfficialInvoiceRecord> records)
    {
        foreach (var record in records)
        {
            if (record.Raw1Items is { Count: > 0 })
            {
                return true;
            }
        }

        return false;
    }

    private static string? ResolveRaw1Value(
        IReadOnlyList<IReadOnlyDictionary<string, string>> raw1Items,
        params string[] fieldNames)
    {
        foreach (var raw1Item in raw1Items)
        {
            foreach (var fieldName in fieldNames)
            {
                if (!raw1Item.TryGetValue(fieldName, out var rawValue)
                    || string.IsNullOrWhiteSpace(rawValue))
                {
                    continue;
                }

                return rawValue.Trim();
            }
        }

        return null;
    }

    private static decimal? ResolveRaw1Decimal(
        IReadOnlyList<IReadOnlyDictionary<string, string>> raw1Items,
        params string[] fieldNames)
    {
        var rawValue = ResolveRaw1Value(raw1Items, fieldNames);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedValue))
        {
            return parsedValue;
        }

        return null;
    }

    private static DateOnly? ResolveRaw1Date(
        IReadOnlyList<IReadOnlyDictionary<string, string>> raw1Items,
        params string[] fieldNames)
    {
        var rawValue = ResolveRaw1Value(raw1Items, fieldNames);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (DateOnly.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            return dateOnly;
        }

        if (DateOnly.TryParse(rawValue, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateOnly))
        {
            return dateOnly;
        }

        if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        if (DateTime.TryParse(rawValue, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return null;
    }

    private static bool TryReadDate(JsonElement element, out DateOnly dateOnly, params string[] names)
    {
        dateOnly = default;

        foreach (var name in names)
        {
            var raw = ReadString(element, name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOnly))
            {
                return true;
            }

            if (DateOnly.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateOnly))
            {
                return true;
            }

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
            {
                dateOnly = DateOnly.FromDateTime(dateTime);
                return true;
            }

            if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dateTime))
            {
                dateOnly = DateOnly.FromDateTime(dateTime);
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveScopeAmount(
        JsonElement item,
        InvoiceDirection direction,
        InvoiceDocumentType documentType,
        out decimal amount)
    {
        string[] candidateFields = (direction, documentType) switch
        {
            (InvoiceDirection.Sales, InvoiceDocumentType.Invoice) =>
            [
                "b2bCrtAvgAmount", "b2cCrtAvgAmount"
            ],
            (InvoiceDirection.Purchase, InvoiceDocumentType.Invoice) =>
            [
                "b2bPurAvgAmount", "b2cPurAvgAmount",
                "b2bBuyAvgAmount", "b2cBuyAvgAmount",
                "b2bInAvgAmount", "b2cInAvgAmount",
                "b2bRcvAvgAmount", "b2cRcvAvgAmount"
            ],
            (InvoiceDirection.Sales, InvoiceDocumentType.Allowance) =>
            [
                "b2bAllwAvgAmount", "b2cAllwAvgAmount",
                "b2bAllowAvgAmount", "b2cAllowAvgAmount",
                "b2bRtnAvgAmount", "b2cRtnAvgAmount",
                "b2bDedAvgAmount", "b2cDedAvgAmount"
            ],
            (InvoiceDirection.Purchase, InvoiceDocumentType.Allowance) =>
            [
                "b2bPurAllwAvgAmount", "b2cPurAllwAvgAmount",
                "b2bPurAllowAvgAmount", "b2cPurAllowAvgAmount",
                "b2bInAllwAvgAmount", "b2cInAllwAvgAmount",
                "b2bInAllowAvgAmount", "b2cInAllowAvgAmount",
                "b2bPurRtnAvgAmount", "b2cPurRtnAvgAmount"
            ],
            _ => []
        };

        amount = 0m;
        var hasAnyField = false;

        foreach (var field in candidateFields)
        {
            var value = ReadDecimal(item, field);
            if (!value.HasValue)
            {
                continue;
            }

            hasAnyField = true;
            amount += value.Value;
        }

        return hasAnyField;
    }

    private static decimal ResolveFallbackInvoiceAmount(JsonElement item)
    {
        var b2bAmount = ReadDecimal(item, "b2bCrtAvgAmount") ?? 0m;
        var b2cAmount = ReadDecimal(item, "b2cCrtAvgAmount") ?? 0m;
        return b2bAmount + b2cAmount;
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
