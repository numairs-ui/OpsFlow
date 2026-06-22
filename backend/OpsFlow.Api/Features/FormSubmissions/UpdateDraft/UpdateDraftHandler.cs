using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;
using System.Text.Json;

namespace OpsFlow.Api.Features.FormSubmissions.UpdateDraft;

internal sealed class UpdateDraftHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateDraftCommand>
{
    public async Task Handle(UpdateDraftCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        await using var db = await factory.CreateAsync(ct);

        var submission = await db.FormSubmissions
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException($"Form submission {cmd.Id} not found.");

        if (submission.SubmittedByUserId != userId)
            throw new UnauthorizedAccessException("You do not own this submission.");

        if (submission.Status != "Draft")
            throw new InvalidOperationException($"Cannot update a submission in status '{submission.Status}'.");

        submission.FieldValuesJson = JsonSerializer.Serialize(cmd.FieldValues);
        await db.SaveChangesAsync(ct);
    }
}
