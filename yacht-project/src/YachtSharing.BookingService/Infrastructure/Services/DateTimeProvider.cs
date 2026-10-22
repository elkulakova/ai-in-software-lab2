using System;
using YachtSharing.BookingService.Application.Abstractions.Services;

namespace YachtSharing.BookingService.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow);
}