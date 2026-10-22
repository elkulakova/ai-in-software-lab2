using System;

namespace YachtSharing.BookingService.Domain.Exceptions;

public sealed class ExpiredInsuranceException : DomainException
{
    public override int StatusCode => 400;

    public ExpiredInsuranceException(string memberName, DateTime expiryDate)
        : base($"Crew member '{memberName}' has expired or invalid insurance. Expiry date: {expiryDate:yyyy-MM-dd}. Insurance must cover the entire booking period.")
    {
    }
}