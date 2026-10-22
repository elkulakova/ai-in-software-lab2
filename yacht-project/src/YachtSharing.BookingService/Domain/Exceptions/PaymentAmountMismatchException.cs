using System;

namespace YachtSharing.BookingService.Domain.Exceptions;

public class PaymentAmountMismatchException : DomainException
{
    public override int StatusCode => 409;

    public PaymentAmountMismatchException(Guid bookingId, decimal expected, decimal actual)
        : base($"Payment security violation! Booking {bookingId} expects {expected} RUB, but webhook reported {actual}.")
    {
    }
}