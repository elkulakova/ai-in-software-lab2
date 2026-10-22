using System;

namespace YachtSharing.BookingService.Domain.Exceptions;

public sealed class PaymentTimeoutException : DomainException
{
    public override int StatusCode => 408;

    public PaymentTimeoutException(Guid bookingId)
        : base($"Booking window for ID {bookingId} has expired. The 1-hour payment limit was exceeded.")
    {
    }
}