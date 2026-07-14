using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Checklists;

/// <summary>A submitted score for one scored checklist item, keyed by the item's template id.</summary>
public sealed record ItemScore(Guid TemplateId, int Score);

/// <summary>A failed scored item whose configured corrective action should spawn a follow-up task (A4).</summary>
public sealed record ChecklistFailure(Guid TemplateId, string CorrectiveActionText);

/// <summary>
/// Pure checklist-session scoring (modeled on <see cref="OpsFlow.Domain.Forms.ApprovalWorkflow"/>): no DB
/// access, operates on already-loaded items + submitted scores. The handler owns load/save/broadcast.
/// A scored item is one with ScoringType "PassFail" or "Scale1To5"; unscored items don't affect the score.
/// </summary>
public static class ChecklistScoring
{
    public const string PassFail = "PassFail";
    public const string Scale1To5 = "Scale1To5";

    public static bool IsScored(ChecklistTemplateItem item) => item.ScoringType is PassFail or Scale1To5;

    /// <summary>
    /// Weighted average of each scored item's percent (Pass/Fail → 100/0; 1–5 → score/5·100), weighted by
    /// <see cref="ChecklistTemplateItem.Weight"/>. Returns null when no scored item has a submitted score.
    /// </summary>
    public static decimal? ComputeCompositeScore(
        IEnumerable<ChecklistTemplateItem> items, IReadOnlyCollection<ItemScore> scores)
    {
        decimal weightedSum = 0m, totalWeight = 0m;
        foreach (var item in items.Where(IsScored))
        {
            var score = scores.FirstOrDefault(s => s.TemplateId == item.TemplateId);
            if (score is null) continue;

            var weight = item.Weight <= 0 ? 1m : item.Weight;
            weightedSum += ItemPercent(item.ScoringType!, score.Score) * weight;
            totalWeight += weight;
        }

        return totalWeight == 0m ? null : Math.Round(weightedSum / totalWeight, 1);
    }

    /// <summary>
    /// Scored items that failed AND carry a corrective-action text: Pass/Fail items scored as fail, or
    /// 1–5 items scored at or below their FailScoreThreshold. (Items with no corrective text don't spawn
    /// a follow-up, so they're not returned here.)
    /// </summary>
    public static List<ChecklistFailure> DetermineFailures(
        IEnumerable<ChecklistTemplateItem> items, IReadOnlyCollection<ItemScore> scores)
    {
        var failures = new List<ChecklistFailure>();
        foreach (var item in items.Where(IsScored))
        {
            var score = scores.FirstOrDefault(s => s.TemplateId == item.TemplateId);
            if (score is null) continue;

            var failed = item.ScoringType == PassFail
                ? score.Score < 1
                : item.FailScoreThreshold is { } threshold && score.Score <= threshold;

            if (failed && !string.IsNullOrWhiteSpace(item.FailCorrectiveActionText))
                failures.Add(new ChecklistFailure(item.TemplateId, item.FailCorrectiveActionText!));
        }
        return failures;
    }

    private static decimal ItemPercent(string scoringType, int score) => scoringType switch
    {
        PassFail => score >= 1 ? 100m : 0m,
        Scale1To5 => Math.Clamp(score, 0, 5) / 5m * 100m,
        _ => 0m,
    };
}
