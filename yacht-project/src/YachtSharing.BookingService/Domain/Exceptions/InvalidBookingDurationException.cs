namespace YachtSharing.BookingService.Domain.Exceptions;

public sealed class InvalidBookingDurationException : DomainException
{
    public override int StatusCode => 400;

    public InvalidBookingDurationException(double requestedHours)
        : base($"INVALID_BOOKING_DURATION: Minimum rental period is 1 hour. Your requested duration was {requestedHours:F2} hours.")
    {
    }
}