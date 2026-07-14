using MediatR;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Templates.ImportTemplates;

internal sealed class ImportTemplatesHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<ImportTemplatesCommand, ImportTemplatesResult>
{
    private static readonly string[] ValidScopes = ["System", "Regional", "Store"];
    private static readonly string[] ValidTypes = ["Task", "Checklist"];

    public async Task<ImportTemplatesResult> Handle(ImportTemplatesCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.FindFirstValue("tenantId")!;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        if (!Roles.IsSuperAdmin(role)) throw new UnauthorizedAccessException("Super admin role required.");

        await using var db = await factory.CreateAsync(ct);

        int created = 0;
        var failed = new List<ImportFailure>();

        for (int i = 0; i < cmd.Templates.Count; i++)
        {
            var item = cmd.Templates[i];
            var errors = Validate(item);

            if (errors.Count > 0)
            {
                failed.Add(new ImportFailure(i, errors));
                continue;
            }

            try
            {
                if (item.Type == "Task")
                {
                    db.TaskTemplates.Add(new TaskTemplate
                    {
                        TenantId = tenantId,
                        Name = item.Name,
                        Description = item.Description,
                        Category = item.Category,
                        Scope = item.Scope,
                        RegionId = item.RegionId,
                        StoreId = item.StoreId,
                        FieldsJson = item.FieldsJson ?? "[]",
                        CreatedByUserId = userId,
                    });
                    created++; // count only rows that actually persist something
                }
                else if (item.Type == "Checklist")
                {
                    ImportChecklist(db, item, tenantId, userId);
                    created++;
                }
                // Form has its own creation flow — Validate() rejects it before reaching here.
            }
            catch (Exception ex)
            {
                failed.Add(new ImportFailure(i, [$"Unexpected error: {ex.Message}"]));
            }
        }

        if (created > 0) await db.SaveChangesAsync(ct);
        return new ImportTemplatesResult(created, failed);
    }

    /// <summary>
    /// Materialize a Checklist import row into one Checklist + one TaskTemplate per sub-item + the
    /// corresponding scored ChecklistTemplateItem rows — the shape a scored checklist actually needs,
    /// rather than the old "one flat template with N crammed fields."
    /// </summary>
    private static void ImportChecklist(TenantDbContext db, ImportTemplateItem item, string tenantId, string userId)
    {
        var checklist = new Checklist
        {
            TenantId = tenantId,
            Name = item.Name,
            Description = item.Description,
            Scope = item.Scope,
            RegionId = item.RegionId,
            StoreId = item.StoreId,
            IsActive = true,
            CreatedByUserId = userId,
        };
        db.Checklists.Add(checklist);

        var order = 0;
        foreach (var sub in item.Items!)
        {
            var template = new TaskTemplate
            {
                TenantId = tenantId,
                Name = sub.Name,
                Description = sub.Description,
                Category = sub.Category ?? item.Category,
                Scope = item.Scope,
                RegionId = item.RegionId,
                StoreId = item.StoreId,
                FieldsJson = sub.FieldsJson ?? "[]",
                CreatedByUserId = userId,
            };
            db.TaskTemplates.Add(template);

            db.ChecklistTemplateItems.Add(new ChecklistTemplateItem
            {
                ChecklistId = checklist.Id,
                TemplateId = template.Id,
                Order = sub.Order != 0 ? sub.Order : order,
                ScoringType = sub.ScoringType,
                Weight = sub.Weight,
                PhotoRequired = sub.PhotoRequired,
                FailCorrectiveActionText = sub.FailCorrectiveActionText,
                FailScoreThreshold = sub.ScoringType == "Scale1To5" ? sub.FailScoreThreshold : null,
            });
            order++;
        }
    }

    private static readonly string[] AllowedScoringTypes = ["PassFail", "Scale1To5"];

    private static List<string> Validate(ImportTemplateItem item)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(item.Name)) errors.Add("Name is required.");
        if (item.Name?.Length > 200) errors.Add("Name must be 200 characters or fewer.");
        if (string.IsNullOrWhiteSpace(item.Category)) errors.Add("Category is required.");
        if (!ValidTypes.Contains(item.Type)) errors.Add($"Type must be one of: {string.Join(", ", ValidTypes)}.");
        if (!ValidScopes.Contains(item.Scope)) errors.Add($"Scope must be one of: {string.Join(", ", ValidScopes)}.");
        if (item.Scope == "Regional" && item.RegionId == null) errors.Add("RegionId is required for Regional scope.");
        if (item.Scope == "Store" && item.StoreId == null) errors.Add("StoreId is required for Store scope.");

        if (item.Type == "Checklist")
        {
            if (item.Items is not { Count: > 0 })
                errors.Add("A Checklist import must include at least one item.");
            else
            {
                foreach (var sub in item.Items)
                {
                    if (string.IsNullOrWhiteSpace(sub.Name)) errors.Add("Each checklist item needs a name.");
                    if (sub.ScoringType is not null && !AllowedScoringTypes.Contains(sub.ScoringType))
                        errors.Add($"Item '{sub.Name}': ScoringType must be one of {string.Join(", ", AllowedScoringTypes)} (or null).");
                    if (sub.FailScoreThreshold is { } t)
                    {
                        if (sub.ScoringType != "Scale1To5")
                            errors.Add($"Item '{sub.Name}': FailScoreThreshold is only valid for Scale1To5.");
                        else if (t is < 1 or > 5)
                            errors.Add($"Item '{sub.Name}': FailScoreThreshold must be between 1 and 5.");
                    }
                }
            }
        }

        return errors;
    }
}
