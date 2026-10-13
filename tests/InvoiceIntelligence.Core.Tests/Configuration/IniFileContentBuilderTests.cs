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
            fileNamingRule: "{taxId}_{timestamp}.xlsx",
            mofAccount: "sample-account",
            invoiceDownloadMode: "official",
            officialApiBaseUrl: "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api",
            officialApiAppId: "",
            officialApiAppKey: "sample-app-key");

        Assert.Contains("[module]", content, StringComparison.Ordinal);
        Assert.Contains("[mof]", content, StringComparison.Ordinal);
        Assert.Contains("invoiceDownloadMode = official", content, StringComparison.Ordinal);
        Assert.Contains("appId = sample-app-key", content, StringComparison.Ordinal);
        Assert.Contains("appKey = sample-app-key", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDownloaderContent_WhenBothAppIdAndAppKeyProvided_PreservesBothValues()
    {
        var content = IniFileContentBuilder.BuildDownloaderContent(
            moduleName: "Downloader",
            folderAPath: "C:\\Data\\FolderA",
            folderBPath: "C:\\Data\\FolderB",
            fileNamingRule: "{taxId}_{timestamp}.xlsx",
            mofAccount: "sample-account",
            invoiceDownloadMode: "official",
            officialApiBaseUrl: "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api",
            officialApiAppId: "sample-app-id",
            officialApiAppKey: "sample-app-key");

        Assert.Contains("appId = sample-app-id", content, StringComparison.Ordinal);
        Assert.Contains("appKey = sample-app-key", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildProcessorContent_WhenAppKeyIsMissing_UsesAppIdForBothFields()
    {
        var content = IniFileContentBuilder.BuildProcessorContent(
            moduleName: "Processor",
            folderAPath: "C:\\Data\\FolderA",
            folderBPath: "C:\\Data\\FolderB",
            fileNamingRule: "{taxId}_{companyName}_{timestamp}.xlsx",
            knowledgeRepositoryFile: "D:\\Knowledge\\cases.json",
            mofAccount: "sample-account",
            officialApiAppId: "sample-app-id",
            officialApiAppKey: "");

        Assert.Contains("[knowledge]", content, StringComparison.Ordinal);
        Assert.Contains("repositoryFile = D:\\Knowledge\\cases.json", content, StringComparison.Ordinal);
        Assert.Contains("appId = sample-app-id", content, StringComparison.Ordinal);
        Assert.Contains("appKey = sample-app-id", content, StringComparison.Ordinal);
    }
}