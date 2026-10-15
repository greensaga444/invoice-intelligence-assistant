using InvoiceDownloader;
using InvoiceIntelligence.Core.Configuration;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class DownloaderWorkflowServiceTests
{
    [Fact]
    public void TryValidateAuthentication_NullConfiguration_ReturnsFalse()
    {
        var service = new DownloaderWorkflowService();

        var success = service.TryValidateAuthentication(
            configuration: null,
            loadedCustomers: [CreateCustomer()],
            out var message);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void TryValidateAuthentication_EmptyCustomers_ReturnsFalse()
    {
        var service = new DownloaderWorkflowService();

        var success = service.TryValidateAuthentication(
            configuration: CreateConfiguration(),
            loadedCustomers: [],
            out var message);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void TryValidateAuthentication_ValidInput_ReturnsTrueWithEmptyMessage()
    {
        var service = new DownloaderWorkflowService();

        var success = service.TryValidateAuthentication(
            configuration: CreateConfiguration(),
            loadedCustomers: [CreateCustomer()],
            out var message);

        Assert.True(success);
        Assert.Equal(string.Empty, message);
    }

    [Fact]
    public void BuildCredential_NullConfiguration_ThrowsArgumentNullException()
    {
        var service = new DownloaderWorkflowService();

        Assert.Throws<ArgumentNullException>(() =>
            service.BuildCredential(null!, "api-key", "configured-pass", "input-pass"));
    }

    [Fact]
    public void BuildCredential_EmptyInputPassword_FallsBackToConfiguredPassword()
    {
        var service = new DownloaderWorkflowService();
        var config = CreateConfiguration();

        var credential = service.BuildCredential(config, "api-key", "configured-pass", " ");

        Assert.Equal(config.MofAccount, credential.Account);
        Assert.Equal("api-key", credential.ApiKey);
        Assert.Equal("configured-pass", credential.Password);
    }

    [Fact]
    public void BuildCredential_InputPasswordProvided_OverridesConfiguredPassword()
    {
        var service = new DownloaderWorkflowService();
        var config = CreateConfiguration();

        var credential = service.BuildCredential(config, "api-key", "configured-pass", "input-pass");

        Assert.Equal("input-pass", credential.Password);
    }

    [Fact]
    public void TryValidateBatchDownload_NullConfiguration_ReturnsFalse()
    {
        var service = new DownloaderWorkflowService();

        var success = service.TryValidateBatchDownload(
            configuration: null,
            loadedCustomers: [CreateCustomer()],
            hasActiveSession: true,
            batchInvoiceDownloader: new StubBatchInvoiceDownloader(),
            out var message);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void TryValidateBatchDownload_EmptyCustomers_ReturnsFalse()
    {
        var service = new DownloaderWorkflowService();

        var success = service.TryValidateBatchDownload(
            configuration: CreateConfiguration(),
            loadedCustomers: [],
            hasActiveSession: true,
            batchInvoiceDownloader: new StubBatchInvoiceDownloader(),
            out var message);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void TryValidateBatchDownload_InactiveSession_ReturnsFalse()
    {
        var service = new DownloaderWorkflowService();

        var success = service.TryValidateBatchDownload(
            configuration: CreateConfiguration(),
            loadedCustomers: [CreateCustomer()],
            hasActiveSession: false,
            batchInvoiceDownloader: new StubBatchInvoiceDownloader(),
            out var message);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void TryValidateBatchDownload_NullDownloader_ReturnsFalse()
    {
        var service = new DownloaderWorkflowService();

        var success = service.TryValidateBatchDownload(
            configuration: CreateConfiguration(),
            loadedCustomers: [CreateCustomer()],
            hasActiveSession: true,
            batchInvoiceDownloader: null,
            out var message);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void TryValidateBatchDownload_ValidInput_ReturnsTrueWithEmptyMessage()
    {
        var service = new DownloaderWorkflowService();

        var success = service.TryValidateBatchDownload(
            configuration: CreateConfiguration(),
            loadedCustomers: [CreateCustomer()],
            hasActiveSession: true,
            batchInvoiceDownloader: new StubBatchInvoiceDownloader(),
            out var message);

        Assert.True(success);
        Assert.Equal(string.Empty, message);
    }

    [Fact]
    public void BuildBatchRequest_NullConfiguration_ThrowsArgumentNullException()
    {
        var service = new DownloaderWorkflowService();

        Assert.Throws<ArgumentNullException>(() => service.BuildBatchRequest(
            configuration: null!,
            loadedCustomers: [CreateCustomer()],
            folderAPath: "folder-a",
            sessionToken: "session",
            officialApiAppId: "app-id",
            authenticatedPassword: "pwd"));
    }

    [Fact]
    public void BuildBatchRequest_MapsFieldsFromInputs()
    {
        var service = new DownloaderWorkflowService();
        var config = CreateConfiguration() with
        {
            OutputNamingRule = "{taxId}_{timestamp}.xlsx",
            InvoiceFileSuffix = "invoice-data",
            AllowanceFileSuffix = "allowance-data"
        };
        var customers = new[] { CreateCustomer() };

        var request = service.BuildBatchRequest(
            configuration: config,
            loadedCustomers: customers,
            folderAPath: "folder-a",
            sessionToken: "session-token",
            officialApiAppId: "app-id",
            authenticatedPassword: "secret");

        Assert.Same(customers, request.Customers);
        Assert.Equal("folder-a", request.FolderAPath);
        Assert.Equal("session-token", request.SessionToken);
        Assert.Equal("app-id", request.OfficialApiAppId);
        Assert.Equal(config.MofAccount, request.OfficialApiAccount);
        Assert.Equal("secret", request.OfficialApiPassword);
        Assert.Equal(config.OutputNamingRule, request.OutputNamingRule);
        Assert.Equal("invoice-data", request.InvoiceFileSuffix);
        Assert.Equal("allowance-data", request.AllowanceFileSuffix);
    }

    private static ModuleConfiguration CreateConfiguration()
    {
        return new ModuleConfiguration(
            ModuleName: "Downloader",
            FolderAPath: "data/folder-a",
            FolderBPath: "data/folder-b",
            OutputNamingRule: "{taxId}_{companyName}_{timestamp}.xlsx",
            MofAccount: "sample-account",
            MofCredentialKey: "sample-key",
            KnowledgeRepositoryPath: "data/knowledge/knowledge-cases.json");
    }

    private static CustomerRecord CreateCustomer()
    {
        return new CustomerRecord(
            TaxId: "12345678",
            CompanyName: "Alpha Co",
            DateFrom: new DateOnly(2026, 8, 1),
            DateTo: new DateOnly(2026, 8, 31));
    }

    private sealed class StubBatchInvoiceDownloader : IBatchInvoiceDownloader
    {
        public Task<BatchInvoiceDownloadResult> DownloadAsync(BatchInvoiceDownloadRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BatchInvoiceDownloadResult([], string.Empty));
        }
    }
}
