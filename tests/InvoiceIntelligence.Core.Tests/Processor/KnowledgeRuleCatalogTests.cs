using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class KnowledgeRuleCatalogTests
{
    [Fact]
    public void DefaultCaseCache_HasSeedData()
    {
        Assert.NotEmpty(KnowledgeRuleCatalog.DefaultCaseCache);
        Assert.All(KnowledgeRuleCatalog.DefaultCaseCache, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.VendorTaxId));
            Assert.False(string.IsNullOrWhiteSpace(item.ItemName));
        });
    }

    [Fact]
    public void DefaultKnowledgeCases_HasSeedData()
    {
        Assert.NotEmpty(KnowledgeRuleCatalog.DefaultKnowledgeCases);
        Assert.All(KnowledgeRuleCatalog.DefaultKnowledgeCases, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.CaseId));
            Assert.NotEmpty(item.Keywords);
        });
    }
}
