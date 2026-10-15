using InvoiceLauncher;

namespace InvoiceIntelligence.Core.Tests.Launcher;

public class LauncherSettingsValidatorTests
{
    [Fact]
    public void TryValidate_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LauncherSettingsValidator.TryValidate(null!, out _));
    }

    [Fact]
    public void TryValidate_MissingRequiredField_ReturnsFalse()
    {
        var input = CreateValidInput() with { ModuleName = " " };

        var success = LauncherSettingsValidator.TryValidate(input, out var errorMessage);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(errorMessage));
    }

    [Fact]
    public void TryValidate_InvalidThreshold_ReturnsFalse()
    {
        var input = CreateValidInput() with { LowConfidenceThresholdText = "1.50" };

        var success = LauncherSettingsValidator.TryValidate(input, out var errorMessage);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(errorMessage));
    }

    [Fact]
    public void TryValidate_MissingBaseUrl_ReturnsFalse()
    {
        var input = CreateValidInput() with { OfficialApiBaseUrl = " " };

        var success = LauncherSettingsValidator.TryValidate(input, out var errorMessage);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(errorMessage));
    }

    [Fact]
    public void TryValidate_MissingOfficialPassword_ReturnsFalse()
    {
        var input = CreateValidInput() with { OfficialApiPassword = " " };

        var success = LauncherSettingsValidator.TryValidate(input, out var errorMessage);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(errorMessage));
    }

    [Fact]
    public void TryValidate_ValidInput_ReturnsTrue()
    {
        var input = CreateValidInput();

        var success = LauncherSettingsValidator.TryValidate(input, out var errorMessage);

        Assert.True(success);
        Assert.Equal(string.Empty, errorMessage);
    }

    private static LauncherSettingsInput CreateValidInput()
    {
        return new LauncherSettingsInput(
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
            LowConfidenceThresholdText: "0.80",
            MofAccount: "mof-account",
            OfficialApiBaseUrl: "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api",
            OfficialApiAppId: "app-id",
            OfficialApiAppKey: "app-key",
            OfficialApiPassword: "password123");
    }
}
