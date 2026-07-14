using MediatR;

namespace OpsFlow.Api.Features.Checklists.UpdateItems;

internal sealed record UpdateItemsCommand(Guid ChecklistId, List<ItemInput> Items) : IRequest;

internal sealed record ItemInput(
    Guid TemplateId,
    int Order,
    string? ScoringType = null,
    decimal Weight = 1.0m,
    bool PhotoRequired = false,
    string? FailCorrectiveActionText = null,
    int? FailScoreThreshold = null);
