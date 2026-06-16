using MediatR;

namespace OpsFlow.Api.Features.Templates.UpdateTemplate;

internal sealed record UpdateTemplateCommand(
    Guid Id,
    string Name,
    string? Description,
    string Category,
    string? FieldsJson) : IRequest;
