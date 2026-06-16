using MediatR;

namespace OpsFlow.Api.Features.Users.GetStoreAssignments;

internal sealed record GetStoreAssignmentsQuery(string UserId) : IRequest<List<StoreAssignmentDto>>;

internal sealed record StoreAssignmentDto(Guid StoreId, string StoreName, string? RegionName, DateTimeOffset AssignedAt);
