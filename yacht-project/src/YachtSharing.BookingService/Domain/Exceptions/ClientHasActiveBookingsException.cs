using System;
using System.Collections.Generic;
using YachtSharing.BookingService.Domain.Enums;

namespace YachtSharing.BookingService.Domain.Exceptions;

public sealed class ClientHasActiveBookingsException : DomainException
{
    public override int StatusCode => 409;

    public ClientHasActiveBookingsException(Guid clientId, IEnumerable<BookingStatus> activeStatuses)
        : base($"CANNOT_DELETE_CLIENT: Client '{clientId}' has active bookings with statuses: {string.Join(", ", activeStatuses)}.")
    {
    }
}

