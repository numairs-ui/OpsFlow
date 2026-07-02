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
                }
                // Checklist and Form types to be handled by respective domains
                created++;
            }
            catch (Exception ex)
            {
                failed.Add(new ImportFailure(i, [$"Unexpected error: {ex.Message}"]));
            }
        }

        if (created > 0) await db.SaveChangesAsync(ct);
        return new ImportTemplatesResult(created, failed);
    }

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
        return errors;
    }
}
