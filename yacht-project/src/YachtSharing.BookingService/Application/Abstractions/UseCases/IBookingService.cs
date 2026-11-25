using System;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.BookingService.Application.Abstractions.UseCases;

public interface IBookingService
{
    Task<Guid> RegisterClientAsync(RegisterClientDto dto, CancellationToken cancellationToken = default);

    Task DeleteClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<BookingResponseDto> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<BookingResponseDto> CreateBookingAsync(
        BookingRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<PaymentSessionResponseDto> PrepareCheckoutAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task CancelBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task ProcessPaymentWebhookAsync(PaymentWebhookDto dto, CancellationToken cancellationToken = default);
}