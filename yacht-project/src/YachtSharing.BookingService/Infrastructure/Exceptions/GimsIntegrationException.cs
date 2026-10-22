using System;

namespace YachtSharing.BookingService.Infrastructure.Exceptions;

public class GimsIntegrationException : Exception
{
    public GimsIntegrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}