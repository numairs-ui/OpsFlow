using MediatR;

namespace OpsFlow.Api.Features.FormTemplates.DeactivateFormTemplate;

internal sealed record DeactivateFormTemplateCommand(Guid Id, bool Activate = false) : IRequest;
