using MediatR;
using OpsFlow.Api.Features.DepositLog.RecordDeposit;

namespace OpsFlow.Api.Features.DepositLog.GetDepositByDate;

internal sealed record GetDepositByDateQuery(Guid StoreId, DateOnly Date) : IRequest<DepositLogDto?>;
