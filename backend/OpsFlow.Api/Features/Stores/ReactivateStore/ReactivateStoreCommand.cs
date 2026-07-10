using MediatR;

namespace OpsFlow.Api.Features.Stores.ReactivateStore;

internal sealed record ReactivateStoreCommand(Guid Id) : IRequest;
