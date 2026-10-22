namespace YachtSharing.BookingService.Domain.Exceptions;

public sealed class GimsVerificationFailedException : DomainException
{
    public override int StatusCode => 403;

    public GimsVerificationFailedException(string series, string number)
        : base($"Skipper license verification failed for passport {series} {number}. Access denied by GIMS registry.")
    {
    }
}