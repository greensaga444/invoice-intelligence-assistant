using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class FolderAFileCategoryHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void GetCategory_BlankFileName_ReturnsUnknown(string? filePathOrName)
    {
        var result = FolderAFileCategoryHelper.GetCategory(filePathOrName!);

        Assert.Equal(FolderAFileCategory.Unknown, result);
    }

    [Fact]
    public void GetCategory_RecognizesInvoiceAndAllowanceIncludingLegacyTypo()
    {
        Assert.Equal(FolderAFileCategory.Invoice, FolderAFileCategoryHelper.GetCategory("A-invoice.xlsx"));
        Assert.Equal(FolderAFileCategory.Allowance, FolderAFileCategoryHelper.GetCategory("A-allowance.xlsx"));
        Assert.Equal(FolderAFileCategory.Allowance, FolderAFileCategoryHelper.GetCategory("A-allwance.xlsx"));
    }

    [Fact]
    public void GetCategory_UsesNormalizedCustomSuffixes()
    {
        var invoice = FolderAFileCategoryHelper.GetCategory("A-inv.xlsx", invoiceFileSuffix: "inv", allowanceFileSuffix: "allow");
        var allowance = FolderAFileCategoryHelper.GetCategory("A-allow.xlsx", invoiceFileSuffix: "inv", allowanceFileSuffix: "allow");

        Assert.Equal(FolderAFileCategory.Invoice, invoice);
        Assert.Equal(FolderAFileCategory.Allowance, allowance);
    }

    [Fact]
    public void IsPreferredSourceFile_UnknownFile_ReturnsFalse()
    {
        Assert.False(FolderAFileCategoryHelper.IsPreferredSourceFile("notes.txt"));
        Assert.True(FolderAFileCategoryHelper.IsPreferredSourceFile("A-allwance.xlsx"));
    }

    [Theory]
    [InlineData("A-invoice.xlsx", "A")]
    [InlineData("A-allowance.xlsx", "A")]
    [InlineData("A-allwance.xlsx", "A")]
    [InlineData("A-other.xlsx", "A-other")]
    public void BuildGroupKey_HandlesKnownSuffixes(string sourceFileName, string expectedKey)
    {
        var result = FolderAFileCategoryHelper.BuildGroupKey(sourceFileName);

        Assert.Equal(expectedKey, result);
    }

    [Fact]
    public void BuildGroupKey_WhenTrimmedNameWouldBeEmpty_ReturnsOriginalFileName()
    {
        var result = FolderAFileCategoryHelper.BuildGroupKey("-invoice.xlsx");

        Assert.Equal("-invoice.xlsx", result);
    }
}
