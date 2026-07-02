using MediatR;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Templates.CreateTemplate;

internal sealed class CreateTemplateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateTemplateCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.GetTenantId();
        var userId = user.GetUserId();

        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        await spec.AssertCanWriteScopeAsync(db, cmd.Scope, cmd.RegionId, cmd.StoreId, ct);

        var template = new TaskTemplate
        {
            TenantId = tenantId,
            Name = cmd.Name,
            Description = cmd.Description,
            Category = cmd.Category,
            Scope = cmd.Scope,
            RegionId = cmd.RegionId,
            StoreId = cmd.StoreId,
            FieldsJson = cmd.FieldsJson,
            CreatedByUserId = userId,
        };

        db.TaskTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template.Id;
    }
}
