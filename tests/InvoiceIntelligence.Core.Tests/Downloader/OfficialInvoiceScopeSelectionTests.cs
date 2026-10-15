using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class OfficialInvoiceScopeSelectionTests
{
    [Fact]
    public void Validate_WhenNoScopeEnabled_ThrowsInvalidOperationException()
    {
        var selection = new OfficialInvoiceScopeSelection(
            IncludeSalesInvoices: false,
            IncludePurchaseInvoices: false,
            IncludeSalesAllowances: false,
            IncludePurchaseAllowances: false);

        Assert.Throws<InvalidOperationException>(() => selection.Validate());
    }

    [Fact]
    public void IncludesAnyScope_WhenAnyFlagEnabled_ReturnsTrue()
    {
        var selection = new OfficialInvoiceScopeSelection(
            IncludeSalesInvoices: false,
            IncludePurchaseInvoices: false,
            IncludeSalesAllowances: true,
            IncludePurchaseAllowances: false);

        Assert.False(selection.IncludesAnyInvoice);
        Assert.True(selection.IncludesAnyAllowance);
        Assert.True(selection.IncludesAnyScope);
    }
}
