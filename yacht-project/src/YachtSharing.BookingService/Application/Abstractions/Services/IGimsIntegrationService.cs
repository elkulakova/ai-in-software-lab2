using System.Threading;
using System.Threading.Tasks;

namespace YachtSharing.BookingService.Application.Abstractions.Services;

public interface IGimsIntegrationService
{
    Task<bool> VerifySkipperLicenseAsync(string series, string number, CancellationToken cancellationToken = default);
}