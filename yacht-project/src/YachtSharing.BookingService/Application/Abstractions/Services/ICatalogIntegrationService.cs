using Catalog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace YachtSharing.BookingService.Application.Abstractions.Services;

public interface ICatalogIntegrationService
{
    Task<decimal> GetYachtPriceIfAvailableAsync(int yachtId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<bool> RegisterBookingAsync(RegisterBookingRequest request);
}