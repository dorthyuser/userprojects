using _123_sadsa_1232.Models;

namespace _123_sadsa_1232.Services;

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetAllAsync(string correlationId, CancellationToken cancellationToken);
    Task<PaymentDto?> GetByIdAsync(long id, string correlationId, CancellationToken cancellationToken);
    Task<PaymentDto> CreateAsync(PaymentUpsertRequest request, string correlationId, CancellationToken cancellationToken);
    Task<PaymentDto?> UpdateAsync(long id, PaymentUpsertRequest request, string correlationId, CancellationToken cancellationToken);
}