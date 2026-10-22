using System;

namespace YachtSharing.BookingService.Application.Abstractions.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateOnly UtcToday { get; }
}