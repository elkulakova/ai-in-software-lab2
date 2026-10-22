using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.Repositories;
using YachtSharing.BookingService.Application.Abstractions.Services;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Infrastructure.BackgroundServices;

public class BookingExpirationWorker : BackgroundService
{
    private static readonly TimeSpan CheckPeriod = TimeSpan.FromMinutes(2);
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingExpirationWorker> _logger;

    public BookingExpirationWorker(IServiceProvider serviceProvider, ILogger<BookingExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Booking Expiration Worker registered and started running.");

        using var timer = new PeriodicTimer(CheckPeriod);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredBookingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute background booking expiration check.");
            }
        }
    }

    private async Task ProcessExpiredBookingsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IDateTimeProvider dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        DateTime now = dateTimeProvider.UtcNow;
        DateTime cutoffTime = now.AddHours(-1);

        _logger.LogInformation("Scanning for pending bookings created before {CutoffTime}", cutoffTime);

        IReadOnlyList<Booking> expiredBookings = await unitOfWork.BookingRepository.GetExpiredPendingBookingsAsync(cutoffTime, cancellationToken);

        if (expiredBookings.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} expired bookings to invalidate.", expiredBookings.Count);

        foreach (Booking booking in expiredBookings)
        {
            try
            {
                booking.Expire();
                unitOfWork.BookingRepository.Update(booking);
                _logger.LogInformation("Booking {BookingId} status successfully set to Expired.", booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning status for booking {BookingId}.", booking.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}