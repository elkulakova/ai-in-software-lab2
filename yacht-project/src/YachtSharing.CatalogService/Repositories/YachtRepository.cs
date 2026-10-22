using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YachtSharing.CatalogService.DataTable;
using YachtSharing.CatalogService.Entities;

namespace YachtSharing.CatalogService.Repositories;

public class YachtRepository : IYachtRepository
{
    private readonly CatalogDbContext _context;

    public YachtRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<List<Yacht>> GetAvailableInInterval(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _context.Yachts
        .Where(yacht => _context.Bookings.Count(booking =>
            booking.YachtId == yacht.Id &&
            booking.StartTime < endTime &&
            booking.EndTime > startTime) == 0)
        .ToListAsync();
    }

    public async Task<Yacht?> GetById(int id)
    {
        return await _context.Yachts.FindAsync(id);
    }

    public async Task AddBookingProjection(BookingProjection projection)
    {
        _context.Bookings.Add(projection);
        await _context.SaveChangesAsync();
    }
}
