using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class ConfigurationDefaultsTests
{
    [Fact]
    public void DefaultValues_ArePresent()
    {
        Assert.False(string.IsNullOrWhiteSpace(ConfigurationDefaults.KnowledgeRepositoryPath));
        Assert.False(string.IsNullOrWhiteSpace(ConfigurationDefaults.CustomerListFilePath));
        Assert.False(string.IsNullOrWhiteSpace(ConfigurationDefaults.FolderCPath));
        Assert.False(string.IsNullOrWhiteSpace(ConfigurationDefaults.InvoiceOutputLabel));
        Assert.False(string.IsNullOrWhiteSpace(ConfigurationDefaults.AllowanceOutputLabel));
        Assert.False(string.IsNullOrWhiteSpace(ConfigurationDefaults.InvoiceFileSuffix));
        Assert.False(string.IsNullOrWhiteSpace(ConfigurationDefaults.AllowanceFileSuffix));
        Assert.False(string.IsNullOrWhiteSpace(ConfigurationDefaults.OfficialDownloadMode));
        Assert.False(string.IsNullOrWhiteSpace(ConfigurationDefaults.OfficialApiBaseUrl));
        Assert.Equal(0.80m, ConfigurationDefaults.LowConfidenceThreshold);
    }
}
