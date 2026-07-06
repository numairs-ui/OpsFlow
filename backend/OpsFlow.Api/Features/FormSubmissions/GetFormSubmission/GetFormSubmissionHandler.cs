using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.FormSubmissions.GetFormSubmission;

internal sealed class GetFormSubmissionHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetFormSubmissionQuery, FormSubmissionDetailDto>
{
    public async Task<FormSubmissionDetailDto> Handle(GetFormSubmissionQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var s = await db.FormSubmissions
            .Include(x => x.FormTemplate)
            .Include(x => x.Store)
            .Include(x => x.ApprovalSteps)
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct)
            ?? throw new KeyNotFoundException($"Form submission {query.Id} not found.");

        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == user.GetUserId() && a.StoreId == s.StoreId, ct);
        spec.AssertCanViewStore(s.Store!.RegionId, s.StoreId, assigned);

        return new FormSubmissionDetailDto(
            s.Id, s.FormTemplateId, s.FormTemplate?.Name, s.FormTemplate?.FieldsJson,
            s.StoreId, s.Store?.Name, s.SubmittedByUserId, s.Status, s.CurrentStepOrder,
            s.FieldValuesJson, s.CreatedAt, s.SubmittedAt, s.ResolvedAt,
            s.ApprovalSteps.OrderBy(a => a.StepOrder).ThenBy(a => a.ActionAt)
                .Select(a => new ApprovalStepDto(a.StepOrder, a.Role, a.ActionByUserId, a.Action, a.Comments, a.ActionAt))
                .ToList());
    }
}
