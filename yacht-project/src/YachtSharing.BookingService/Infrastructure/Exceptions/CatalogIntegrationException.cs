using System;

namespace YachtSharing.BookingService.Infrastructure.Exceptions;

public class CatalogIntegrationException : Exception
{
    public CatalogIntegrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}