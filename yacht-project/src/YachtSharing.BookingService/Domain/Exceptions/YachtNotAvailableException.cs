using System;

namespace YachtSharing.BookingService.Domain.Exceptions;

public sealed class YachtNotAvailableException : DomainException
{
    public override int StatusCode => 409;

    public YachtNotAvailableException(int yachtId, DateTime start, DateTime end)
        : base($"Yacht with ID {yachtId} is already booked from {start:s} to {end:s}.")
    {
    }
}