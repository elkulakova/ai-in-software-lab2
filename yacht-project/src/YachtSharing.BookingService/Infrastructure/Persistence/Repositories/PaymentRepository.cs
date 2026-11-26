using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public async Task<IReadOnlyList<Payment>> GetByBookingIdsAsync(
        IReadOnlyList<Guid> bookingIds,
        CancellationToken cancellationToken = default)
    {
        if (bookingIds.Count == 0)
        {
            return Array.Empty<Payment>();
        }

        return await _context.Payments
            .Where(payment => bookingIds.Contains(payment.BookingId))
            .ToListAsync(cancellationToken);
    }

    public void Add(Payment payment)
    {
        _context.Payments.Add(payment);
    }

    public void RemoveRange(IEnumerable<Payment> payments)
    {
        _context.Payments.RemoveRange(payments);
    }
}