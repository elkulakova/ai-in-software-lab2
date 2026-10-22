using System.Threading;
using System.Threading.Tasks;

namespace YachtSharing.BookingService.Application.Abstractions.Repositories;

public interface IUnitOfWork
{
    IBookingRepository BookingRepository { get; }

    IPaymentRepository PaymentRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}