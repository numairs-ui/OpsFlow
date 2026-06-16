using MediatR;

namespace OpsFlow.Api.Features.Checklists.GetChecklists;

internal sealed record GetChecklistsQuery(
    string? Scope = null,
    bool? IsActive = null,
    string? Search = null) : IRequest<List<ChecklistDto>>;

internal sealed record ChecklistDto(
    Guid Id, string Name, string? Description, string Scope,
    Guid? RegionId, string? RegionName, Guid? StoreId, string? StoreName,
    int ItemCount, List<string> FirstThreeTemplateNames,
    bool IsActive, DateTimeOffset CreatedAt);
