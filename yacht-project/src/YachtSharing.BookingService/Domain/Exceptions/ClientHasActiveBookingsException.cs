using System;

namespace YachtSharing.BookingService.Domain.Exceptions;

public sealed class ClientHasActiveBookingsException : DomainException
{
    public override int StatusCode => 409;

    public ClientHasActiveBookingsException(Guid clientId)
        : base($"Client with ID {clientId} cannot be deleted while active bookings exist.")
    {
    }
}
