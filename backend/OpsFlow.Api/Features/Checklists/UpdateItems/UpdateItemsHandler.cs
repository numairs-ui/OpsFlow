using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Checklists.UpdateItems;

internal sealed class UpdateItemsHandler(TenantDbContextFactory factory)
    : IRequestHandler<UpdateItemsCommand>
{
    internal static readonly string[] AllowedScoringTypes = ["PassFail", "Scale1To5"];

    public async Task Handle(UpdateItemsCommand cmd, CancellationToken ct)
    {
        ValidateScoring(cmd.Items);

        await using var db = await factory.CreateAsync(ct);

        var checklist = await db.Checklists.FindAsync([cmd.ChecklistId], ct)
            ?? throw new KeyNotFoundException($"Checklist {cmd.ChecklistId} not found.");

        var templateIds = cmd.Items.Select(i => i.TemplateId).Distinct().ToList();
        var foundCount = await db.TaskTemplates.CountAsync(t => templateIds.Contains(t.Id) && t.IsActive, ct);
        if (foundCount != templateIds.Count)
            throw new KeyNotFoundException("One or more template IDs are invalid or inactive.");

        // Full replace — remove all existing items, add the new set
        var existing = await db.ChecklistTemplateItems
            .Where(i => i.ChecklistId == cmd.ChecklistId)
            .ToListAsync(ct);
        db.ChecklistTemplateItems.RemoveRange(existing);

        foreach (var item in cmd.Items)
        {
            db.ChecklistTemplateItems.Add(new ChecklistTemplateItem
            {
                ChecklistId = cmd.ChecklistId,
                TemplateId = item.TemplateId,
                Order = item.Order,
                ScoringType = item.ScoringType,
                Weight = item.Weight,
                PhotoRequired = item.PhotoRequired,
                FailCorrectiveActionText = item.FailCorrectiveActionText,
                // FailScoreThreshold only applies to Scale1To5 — drop it otherwise.
                FailScoreThreshold = item.ScoringType == "Scale1To5" ? item.FailScoreThreshold : null,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static void ValidateScoring(List<ItemInput> items)
    {
        var errors = new List<ValidationFailure>();
        foreach (var item in items)
        {
            if (item.ScoringType is not null && !AllowedScoringTypes.Contains(item.ScoringType))
                errors.Add(new ValidationFailure(nameof(item.ScoringType),
                    $"ScoringType must be one of: {string.Join(", ", AllowedScoringTypes)} (or null)."));

            if (item.Weight <= 0)
                errors.Add(new ValidationFailure(nameof(item.Weight), "Weight must be greater than zero."));

            if (item.FailScoreThreshold is { } threshold)
            {
                if (item.ScoringType != "Scale1To5")
                    errors.Add(new ValidationFailure(nameof(item.FailScoreThreshold),
                        "FailScoreThreshold is only valid when ScoringType is Scale1To5."));
                else if (threshold is < 1 or > 5)
                    errors.Add(new ValidationFailure(nameof(item.FailScoreThreshold),
                        "FailScoreThreshold must be between 1 and 5."));
            }
        }

        if (errors.Count > 0) throw new ValidationException(errors);
    }
}
