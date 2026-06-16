using MediatR;

namespace OpsFlow.Api.Features.Users.AddStoreAssignment;

internal sealed record AddStoreAssignmentCommand(string UserId, Guid StoreId) : IRequest;
