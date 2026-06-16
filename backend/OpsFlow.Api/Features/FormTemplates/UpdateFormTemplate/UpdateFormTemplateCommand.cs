using MediatR;
using OpsFlow.Api.Features.FormTemplates.CreateFormTemplate;

namespace OpsFlow.Api.Features.FormTemplates.UpdateFormTemplate;

internal sealed record UpdateFormTemplateCommand(
    Guid Id,
    string Name,
    string? Description,
    string PropagationType,
    List<ApprovalStepInput> ApprovalSteps,
    string? FieldsJson) : IRequest;
