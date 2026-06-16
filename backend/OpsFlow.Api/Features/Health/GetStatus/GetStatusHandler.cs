using MediatR;

namespace OpsFlow.Api.Features.Health.GetStatus;

internal sealed class GetStatusHandler(IHostEnvironment env) : IRequestHandler<GetStatusQuery, GetStatusResponse>
{
    public Task<GetStatusResponse> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        var response = new GetStatusResponse(
            Status: "ok",
            Environment: env.EnvironmentName,
            Timestamp: DateTimeOffset.UtcNow);

        return Task.FromResult(response);
    }
}
