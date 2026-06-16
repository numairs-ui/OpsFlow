using MediatR;

namespace OpsFlow.Api.Features.Health.GetStatus;

public sealed record GetStatusQuery : IRequest<GetStatusResponse>;

public sealed record GetStatusResponse(string Status, string Environment, DateTimeOffset Timestamp);
