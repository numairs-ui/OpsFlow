using MediatR;

namespace OpsFlow.Api.Features.Auth.Refresh;

// No input — the refresh token is read from the HttpOnly cookie in the endpoint
internal sealed record RefreshCommand : IRequest<RefreshResult>;

internal sealed record RefreshResult(string AccessToken, int ExpiresIn);
