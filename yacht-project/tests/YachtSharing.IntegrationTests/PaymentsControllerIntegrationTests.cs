using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YachtSharing.BookingService.Application.Abstractions.UseCases;
using YachtSharing.BookingService.Domain.Exceptions;
using YachtSharing.BookingService.DTOs;
namespace YachtSharing.IntegrationTests;
public class PaymentsControllerIntegrationTests
{
    [Fact]
    public async Task Webhook_ReturnsOk_WhenServiceProcessesWebhook()
    {
        var bookingService = new FakeBookingService();
        using var factory = new BookingServiceWebApplicationFactory(bookingService);
        HttpClient client = factory.CreateClient();
        PaymentWebhookDto dto = CreateWebhookDto();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/payments/webhook", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        bookingService.LastWebhook.Should().NotBeNull();
        PaymentWebhookDto lastWebhook = bookingService.LastWebhook
            ?? throw new InvalidOperationException("Expected webhook to be captured by the fake service.");
        lastWebhook.TransactionId.Should().Be(dto.TransactionId);
    }
    [Fact]
    public async Task Webhook_ReturnsNotFound_WhenServiceThrowsKeyNotFound()
    {
        var bookingService = new FakeBookingService
        {
            WebhookException = new KeyNotFoundException("Booking not found."),
        };
        using var factory = new BookingServiceWebApplicationFactory(bookingService);
        HttpClient client = factory.CreateClient();
        PaymentWebhookDto dto = CreateWebhookDto();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/payments/webhook", dto);
        string content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        content.Should().Contain("Booking not found");
    }
    [Fact]
    public async Task Webhook_ReturnsConflict_WhenServiceThrowsPaymentMismatch()
    {
        PaymentWebhookDto dto = CreateWebhookDto();
        var bookingService = new FakeBookingService
        {
            WebhookException = new PaymentAmountMismatchException(dto.BookingId, 1000m, dto.Amount),
        };
        using var factory = new BookingServiceWebApplicationFactory(bookingService);
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/payments/webhook", dto);
        string content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        content.Should().Contain("Payment security violation");
    }
    [Fact]
    public async Task Webhook_ReturnsRequestTimeout_WhenServiceThrowsPaymentTimeout()
    {
        PaymentWebhookDto dto = CreateWebhookDto();
        var bookingService = new FakeBookingService
        {
            WebhookException = new PaymentTimeoutException(dto.BookingId),
        };
        using var factory = new BookingServiceWebApplicationFactory(bookingService);
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/payments/webhook", dto);
        string content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
        content.Should().Contain("expired");
    }
    private static PaymentWebhookDto CreateWebhookDto()
    {
        return new PaymentWebhookDto(
            Guid.NewGuid(),
            "tx_e2e_777888",
            YachtSharing.BookingService.Domain.Enums.PaymentStatus.Success,
            15000m);
    }
    private sealed class FakeBookingService : IBookingService
    {
        public Exception? WebhookException { get; set; }
        public PaymentWebhookDto? LastWebhook { get; private set; }
        public Task<Guid> RegisterClientAsync(RegisterClientDto dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task<BookingResponseDto> CreateBookingAsync(BookingRequestDto dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task<PaymentSessionResponseDto> PrepareCheckoutAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task ProcessPaymentWebhookAsync(PaymentWebhookDto dto, CancellationToken cancellationToken = default)
        {
            LastWebhook = dto;
            if (WebhookException is not null)
            {
                throw WebhookException;
            }
            return Task.CompletedTask;
        }
    }
}
