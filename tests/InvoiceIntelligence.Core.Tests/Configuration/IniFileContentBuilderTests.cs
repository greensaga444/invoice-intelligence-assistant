using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class IniFileContentBuilderTests
{
    [Fact]
    public void BuildDownloaderContent_WhenAppIdIsMissing_UsesAppKeyForBothFields()
    {
        var content = IniFileContentBuilder.BuildDownloaderContent(
            moduleName: "Downloader",
            folderAPath: "C:\\Data\\FolderA",
            folderBPath: "C:\\Data\\FolderB",
            customerListFilePath: "config\\company-list.xlsx",
            fileNamingRule: "{taxId}_{timestamp}.xlsx",
            invoiceFileSuffix: "invoice",
            allowanceFileSuffix: "allowance",
            mofAccount: "sample-account",
            officialApiBaseUrl: "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api",
            officialApiAppId: "",
            officialApiAppKey: "sample-app-key",
            officialApiPasswordEncrypted: "encrypted-password");

        Assert.Contains("[module]", content, StringComparison.Ordinal);
        Assert.Contains("[input]", content, StringComparison.Ordinal);
        Assert.Contains("customerListFile = config\\company-list.xlsx", content, StringComparison.Ordinal);
        Assert.Contains("invoiceFileSuffix = invoice", content, StringComparison.Ordinal);
        Assert.Contains("allowanceFileSuffix = allowance", content, StringComparison.Ordinal);
        Assert.Contains("[mof]", content, StringComparison.Ordinal);
        Assert.DoesNotContain("invoiceDownloadMode", content, StringComparison.Ordinal);
        Assert.Contains("appId = sample-app-key", content, StringComparison.Ordinal);
        Assert.Contains("appKey = sample-app-key", content, StringComparison.Ordinal);
        Assert.Contains("passwordEncrypted = encrypted-password", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDownloaderContent_WhenBothAppIdAndAppKeyProvided_PreservesBothValues()
    {
        var content = IniFileContentBuilder.BuildDownloaderContent(
            moduleName: "Downloader",
            folderAPath: "C:\\Data\\FolderA",
            folderBPath: "C:\\Data\\FolderB",
            customerListFilePath: "config\\company-list.xlsx",
            fileNamingRule: "{taxId}_{timestamp}.xlsx",
            invoiceFileSuffix: "invoice",
            allowanceFileSuffix: "allowance",
            mofAccount: "sample-account",
            officialApiBaseUrl: "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api",
            officialApiAppId: "sample-app-id",
            officialApiAppKey: "sample-app-key",
            officialApiPasswordEncrypted: "encrypted-password");

        Assert.Contains("appId = sample-app-id", content, StringComparison.Ordinal);
        Assert.Contains("appKey = sample-app-key", content, StringComparison.Ordinal);
        Assert.Contains("passwordEncrypted = encrypted-password", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildProcessorContent_WhenAppKeyIsMissing_UsesAppIdForBothFields()
    {
        var content = IniFileContentBuilder.BuildProcessorContent(
            moduleName: "Processor",
            folderAPath: "C:\\Data\\FolderA",
            folderBPath: "C:\\Data\\FolderB",
            folderCPath: "C:\\Data\\FolderC",
            fileNamingRule: "{taxId}_{companyName}_{timestamp}.xlsx",
            invoiceFileSuffix: "invoice",
            allowanceFileSuffix: "allowance",
            lowConfidenceThreshold: 0.75m,
            knowledgeRepositoryFile: "D:\\Knowledge\\cases.json",
            mofAccount: "sample-account",
            officialApiAppId: "sample-app-id",
            officialApiAppKey: "");

        Assert.Contains("folderC = C:\\Data\\FolderC", content, StringComparison.Ordinal);
        Assert.Contains("invoiceOutputLabel = SalesInvoiceDetail-T3", content, StringComparison.Ordinal);
        Assert.Contains("allowanceOutputLabel = PurchaseVoucherDetail-T4", content, StringComparison.Ordinal);
        Assert.Contains("invoiceFileSuffix = invoice", content, StringComparison.Ordinal);
        Assert.Contains("allowanceFileSuffix = allowance", content, StringComparison.Ordinal);
        Assert.Contains("[review]", content, StringComparison.Ordinal);
        Assert.Contains("confidenceThreshold = 0.75", content, StringComparison.Ordinal);
        Assert.Contains("[knowledge]", content, StringComparison.Ordinal);
        Assert.Contains("repositoryFile = D:\\Knowledge\\cases.json", content, StringComparison.Ordinal);
        Assert.Contains("appId = sample-app-id", content, StringComparison.Ordinal);
        Assert.Contains("appKey = sample-app-id", content, StringComparison.Ordinal);
    }
}