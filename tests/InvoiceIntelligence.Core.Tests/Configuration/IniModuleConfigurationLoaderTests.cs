using System.IO;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class IniModuleConfigurationLoaderTests
{
    [Fact]
    public void Load_ValidIni_ReturnsConfiguration()
    {
        const string iniContent = """
            [module]
            name = Downloader

            [paths]
            folderA = C:\Data\FolderA
            folderB = C:\Data\FolderB

            [output]
            fileNamingRule = {taxId}_{companyName}_{timestamp}.xlsx

            [mof]
            account = sample-account
            appKey = sample-secret
            """;

        var iniPath = WriteTempIni(iniContent);
        var loader = new IniModuleConfigurationLoader();

        try
        {
            var config = loader.Load(iniPath);

            Assert.Equal("Downloader", config.ModuleName);
            Assert.Equal("C:\\Data\\FolderA", config.FolderAPath);
            Assert.Equal("C:\\Data\\FolderB", config.FolderBPath);
            Assert.Equal("{taxId}_{companyName}_{timestamp}.xlsx", config.OutputNamingRule);
            Assert.Equal("sample-account", config.MofAccount);
            Assert.Equal("sample-secret", config.MofCredentialKey);
            Assert.Equal("data\\knowledge\\knowledge-cases.json", config.KnowledgeRepositoryPath);
            Assert.Equal("local", config.InvoiceDownloadMode);
            Assert.Null(config.OfficialApiBaseUrl);
            Assert.Equal("sample-secret", config.OfficialApiAppId);
            Assert.Equal("sample-secret", config.OfficialApiAppKey);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Load_KnowledgeRepositoryPathProvided_UsesConfiguredPath()
    {
        const string iniContent = """
            [module]
            name = Processor

            [paths]
            folderA = C:\Data\FolderA
            folderB = C:\Data\FolderB

            [output]
            fileNamingRule = {taxId}_{companyName}_{timestamp}.xlsx

            [knowledge]
            repositoryFile = D:\Knowledge\cases.json

            [mof]
            account = sample-account
            appKey = sample-secret
            """;

        var iniPath = WriteTempIni(iniContent);
        var loader = new IniModuleConfigurationLoader();

        try
        {
            var config = loader.Load(iniPath);

            Assert.Equal("D:\\Knowledge\\cases.json", config.KnowledgeRepositoryPath);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Load_UnknownPlaceholder_ThrowsFormatException()
    {
        const string iniContent = """
            [module]
            name = Downloader

            [paths]
            folderA = C:\Data\FolderA
            folderB = C:\Data\FolderB

            [output]
            fileNamingRule = {unknown}_{timestamp}.xlsx

            [mof]
            account = sample-account
            appKey = sample-secret
            """;

        var iniPath = WriteTempIni(iniContent);
        var loader = new IniModuleConfigurationLoader();

        try
        {
            Assert.Throws<FormatException>(() => loader.Load(iniPath));
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Load_MissingRequiredKey_ThrowsInvalidDataException()
    {
        const string iniContent = """
            [module]
            name = Downloader

            [paths]
            folderA = C:\Data\FolderA

            [output]
            fileNamingRule = {taxId}_{timestamp}.xlsx

            [mof]
            account = sample-account
            appKey = sample-secret
            """;

        var iniPath = WriteTempIni(iniContent);
        var loader = new IniModuleConfigurationLoader();

        try
        {
            Assert.Throws<InvalidDataException>(() => loader.Load(iniPath));
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Load_OfficialMode_ParsesBaseUrl()
    {
        const string iniContent = """
            [module]
            name = Downloader

            [paths]
            folderA = C:\Data\FolderA
            folderB = C:\Data\FolderB

            [output]
            fileNamingRule = {taxId}_{timestamp}.xlsx

            [mof]
            account = sample-account
            invoiceDownloadMode = official
            officialApiBaseUrl = https://service-mc.einvoice.nat.gov.tw/ptl/portal/api
            appId = sample-app-id
            appKey = sample-app-key
            """;

        var iniPath = WriteTempIni(iniContent);
        var loader = new IniModuleConfigurationLoader();

        try
        {
            var config = loader.Load(iniPath);

            Assert.Equal("official", config.InvoiceDownloadMode);
            Assert.Equal("https://service-mc.einvoice.nat.gov.tw/ptl/portal/api", config.OfficialApiBaseUrl);
            Assert.Equal("sample-app-id", config.OfficialApiAppId);
            Assert.Equal("sample-app-key", config.OfficialApiAppKey);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Load_OfficialModeWithoutBaseUrl_ThrowsInvalidDataException()
    {
        const string iniContent = """
            [module]
            name = Downloader

            [paths]
            folderA = C:\Data\FolderA
            folderB = C:\Data\FolderB

            [output]
            fileNamingRule = {taxId}_{timestamp}.xlsx

            [mof]
            account = sample-account
            appKey = sample-secret
            invoiceDownloadMode = official
            """;

        var iniPath = WriteTempIni(iniContent);
        var loader = new IniModuleConfigurationLoader();

        try
        {
            Assert.Throws<InvalidDataException>(() => loader.Load(iniPath));
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Load_AppKeyOnly_UsesAppKeyAsCredentialKey()
    {
        const string iniContent = """
            [module]
            name = Processor

            [paths]
            folderA = C:\Data\FolderA
            folderB = C:\Data\FolderB

            [output]
            fileNamingRule = {taxId}_{companyName}_{timestamp}.xlsx

            [mof]
            account = sample-account
            appId = sample-app-id
            appKey = sample-app-key
            """;

        var iniPath = WriteTempIni(iniContent);
        var loader = new IniModuleConfigurationLoader();

        try
        {
            var config = loader.Load(iniPath);

            Assert.Equal("sample-app-key", config.MofCredentialKey);
            Assert.Equal("sample-app-id", config.OfficialApiAppId);
            Assert.Equal("sample-app-key", config.OfficialApiAppKey);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void Load_WithoutAppKey_ThrowsInvalidDataException()
    {
        const string iniContent = """
            [module]
            name = Processor

            [paths]
            folderA = C:\Data\FolderA
            folderB = C:\Data\FolderB

            [output]
            fileNamingRule = {taxId}_{companyName}_{timestamp}.xlsx

            [mof]
            account = sample-account
            appId = sample-app-id
            """;

        var iniPath = WriteTempIni(iniContent);
        var loader = new IniModuleConfigurationLoader();

        try
        {
            Assert.Throws<InvalidDataException>(() => loader.Load(iniPath));
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    private static string WriteTempIni(string content)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-{Guid.NewGuid():N}.ini");
        File.WriteAllText(tempPath, content);
        return tempPath;
    }
}
