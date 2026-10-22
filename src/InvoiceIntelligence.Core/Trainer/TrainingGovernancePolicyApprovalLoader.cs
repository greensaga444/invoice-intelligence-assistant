using System.Text.Json;
using System.Text.RegularExpressions;

namespace InvoiceIntelligence.Core.Trainer;

internal sealed class TrainingGovernancePolicyApprovalLoader
{
    private const int SupportedPolicyMajorVersion = 1;

    private static readonly Regex SemanticVersionPattern = new(
        "^\\d+\\.\\d+\\.\\d+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TrainingGovernancePolicyApprovalManifest LoadApprovedManifest(string policyFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyFilePath);

        if (!File.Exists(policyFilePath))
        {
            throw new InvalidOperationException(
                $"找不到治理政策檔：{policyFilePath}。請提供已核准 policy 後再執行訓練流程。");
        }

        GovernancePolicyDocument? document;
        try
        {
            var json = File.ReadAllText(policyFilePath);
            document = JsonSerializer.Deserialize<GovernancePolicyDocument>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"治理政策檔格式錯誤：{policyFilePath}。{ex.Message}",
                ex);
        }

        if (document is null)
        {
            throw new InvalidOperationException($"治理政策檔內容為空：{policyFilePath}。");
        }

        if (!string.Equals(document.Status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            var statusDisplay = string.IsNullOrWhiteSpace(document.Status) ? "<null>" : document.Status.Trim();
            throw new InvalidOperationException(
                $"治理政策未核准：status={statusDisplay}。請將 status 設為 approved，並填寫 approvedBy/approvedAt/effectiveFrom。");
        }

        if (string.IsNullOrWhiteSpace(document.PolicyId))
        {
            throw new InvalidOperationException("治理政策缺少 policyId，無法驗證版本核准資訊。");
        }

        if (string.IsNullOrWhiteSpace(document.Version) || !SemanticVersionPattern.IsMatch(document.Version.Trim()))
        {
            var versionDisplay = string.IsNullOrWhiteSpace(document.Version) ? "<null>" : document.Version.Trim();
            throw new InvalidOperationException(
                $"治理政策版本格式錯誤：{versionDisplay}。需為 MAJOR.MINOR.PATCH。");
        }

        var policyVersion = Version.Parse(document.Version.Trim());
        if (policyVersion.Major != SupportedPolicyMajorVersion)
        {
            throw new InvalidOperationException(
                $"治理政策版本不受支援：{document.Version.Trim()}。目前僅支援 major {SupportedPolicyMajorVersion}.x.x。");
        }

        if (string.IsNullOrWhiteSpace(document.ApprovedBy))
        {
            throw new InvalidOperationException("治理政策缺少 approvedBy，請補齊核准者資訊。");
        }

        if (document.ApprovedAt is null)
        {
            throw new InvalidOperationException("治理政策缺少 approvedAt，請補齊核准時間。");
        }

        if (document.EffectiveFrom is null)
        {
            throw new InvalidOperationException("治理政策缺少 effectiveFrom，請補齊生效日期。");
        }

        var effectiveFrom = document.EffectiveFrom.Value;
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        if (effectiveFrom > todayUtc)
        {
            throw new InvalidOperationException(
                $"治理政策尚未生效：effectiveFrom={effectiveFrom:yyyy-MM-dd}。請等待生效或調整生效日期。");
        }

        var approvedAt = document.ApprovedAt.Value;
        if (approvedAt > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            throw new InvalidOperationException(
                $"治理政策核准時間不合理：approvedAt={approvedAt:O}。請確認時間設定。");
        }

        return new TrainingGovernancePolicyApprovalManifest(
            document.PolicyId.Trim(),
            document.Version.Trim(),
            document.ApprovedBy.Trim(),
            approvedAt,
            effectiveFrom);
    }

    private sealed record GovernancePolicyDocument(
        string? PolicyId,
        string? Version,
        string? Status,
        DateOnly? EffectiveFrom,
        string? ApprovedBy,
        DateTimeOffset? ApprovedAt);
}

internal sealed record TrainingGovernancePolicyApprovalManifest(
    string PolicyId,
    string Version,
    string ApprovedBy,
    DateTimeOffset ApprovedAt,
    DateOnly EffectiveFrom);
