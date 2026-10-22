using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.Repositories;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly BookingDbContext _context;

    public PaymentRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
    }

    public void Add(Payment payment)
    {
        _context.Payments.Add(payment);
    }
}