using MediatR;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;
using System.Text.Json;

namespace OpsFlow.Api.Features.FormTemplates.UpdateFormTemplate;

internal sealed class UpdateFormTemplateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateFormTemplateCommand>
{
    public async Task Handle(UpdateFormTemplateCommand cmd, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);
        var template = await db.FormTemplates.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Form template {cmd.Id} not found.");

        await spec.AssertCanWriteScopeAsync(db, template.Scope, template.RegionId, template.StoreId, ct);

        template.Name = cmd.Name;
        template.Description = cmd.Description;
        template.PropagationType = cmd.PropagationType;
        template.ApprovalStepsJson = JsonSerializer.Serialize(cmd.ApprovalSteps);
        if (cmd.FieldsJson != null) template.FieldsJson = cmd.FieldsJson;

        await db.SaveChangesAsync(ct);
    }
}
