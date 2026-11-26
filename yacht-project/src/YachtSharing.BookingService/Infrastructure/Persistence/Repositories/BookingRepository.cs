using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.Repositories;
using YachtSharing.BookingService.Domain.Entities;
using YachtSharing.BookingService.Domain.Enums;

namespace YachtSharing.BookingService.Infrastructure.Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(b => b.ClientId == clientId)
            .ToListAsync(cancellationToken);
    }

    public void Add(Booking booking)
    {
        _context.Bookings.Add(booking);
    }

    public void Update(Booking booking)
    {
        _context.Bookings.Update(booking);
    }

    public void RemoveRange(IEnumerable<Booking> bookings)
    {
        _context.Bookings.RemoveRange(bookings);
    }

    public async Task<IReadOnlyList<Booking>> GetExpiredPendingBookingsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatus.PendingPayment && b.CreatedAt < cutoffTime)
            .ToListAsync(cancellationToken);
    }
}