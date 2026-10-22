using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.Repositories;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Infrastructure.Persistence.Repositories;

public class CrewRepository : ICrewRepository
{
    private readonly BookingDbContext _context;

    public CrewRepository(BookingDbContext context)
    {
        _context = context;
    }

    public void AddRange(IEnumerable<CrewMember> members)
    {
        _context.CrewMembers.AddRange(members);
    }

    public async Task<IReadOnlyList<CrewMember>> GetExpiredCrewAsync(DateTime thresholdDate, CancellationToken cancellationToken = default)
    {
        return await _context.CrewMembers
            .Where(cm => cm.InsuranceExpiry < thresholdDate)
            .ToListAsync(cancellationToken);
    }

    public void RemoveRange(IEnumerable<CrewMember> members)
    {
        _context.CrewMembers.RemoveRange(members);
    }
}