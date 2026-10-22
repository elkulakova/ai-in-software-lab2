using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Application.Abstractions.Repositories;

public interface ICrewRepository
{
    void AddRange(IEnumerable<CrewMember> members);

    Task<IReadOnlyList<CrewMember>> GetExpiredCrewAsync(DateTime thresholdDate, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<CrewMember> members);
}