using MediatR;

namespace OpsFlow.Api.Features.Users.RemoveStoreAssignment;

internal sealed record RemoveStoreAssignmentCommand(string UserId, Guid StoreId) : IRequest;
