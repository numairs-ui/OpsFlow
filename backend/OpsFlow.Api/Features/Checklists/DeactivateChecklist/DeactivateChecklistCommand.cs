using MediatR;

namespace OpsFlow.Api.Features.Checklists.DeactivateChecklist;

internal sealed record DeactivateChecklistCommand(Guid Id, bool Activate = false) : IRequest;
