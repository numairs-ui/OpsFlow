using MediatR;

namespace OpsFlow.Api.Features.DepositLog.RecordDeposit;

internal sealed record RecordDepositCommand(Guid StoreId, decimal Amount) : IRequest<DepositLogDto>;

internal sealed record DepositLogDto(
    Guid Id,
    Guid StoreId,
    decimal Amount,
    string SubmittedByManagerId,
    DateTimeOffset SubmittedAt);
