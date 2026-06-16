using MediatR;

namespace OpsFlow.Api.Features.Auth.Logout;

internal sealed record LogoutCommand : IRequest;
