using Catalog;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YachtSharing.CatalogService.DataTable;
using YachtSharing.CatalogService.Entities;
using YachtSharing.CatalogService.Repositories;

namespace YachtSharing.CatalogService.Grpc;

public class CatalogGrpcService : Catalog.Catalog.CatalogBase
{
    private readonly IYachtRepository _repository;

    public CatalogGrpcService(IYachtRepository repository)
    {
        _repository = repository;
    }

    public override async Task<AvailabilityResponse> CheckYachtAvailability(
        AvailabilityRequest request,
        ServerCallContext context)
    {
        var start = DateTimeOffset.Parse(request.StartTime);
        var end = DateTimeOffset.Parse(request.EndTime);

        List<Yacht> availableYachts = await _repository.GetAvailableInInterval(start, end);

        Yacht? requestedYacht = availableYachts.FirstOrDefault(y => y.Id == request.YachtId);

        var response = new AvailabilityResponse();

        if (requestedYacht != null)
        {
            response.IsAvailable = true;
            response.PricePerHour = (double)requestedYacht.PricePerHour;
        }
        else
        {
            response.IsAvailable = false;
            response.PricePerHour = 0;
        }

        response.Currency = "RUB";

        return response;
    }

    public override async Task<YachtResponse> GetYachtInfo(YachtRequest request, ServerCallContext context)
    {
        Yacht? yacht = await _repository.GetById(request.YachtId);

        if (yacht == null)
        {
            var status = new Status(StatusCode.NotFound, "Яхта ID " + request.YachtId + " не найдена");
            throw new RpcException(status);
        }

        var response = new YachtResponse
        {
            Id = yacht.Id,
            Model = yacht.Model,
            PricePerHour = (double)yacht.PricePerHour,
        };

        return response;
    }

    public override async Task<RegisterBookingResponse> RegisterBookingProjection(
        RegisterBookingRequest request,
        ServerCallContext context)
    {
        try
        {
            var projection = new BookingProjection
            {
                YachtId = request.YachtId,
                StartTime = DateTimeOffset.Parse(request.StartTime),
                EndTime = DateTimeOffset.Parse(request.EndTime),
            };

            await _repository.AddBookingProjection(projection);

            return new RegisterBookingResponse { IsSuccess = true };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Не удалось сохранить проекцию: {ex.Message}"));
        }
    }
}
