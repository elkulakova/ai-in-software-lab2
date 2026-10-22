using System;

namespace YachtSharing.BookingService.Domain.Exceptions;

public sealed class InvalidWorkingHoursException : DomainException
{
    public override int StatusCode => 400;

    public InvalidWorkingHoursException(DateTime start, DateTime end)
        : base($"INVALID_WORKING_HOURS: The yacht club is open strictly from 10:00 to 21:00. Your request ({start:HH:mm} - {end:HH:mm}) falls outside of working hours.")
    {
    }
}