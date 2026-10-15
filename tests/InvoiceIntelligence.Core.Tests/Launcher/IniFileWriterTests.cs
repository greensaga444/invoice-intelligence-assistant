using InvoiceIntelligence.Core.Configuration;
using InvoiceLauncher;

namespace InvoiceIntelligence.Core.Tests.Launcher;

public class IniFileWriterTests
{
    [Fact]
    public void DownloaderIniFileWriter_Write_PersistsEncryptedPasswordAndResolvedCredentials()
    {
        var tempRoot = CreateTempDirectory();
        var iniPath = Path.Combine(tempRoot, "nested", "downloader.ini");
        var settings = CreateSettings() with
        {
            OfficialApiAppId = "",
            OfficialApiAppKey = "shared-key",
            OfficialApiPassword = "password123"
        };

        try
        {
            DownloaderIniFileWriter.Write(iniPath, settings);

            Assert.True(File.Exists(iniPath));

            var loader = new IniModuleConfigurationLoader();
            var config = loader.Load(iniPath);

            Assert.Equal("shared-key", config.OfficialApiAppId);
            Assert.Equal("shared-key", config.OfficialApiAppKey);
            Assert.False(string.IsNullOrWhiteSpace(config.OfficialApiPasswordEncrypted));
            Assert.NotEqual(settings.OfficialApiPassword, config.OfficialApiPasswordEncrypted);

            var decrypted = MofPasswordProtector.TryDecrypt(config.OfficialApiPasswordEncrypted, out var plainText);
            Assert.True(decrypted);
            Assert.Equal(settings.OfficialApiPassword, plainText);
        }
        finally
        {
            DeleteDirectory(tempRoot);
        }
    }

    [Fact]
    public void ProcessorIniFileWriter_Write_UsesExistingModuleNameAndSettingsFields()
    {
        var tempRoot = CreateTempDirectory();
        var iniPath = Path.Combine(tempRoot, "nested", "processor.ini");
        var settings = CreateSettings() with
        {
            ModuleName = "SettingsModuleName",
            ProcessorKnowledgeRepositoryFile = "data/custom/repo.json",
            LowConfidenceThreshold = 0.72m,
            OfficialApiAppId = "processor-app-id",
            OfficialApiAppKey = "",
            InvoiceOutputLabel = "Invoice-Label",
            AllowanceOutputLabel = "Allowance-Label",
            InvoiceFileSuffix = "invoice-data",
            AllowanceFileSuffix = "allowance-data"
        };

        var existingConfig = new ModuleConfiguration(
            ModuleName: "ExistingProcessorModule",
            FolderAPath: "legacy/folder-a",
            FolderBPath: "legacy/folder-b",
            OutputNamingRule: "{taxId}_{timestamp}.xlsx",
            MofAccount: "legacy-account",
            MofCredentialKey: "legacy-key",
            KnowledgeRepositoryPath: "legacy/repo.json");

        try
        {
            ProcessorIniFileWriter.Write(iniPath, existingConfig, settings);

            Assert.True(File.Exists(iniPath));

            var loader = new IniModuleConfigurationLoader();
            var config = loader.Load(iniPath);

            Assert.Equal("ExistingProcessorModule", config.ModuleName);
            Assert.Equal(settings.FolderAPath, config.FolderAPath);
            Assert.Equal(settings.FolderBPath, config.FolderBPath);
            Assert.Equal(settings.FolderCPath, config.FolderCPath);
            Assert.Equal(settings.ProcessorFileNamingRule, config.OutputNamingRule);
            Assert.Equal(settings.ProcessorKnowledgeRepositoryFile, config.KnowledgeRepositoryPath);
            Assert.Equal(settings.LowConfidenceThreshold, config.LowConfidenceThreshold);
            Assert.Equal(settings.InvoiceOutputLabel, config.InvoiceOutputLabel);
            Assert.Equal(settings.AllowanceOutputLabel, config.AllowanceOutputLabel);
            Assert.Equal(settings.InvoiceFileSuffix, config.InvoiceFileSuffix);
            Assert.Equal(settings.AllowanceFileSuffix, config.AllowanceFileSuffix);
            Assert.Equal("processor-app-id", config.OfficialApiAppId);
            Assert.Equal("processor-app-id", config.OfficialApiAppKey);
        }
        finally
        {
            DeleteDirectory(tempRoot);
        }
    }

    private static LauncherIniSettings CreateSettings()
    {
        return new LauncherIniSettings(
            ModuleName: "Invoice Module",
            FolderAPath: "data/folder-a",
            FolderBPath: "data/folder-b",
            FolderCPath: "data/folder-c",
            CustomerListFilePath: "config/company-list.xlsx",
            FileNamingRule: "{taxId}_{companyName}_{timestamp}.xlsx",
            InvoiceFileSuffix: "invoice",
            AllowanceFileSuffix: "allowance",
            InvoiceOutputLabel: "SalesInvoiceDetail-T3",
            AllowanceOutputLabel: "PurchaseVoucherDetail-T4",
            ProcessorFileNamingRule: "{taxId}_{timestamp}.xlsx",
            ProcessorKnowledgeRepositoryFile: "data/knowledge/knowledge-cases.json",
            LowConfidenceThreshold: 0.80m,
            MofAccount: "mof-account",
            OfficialApiBaseUrl: "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api",
            OfficialApiAppId: "app-id",
            OfficialApiAppKey: "app-key",
            OfficialApiPassword: "password123");
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"invoice-launcher-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
