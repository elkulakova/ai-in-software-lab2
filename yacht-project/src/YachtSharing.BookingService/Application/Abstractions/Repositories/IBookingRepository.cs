using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Application.Abstractions.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Booking booking);

    void Update(Booking booking);

    Task<IReadOnlyList<Booking>> GetExpiredPendingBookingsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);

    Task<bool> HasActiveBookingsForClientAsync(Guid clientId, CancellationToken cancellationToken = default);
}