using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Text.Json;

namespace OpsFlow.Api.Features.FormSubmissions.CreateFormSubmission;

internal sealed class CreateFormSubmissionHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateFormSubmissionCommand, Guid>
{
    public async Task<Guid> Handle(CreateFormSubmissionCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.GetTenantId();
        var userId = user.GetUserId();
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        // Auth: a submission is a store-level action — the caller must be scoped to its store
        // (own/assigned store for store roles, region set for region roles, super_admin always).
        var store = await db.Stores
            .Where(s => s.Id == cmd.StoreId)
            .Select(s => new { s.RegionId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Store {cmd.StoreId} not found.");
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == userId && a.StoreId == cmd.StoreId, ct);
        spec.AssertCanViewStore(store.RegionId, cmd.StoreId, assigned);

        var submission = new FormSubmission
        {
            TenantId = tenantId,
            FormTemplateId = cmd.FormTemplateId,
            StoreId = cmd.StoreId,
            SubmittedByUserId = userId,
            Status = "Draft",
            FieldValuesJson = JsonSerializer.Serialize(cmd.FieldValues),
        };

        db.FormSubmissions.Add(submission);
        await db.SaveChangesAsync(ct);
        return submission.Id;
    }
}
