using MediatR;
using OpsFlow.Api.Features.DepositLog.RecordDeposit;

namespace OpsFlow.Api.Features.DepositLog.GetDepositLog;

internal sealed record GetDepositLogQuery(
    Guid StoreId,
    DateOnly? From,
    DateOnly? To,
    int Page = 1,
    int PageSize = 20) : IRequest<GetDepositLogResponse>;

internal sealed record GetDepositLogResponse(
    List<DepositLogDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
