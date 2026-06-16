using MediatR;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;
using System.Text.Json;

namespace OpsFlow.Api.Features.FormSubmissions.CreateFormSubmission;

internal sealed class CreateFormSubmissionHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateFormSubmissionCommand, Guid>
{
    public async Task<Guid> Handle(CreateFormSubmissionCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.FindFirstValue("tenantId")!;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        await using var db = await factory.CreateAsync(ct);

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
