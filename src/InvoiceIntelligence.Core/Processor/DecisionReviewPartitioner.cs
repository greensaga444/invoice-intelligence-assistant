namespace InvoiceIntelligence.Core.Processor;

public static class DecisionReviewPartitioner
{
    public static (
        IReadOnlyList<InvoiceDecisionResult> ReadyForFolderB,
        IReadOnlyList<InvoiceDecisionResult> NeedManualReview)
        SplitByThreshold(
            IReadOnlyCollection<InvoiceDecisionResult> decisionResults,
            decimal lowConfidenceThreshold,
            string invoiceFileSuffix,
            string allowanceFileSuffix)
    {
        ArgumentNullException.ThrowIfNull(decisionResults);

        var readyForFolderB = new List<InvoiceDecisionResult>(decisionResults.Count);
        var needManualReview = new List<InvoiceDecisionResult>();

        var groupedByReviewGroup = decisionResults
            .GroupBy(
                item => FolderAFileCategoryHelper.BuildGroupKey(
                    item.Record.SourceFileName,
                    invoiceFileSuffix,
                    allowanceFileSuffix),
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupedByReviewGroup)
        {
            if (group.Any(item => item.Decision.ConfidenceScore < lowConfidenceThreshold))
            {
                needManualReview.AddRange(group);
                continue;
            }

            readyForFolderB.AddRange(group);
        }

        return (readyForFolderB, needManualReview);
    }
}
