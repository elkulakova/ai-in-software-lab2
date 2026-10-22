using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.Repositories;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Infrastructure.Persistence.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly BookingDbContext _context;

    public ClientRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<UserClient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<UserClient?> GetByNameAndEmailAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        return await _context.Clients.FirstOrDefaultAsync(
            c => c.Name == name && c.Email == email,
            cancellationToken);
    }

    public void Add(UserClient client)
    {
        _context.Clients.Add(client);
    }
}