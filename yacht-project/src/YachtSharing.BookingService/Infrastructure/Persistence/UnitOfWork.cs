using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.Repositories;

namespace YachtSharing.BookingService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly BookingDbContext _context;

    public IBookingRepository BookingRepository { get; }

    public IPaymentRepository PaymentRepository { get; }

    public UnitOfWork(IBookingRepository bookingRepository, IPaymentRepository paymentRepository, BookingDbContext context)
    {
        BookingRepository = bookingRepository;
        PaymentRepository = paymentRepository;
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}