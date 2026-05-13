using test_capi_1111123323333.Models;

namespace test_capi_1111123323333.Services;

public interface IOrdersService
{
    Task<IEnumerable<OrderResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse?> UpdateAsync(Guid orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid orderId, CancellationToken cancellationToken = default);
}