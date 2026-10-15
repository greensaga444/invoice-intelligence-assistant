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
}
