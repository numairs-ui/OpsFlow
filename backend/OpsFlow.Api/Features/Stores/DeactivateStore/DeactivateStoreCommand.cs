using MediatR;

namespace OpsFlow.Api.Features.Stores.DeactivateStore;

internal sealed record DeactivateStoreCommand(Guid Id) : IRequest;
