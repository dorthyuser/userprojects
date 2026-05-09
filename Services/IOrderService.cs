using System.Threading;
using System.Threading.Tasks;
using jkjkjkjkjkjkjkjkjkjkjkjkjkjjk.Models;

namespace jkjkjkjkjkjkjkjkjkjkjkjkjkjjk.Services
{
    public interface IOrderService
    {
        Task<OrderResponse> GetByIdAsync(string orderId, CancellationToken cancellationToken = default);
        Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
        Task<OrderResponse> UpdateAsync(string orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default);
        Task<CancelOrderResponse> CancelAsync(string orderId, CancellationToken cancellationToken = default);
    }
}