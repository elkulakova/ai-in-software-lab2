using YachtSharing.BookingService.Domain.Enums;

namespace YachtSharing.BookingService.Domain.Exceptions;

public sealed class InvalidBookingStatusTransitionException : DomainException
{
    public override int StatusCode => 400;

    public InvalidBookingStatusTransitionException(BookingStatus currentStatus, BookingStatus targetStatus)
        : base($"INVALID_STATUS_TRANSITION: Cannot transition booking from state '{currentStatus}' to '{targetStatus}'. Operation is not allowed in the current state.")
    {
    }
}