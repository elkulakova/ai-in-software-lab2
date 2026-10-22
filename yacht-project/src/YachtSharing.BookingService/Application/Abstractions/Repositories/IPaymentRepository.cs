using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Application.Abstractions.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);

    void Add(Payment payment);
}