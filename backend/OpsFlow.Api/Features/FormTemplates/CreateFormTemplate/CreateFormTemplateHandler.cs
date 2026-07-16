using MediatR;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Text.Json;

namespace OpsFlow.Api.Features.FormTemplates.CreateFormTemplate;

internal sealed class CreateFormTemplateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateFormTemplateCommand, Guid>
{
    // Frontend reads approvalStepsJson with a plain JSON.parse (no case-insensitivity), so the
    // stored JSON must use camelCase property names, not the default PascalCase C# serialization.
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Guid> Handle(CreateFormTemplateCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.GetTenantId();
        var userId = user.GetUserId();

        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        await spec.AssertCanWriteScopeAsync(db, cmd.Scope, cmd.RegionId, cmd.StoreId, ct);

        var template = new FormTemplate
        {
            TenantId = tenantId,
            Name = cmd.Name,
            Description = cmd.Description,
            Scope = cmd.Scope,
            RegionId = cmd.RegionId,
            StoreId = cmd.StoreId,
            PropagationType = cmd.PropagationType,
            ApprovalStepsJson = JsonSerializer.Serialize(cmd.ApprovalSteps, JsonOptions),
            FieldsJson = cmd.FieldsJson,
            CreatedByUserId = userId,
        };

        db.FormTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template.Id;
    }
}
