using MediatR;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Tasks.GetPhotoUploadUrl;

internal sealed class GetPhotoUploadUrlHandler(
    TenantDbContextFactory factory,
    IStorageProvider storageProvider) : IRequestHandler<GetPhotoUploadUrlCommand, GetPhotoUploadUrlResponse>
{
    // Signed URLs are short-lived: long enough to upload one photo from a phone on a slow
    // connection, short enough that a leaked URL is quickly useless.
    private static readonly TimeSpan UploadUrlTtl = TimeSpan.FromMinutes(15);

    public async Task<GetPhotoUploadUrlResponse> Handle(GetPhotoUploadUrlCommand cmd, CancellationToken ct)
    {
        // Tenant-scoped context is the same isolation the other task-mutation handlers rely on:
        // a caller can only resolve a task in their own tenant.
        await using var db = await factory.CreateAsync(ct);
        var task = await db.TaskInstances.FindAsync([cmd.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        // Deterministic-prefix, unique-suffix path: groups a task's photos together while the guid
        // keeps re-captures of the same field from colliding.
        var blobPath = $"{task.TenantId}/{task.Id}/{Sanitize(cmd.TemplateId)}-{Sanitize(cmd.FieldId)}-{Guid.NewGuid():N}.jpg";

        var result = await storageProvider.GetUploadUrlAsync(blobPath, UploadUrlTtl, ct);
        return new GetPhotoUploadUrlResponse(result.UploadUrl, result.BlobUrl);
    }

    // Field/template ids are already server-controlled, but strip anything that isn't path-safe so
    // a malformed id can never escape the tenant/task prefix.
    private static string Sanitize(string value)
    {
        var chars = value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray();
        return chars.Length > 0 ? new string(chars) : "field";
    }
}
