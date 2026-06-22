using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.UpdateDraft;

internal sealed record UpdateDraftCommand(
    Guid Id,
    Dictionary<string, string> FieldValues) : IRequest;
