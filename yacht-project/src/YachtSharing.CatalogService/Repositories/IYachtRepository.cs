using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YachtSharing.CatalogService.DataTable;
using YachtSharing.CatalogService.Entities;

namespace YachtSharing.CatalogService.Repositories;

public interface IYachtRepository
{
    Task<List<Yacht>> GetAvailableInInterval(DateTimeOffset startTime, DateTimeOffset endTime);

    Task<Yacht?> GetById(int id);

    Task AddBookingProjection(BookingProjection projection);
}
