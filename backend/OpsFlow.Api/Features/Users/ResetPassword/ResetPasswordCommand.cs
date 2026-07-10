using MediatR;

namespace OpsFlow.Api.Features.Users.ResetPassword;

/// <summary>
/// Admin-triggered password reset. When <paramref name="NewPassword"/> is null a random
/// temp password is generated. The resulting temp password is returned exactly once and is
/// never logged or persisted.
/// </summary>
internal sealed record ResetPasswordCommand(string UserId, string? NewPassword)
    : IRequest<ResetPasswordResponse>;

internal sealed record ResetPasswordResponse(string TempPassword);
