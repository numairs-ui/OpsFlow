using MediatR;

namespace OpsFlow.Api.Features.Tasks.GetPhotoUploadUrl;

/// <summary>
/// Requests a short-lived signed URL the client can PUT a photo directly to, bypassing the API.
/// The returned <see cref="GetPhotoUploadUrlResponse.BlobUrl"/> becomes the Photo field's value,
/// submitted with the rest of the completion like any other field.
/// </summary>
internal sealed record GetPhotoUploadUrlCommand(Guid TaskId, string TemplateId, string FieldId)
    : IRequest<GetPhotoUploadUrlResponse>;

internal sealed record GetPhotoUploadUrlResponse(string UploadUrl, string BlobUrl);
