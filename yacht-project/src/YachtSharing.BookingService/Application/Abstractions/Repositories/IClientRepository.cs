using System;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Application.Abstractions.Repositories;

public interface IClientRepository
{
    Task<UserClient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserClient?> GetByNameAndEmailAsync(string name, string email, CancellationToken cancellationToken = default);

    void Add(UserClient client);
}