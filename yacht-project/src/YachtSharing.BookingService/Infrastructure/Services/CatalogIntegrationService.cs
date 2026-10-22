using Catalog;
using Grpc.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.Services;
using YachtSharing.BookingService.Infrastructure.Exceptions;

namespace YachtSharing.BookingService.Infrastructure.Services;

public class CatalogIntegrationService : ICatalogIntegrationService
{
    private readonly Catalog.Catalog.CatalogClient _grpcClient;

    public CatalogIntegrationService(Catalog.Catalog.CatalogClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    public async Task<decimal> GetYachtPriceIfAvailableAsync(
        int yachtId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var request = new AvailabilityRequest
        {
            YachtId = yachtId,
            StartTime = startDate.ToString("o"),
            EndTime = endDate.ToString("o"),
        };

        try
        {
            AvailabilityResponse response = await _grpcClient.CheckYachtAvailabilityAsync(request, cancellationToken: cancellationToken);
            return response.IsAvailable ? (decimal)response.PricePerHour : 0m;
        }
        catch (RpcException ex)
        {
            throw new CatalogIntegrationException($"Catalog gRPC error: {ex.Status.Detail}", ex);
        }
    }

    public async Task<bool> RegisterBookingAsync(RegisterBookingRequest request)
    {
        RegisterBookingResponse response = await _grpcClient.RegisterBookingProjectionAsync(request);

        return response.IsSuccess;
    }
}