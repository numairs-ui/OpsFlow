using MediatR;

namespace OpsFlow.Api.Features.Stores.GetStoreEmployees;

internal sealed record GetStoreEmployeesQuery(Guid StoreId) : IRequest<List<StoreEmployeeDto>>;

internal sealed record StoreEmployeeDto(
    string UserId, string Email, string DisplayName, string Role,
    bool IsActive, DateTimeOffset CreatedAt);
