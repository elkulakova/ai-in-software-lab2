using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Catalog;
using YachtSharing.BookingService;
using YachtSharing.BookingService.Application.Abstractions.Services;
using YachtSharing.BookingService.Domain.Entities;
using YachtSharing.BookingService.Domain.Enums;
using YachtSharing.BookingService.Infrastructure.BackgroundServices;
using YachtSharing.BookingService.Infrastructure.Persistence;

namespace YachtSharing.IntegrationTests;

public sealed class BookingServiceE2eFactory : WebApplicationFactory<BookingApplicationMarker>
{
    private readonly string _databaseName = $"booking-e2e-{Guid.NewGuid()}";

    public BookingServiceE2eFactory()
    {
        Catalog = new StubCatalogIntegrationService();
        Gims = new StubGimsIntegrationService();
        Clock = new MutableDateTimeProvider();
    }

    public StubCatalogIntegrationService Catalog { get; }

    public StubGimsIntegrationService Gims { get; }

    public MutableDateTimeProvider Clock { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<BookingDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<BookingDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<BookingDbContext>>();
            services.RemoveAll<ICatalogIntegrationService>();
            services.RemoveAll<IGimsIntegrationService>();
            services.RemoveAll<IDateTimeProvider>();

            ServiceDescriptor? expirationWorkerDescriptor = services
                .FirstOrDefault(descriptor =>
                    descriptor.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                    descriptor.ImplementationType == typeof(BookingExpirationWorker));

            if (expirationWorkerDescriptor is not null)
            {
                services.Remove(expirationWorkerDescriptor);
            }

            services.AddDbContext<BookingDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddSingleton<ICatalogIntegrationService>(Catalog);
            services.AddSingleton<IGimsIntegrationService>(Gims);
            services.AddSingleton<IDateTimeProvider>(Clock);
        });
    }

    public async Task<Guid> SeedClientAsync(string name = "Ivan Petrov", string email = "ivan@example.com")
    {
        using IServiceScope scope = Services.CreateScope();
        BookingDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

        var client = new UserClient(name, email);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        return client.Id;
    }

    public async Task<Booking?> GetBookingAsync(Guid bookingId)
    {
        using IServiceScope scope = Services.CreateScope();
        BookingDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        return await dbContext.Bookings.FirstOrDefaultAsync(booking => booking.Id == bookingId);
    }

    public async Task<int> GetPaymentsCountAsync(Guid bookingId)
    {
        using IServiceScope scope = Services.CreateScope();
        BookingDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        return await dbContext.Payments.CountAsync(payment => payment.BookingId == bookingId);
    }

    public async Task<int> GetBookingsCountAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        BookingDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        return await dbContext.Bookings.CountAsync();
    }

    public async Task<bool> ClientExistsAsync(Guid clientId)
    {
        using IServiceScope scope = Services.CreateScope();
        BookingDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        return await dbContext.Clients.AnyAsync(client => client.Id == clientId);
    }

    public async Task SetBookingStatusAsync(Guid bookingId, BookingStatus status)
    {
        using IServiceScope scope = Services.CreateScope();
        BookingDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Booking booking = await dbContext.Bookings.FirstAsync(b => b.Id == bookingId);

        switch (status)
        {
            case BookingStatus.Cancelled:
                booking.Cancel(DateTime.UtcNow);
                break;
            case BookingStatus.Expired:
                booking.Expire();
                break;
            case BookingStatus.Confirmed:
                booking.Confirm();
                break;
            default:
                throw new InvalidOperationException($"Unsupported status seed: {status}");
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<int> GetCrewCountForBookingAsync(Guid bookingId)
    {
        using IServiceScope scope = Services.CreateScope();
        BookingDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        return await dbContext.CrewMembers.CountAsync(member => member.BookingId == bookingId);
    }
}

public sealed class StubCatalogIntegrationService : ICatalogIntegrationService
{
    public bool IsAvailable { get; set; } = true;

    public bool RegisterBookingResult { get; set; } = true;

    public decimal PricePerHour { get; set; } = 5000m;

    public Task<decimal> GetYachtPriceIfAvailableAsync(
        int yachtId,
        DateTime startDate,
        DateTime endDate,
        System.Threading.CancellationToken cancellationToken = default)
    {
        decimal result = IsAvailable ? PricePerHour : 0m;
        return Task.FromResult(result);
    }

    public Task<bool> RegisterBookingAsync(RegisterBookingRequest request)
    {
        return Task.FromResult(RegisterBookingResult);
    }
}

public sealed class StubGimsIntegrationService : IGimsIntegrationService
{
    public bool IsValid { get; set; } = true;

    public Task<bool> VerifySkipperLicenseAsync(
        string series,
        string number,
        System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsValid);
    }
}

public sealed class MutableDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;

    public DateOnly UtcToday => DateOnly.FromDateTime(UtcNow);
}
