using MediatR;

namespace OpsFlow.Api.Features.Templates.DeactivateTemplate;

internal sealed record DeactivateTemplateCommand(Guid Id, bool Activate = false) : IRequest;
