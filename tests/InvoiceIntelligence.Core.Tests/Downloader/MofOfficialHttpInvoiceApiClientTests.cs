using System.Net;
using System.Net.Http;
using System.Text;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class MofOfficialHttpInvoiceApiClientTests
{
    [Fact]
    public async Task QueryInvoicesAsync_ValidOfficialResponse_UsesOfficialEndpointAndMapsRecords()
    {
        var responseJson = """
            {
              "v": "1.0",
              "code": "200",
              "msg": "執行成功",
              "invoiceDateS": "20250907",
              "invoiceDateE": "20250910",
              "details": [
                {
                  "invoiceDate": "2025/09/07",
                  "b2bCrtAvgCount": "2",
                  "b2bCrtAvgAmount": "100",
                  "b2cCrtAvgCount": "1",
                  "b2cCrtAvgAmount": "50"
                }
              ]
            }
            """;

        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var client = new MofOfficialHttpInvoiceApiClient(httpClient, "https://www-vc.einvoice.nat.gov.tw");

        var request = new OfficialInvoicePageRequest(
            SessionToken: "unused-in-v1.9",
            TaxId: "12345678",
            DateFrom: new DateOnly(2025, 9, 7),
            DateTo: new DateOnly(2025, 9, 10),
            Direction: InvoiceDirection.Sales,
            PageNumber: 1,
            PageSize: 200,
            OfficialApiAppId: "app-id-001",
            OfficialApiAccount: "merchant-account",
            OfficialApiPassword: "merchant-password");

        var result = await client.QueryInvoicesAsync(request);

        Assert.Single(result.Items);
        Assert.False(result.HasMore);

        var record = result.Items[0];
        Assert.Equal("STAT-20250907-S-INV", record.InvoiceNumber);
        Assert.Equal(new DateOnly(2025, 9, 7), record.InvoiceDate);
        Assert.Equal("12345678", record.SellerTaxId);
        Assert.Equal(150m, record.TotalAmount);
        Assert.Equal(150m, record.NetAmount);
        Assert.Equal(0m, record.TaxAmount);
        Assert.Equal(InvoiceDirection.Sales, record.Direction);
        Assert.Equal(InvoiceDocumentType.Invoice, record.DocumentType);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(
            "https://www-vc.einvoice.nat.gov.tw/ods-main/ODS374I/query",
            handler.LastRequest.RequestUri?.ToString());

        var formBody = handler.LastRequestBody;
        Assert.NotNull(formBody);
        Assert.Contains("version=1.0", formBody);
        Assert.Contains("appID=app-id-001", formBody);
        Assert.Contains("ban=12345678", formBody);
        Assert.Contains("account=merchant-account", formBody);
        Assert.Contains("password=merchant-password", formBody);
        Assert.Contains("invoiceDateS=20250907", formBody);
        Assert.Contains("invoiceDateE=20250910", formBody);
    }

    [Fact]
    public async Task QueryInvoicesAsync_BusinessCodeNot200_ThrowsMofOfficialInvoiceApiException()
    {
        var responseJson = """
            {
              "v": "1.0",
              "code": "903",
              "msg": "參數錯誤",
              "details": []
            }
            """;

        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");

        var request = new OfficialInvoicePageRequest(
            SessionToken: "unused-in-v1.9",
            TaxId: "12345678",
            DateFrom: new DateOnly(2025, 9, 7),
            DateTo: new DateOnly(2025, 9, 10),
            Direction: InvoiceDirection.Sales,
            PageNumber: 1,
            PageSize: 200,
            OfficialApiAppId: "app-id-001",
            OfficialApiAccount: "merchant-account",
            OfficialApiPassword: "merchant-password");

        await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => client.QueryInvoicesAsync(request));
    }

    [Fact]
    public async Task QueryInvoicesAsync_MissingOfficialCredential_ThrowsInvalidOperationException()
    {
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");

        var request = new OfficialInvoicePageRequest(
            SessionToken: "unused-in-v1.9",
            TaxId: "12345678",
            DateFrom: new DateOnly(2025, 9, 7),
            DateTo: new DateOnly(2025, 9, 10),
            Direction: InvoiceDirection.Sales,
            PageNumber: 1,
            PageSize: 200);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.QueryInvoicesAsync(request));
    }

    [Fact]
    public async Task QueryInvoicesAsync_AllowanceFieldsPresent_MapsAllowanceAmountAndType()
    {
        var responseJson = """
            {
              "v": "1.0",
              "code": "200",
              "msg": "執行成功",
              "invoiceDateS": "20250907",
              "invoiceDateE": "20250910",
              "details": [
                {
                  "invoiceDate": "2025/09/07",
                  "b2bAllwAvgAmount": "80",
                  "b2cAllwAvgAmount": "20"
                }
              ]
            }
            """;

        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var client = new MofOfficialHttpInvoiceApiClient(httpClient, "https://www-vc.einvoice.nat.gov.tw");

        var request = new OfficialInvoicePageRequest(
            SessionToken: "unused-in-v1.9",
            TaxId: "12345678",
            DateFrom: new DateOnly(2025, 9, 7),
            DateTo: new DateOnly(2025, 9, 10),
            Direction: InvoiceDirection.Sales,
            PageNumber: 1,
            PageSize: 200,
            DocumentType: InvoiceDocumentType.Allowance,
            OfficialApiAppId: "app-id-001",
            OfficialApiAccount: "merchant-account",
            OfficialApiPassword: "merchant-password");

        var result = await client.QueryInvoicesAsync(request);

        Assert.Single(result.Items);
        var record = result.Items[0];
        Assert.Equal("STAT-20250907-S-ALW", record.InvoiceNumber);
        Assert.Equal(100m, record.TotalAmount);
        Assert.Equal(InvoiceDocumentType.Allowance, record.DocumentType);
    }

    [Fact]
    public async Task QueryInvoicesAsync_Raw1ItemsPresent_MapsAllowanceDetailRow()
    {
        var responseJson = """
                        {
                            "v": "1.0",
                            "code": "200",
                            "msg": "執行成功",
                            "details": [
                                {
                                    "invoiceDate": "2026/08/07",
                                    "raw1": [
                                        {
                                            "allowanceNo": "2026080718487658",
                                            "formatCode": "23",
                                            "allowanceStatus": "折讓已確認",
                                            "allowanceType": "進項",
                                            "invoiceNo": "DN24749905",
                                            "invoiceDate": "2026-08-06",
                                            "buyerTaxId": "54274765",
                                            "buyerName": "立璽科技股份有限公司",
                                            "sellerTaxId": "83513625",
                                            "sellerName": "客遊天下玩樂股份有限公司",
                                            "sendDate": "2026-08-07",
                                            "itemName": "綜合服務",
                                            "itemNetAmount": "357",
                                            "itemTaxAmount": "18",
                                            "netAmount": "357",
                                            "taxAmount": "18",
                                            "allowanceDate": "2026-08-07",
                                            "lastUpdatedTime": "2026-08-07 19:05:43",
                                            "migType": "G0401",
                                            "senderTaxId": "97025978",
                                            "senderName": "綠界科技股份有限公司"
                                        }
                                    ]
                                }
                            ]
                        }
                        """;

        var handler = new RecordingHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");

        var request = new OfficialInvoicePageRequest(
                SessionToken: "unused-in-v1.9",
                TaxId: "54274765",
                DateFrom: new DateOnly(2026, 8, 1),
                DateTo: new DateOnly(2026, 8, 31),
                Direction: InvoiceDirection.Purchase,
                PageNumber: 1,
                PageSize: 200,
                DocumentType: InvoiceDocumentType.Allowance,
                OfficialApiAppId: "app-id-001",
                OfficialApiAccount: "merchant-account",
                OfficialApiPassword: "merchant-password");

        var result = await client.QueryInvoicesAsync(request);

        Assert.Single(result.Items);

        var record = result.Items[0];
        Assert.Equal("DN24749905", record.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 8, 6), record.InvoiceDate);
        Assert.Equal("83513625", record.SellerTaxId);
        Assert.Equal("54274765", record.BuyerTaxId);
        Assert.Equal(357m, record.NetAmount);
        Assert.Equal(18m, record.TaxAmount);
        Assert.Equal(375m, record.TotalAmount);
        Assert.Equal(InvoiceDocumentType.Allowance, record.DocumentType);
        Assert.NotNull(record.Raw1Items);
        Assert.Single(record.Raw1Items!);
        Assert.Equal("2026080718487658", record.Raw1Items![0]["allowanceNo"]);
    }

    [Fact]
    public async Task QueryInvoicesAsync_HttpRequestExceptionWithStatusCode_ThrowsTransientDomainException()
    {
        var handler = new ThrowingHttpMessageHandler(_ =>
            throw new HttpRequestException("gateway failed", null, HttpStatusCode.BadGateway));

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest();

        var exception = await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => client.QueryInvoicesAsync(request));

        Assert.Equal(502, exception.StatusCode);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public async Task QueryInvoicesAsync_HttpRequestExceptionWithoutStatusCode_ThrowsTransientDomainException()
    {
        var handler = new ThrowingHttpMessageHandler(_ => throw new HttpRequestException("network unavailable"));

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest();

        var exception = await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => client.QueryInvoicesAsync(request));

        Assert.Null(exception.StatusCode);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public async Task QueryInvoicesAsync_Timeout_ThrowsRequestTimeoutDomainException()
    {
        var handler = new ThrowingHttpMessageHandler(_ => throw new TaskCanceledException("timeout"));

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest();

        var exception = await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => client.QueryInvoicesAsync(request));

        Assert.Equal(408, exception.StatusCode);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public async Task QueryInvoicesAsync_NonSuccessStatusWithBody_ThrowsWithResponseBodyAndNonTransientFlag()
    {
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"bad request\"}", Encoding.UTF8, "application/json")
            });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest();

        var exception = await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => client.QueryInvoicesAsync(request));

        Assert.Equal(400, exception.StatusCode);
        Assert.False(exception.IsTransient);
        Assert.Contains("bad request", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueryInvoicesAsync_NonSuccessStatus429_ThrowsTransientException()
    {
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
            });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest();

        var exception = await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => client.QueryInvoicesAsync(request));

        Assert.Equal(429, exception.StatusCode);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public async Task QueryInvoicesAsync_InvalidJson_ThrowsDomainException()
    {
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{not-valid-json}", Encoding.UTF8, "application/json")
            });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest();

        var exception = await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => client.QueryInvoicesAsync(request));

        Assert.Equal(200, exception.StatusCode);
        Assert.False(exception.IsTransient);
        Assert.Contains("JSON", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueryInvoicesAsync_MissingInvoiceDateAndRaw1Date_ThrowsDomainException()
    {
        var responseJson = """
            {
              "code": "200",
              "msg": "OK",
              "details": [
                {
                  "b2bCrtAvgAmount": "100"
                }
              ]
            }
            """;

        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest();

        var exception = await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => client.QueryInvoicesAsync(request));

        Assert.Equal(200, exception.StatusCode);
        Assert.False(exception.IsTransient);
        Assert.Contains("invoiceDate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueryInvoicesAsync_DetailsMissing_UsesTopLevelRaw1FallbackToBuildRecords()
    {
        var responseJson = """
            {
              "code": "200",
              "msg": "OK",
              "raw1": [
                {
                  "invoiceNo": "RAW1-001",
                  "invoiceDate": "2026-06-20",
                  "buyerTaxId": "11111111",
                  "sellerTaxId": "22222222",
                  "netAmount": "100",
                  "taxAmount": "5",
                  "totalAmount": "105"
                }
              ]
            }
            """;

        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest();

        var result = await client.QueryInvoicesAsync(request);

        Assert.Single(result.Items);
        var record = result.Items[0];
        Assert.Equal("RAW1-001", record.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 6, 20), record.InvoiceDate);
        Assert.Equal(105m, record.TotalAmount);
        Assert.NotNull(record.Raw1Items);
        Assert.Single(record.Raw1Items!);
    }

    [Fact]
    public async Task QueryInvoicesAsync_TopLevelRaw1ExistsAndDetailsMissingRaw1_AttachesRaw1ToFirstRecord()
    {
        var responseJson = """
            {
              "code": "200",
              "msg": "OK",
              "details": [
                {
                  "invoiceDate": "2026-07-01",
                  "b2bCrtAvgAmount": "80",
                  "b2cCrtAvgAmount": "20"
                }
              ],
              "raw1": [
                {
                  "invoiceNo": "RAW1-ATTACH",
                  "itemName": "統計來源"
                }
              ]
            }
            """;

        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest();

        var result = await client.QueryInvoicesAsync(request);

        Assert.Single(result.Items);
        Assert.NotNull(result.Items[0].Raw1Items);
        Assert.Single(result.Items[0].Raw1Items!);
        Assert.Equal("RAW1-ATTACH", result.Items[0].Raw1Items![0]["invoiceNo"]);
    }

    [Fact]
    public async Task QueryInvoicesAsync_PurchaseAllowanceScope_UsesPurchaseAllowanceAmountFields()
    {
        var responseJson = """
            {
              "code": "200",
              "msg": "OK",
              "details": [
                {
                  "invoiceDate": "2026-07-15",
                  "b2bPurAllwAvgAmount": "120",
                  "b2cPurAllwAvgAmount": "30"
                }
              ]
            }
            """;

        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var client = new MofOfficialHttpInvoiceApiClient(new HttpClient(handler), "https://www-vc.einvoice.nat.gov.tw");
        var request = CreateBaseRequest(
            direction: InvoiceDirection.Purchase,
            documentType: InvoiceDocumentType.Allowance);

        var result = await client.QueryInvoicesAsync(request);

        Assert.Single(result.Items);
        Assert.Equal(150m, result.Items[0].TotalAmount);
        Assert.Equal(InvoiceDirection.Purchase, result.Items[0].Direction);
        Assert.Equal(InvoiceDocumentType.Allowance, result.Items[0].DocumentType);
    }

    private static OfficialInvoicePageRequest CreateBaseRequest(
        InvoiceDirection direction = InvoiceDirection.Sales,
        InvoiceDocumentType documentType = InvoiceDocumentType.Invoice)
    {
        return new OfficialInvoicePageRequest(
            SessionToken: "unused-in-v1.9",
            TaxId: "12345678",
            DateFrom: new DateOnly(2025, 9, 7),
            DateTo: new DateOnly(2025, 9, 10),
            Direction: direction,
            PageNumber: 1,
            PageSize: 200,
            DocumentType: documentType,
            OfficialApiAppId: "app-id-001",
            OfficialApiAccount: "merchant-account",
            OfficialApiPassword: "merchant-password");
    }

    private sealed class RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return _responseFactory(request);
        }
    }

    private sealed class ThrowingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> callback)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _callback = callback;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_callback(request));
        }
    }
}
