using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YachtSharing.BookingService.Application.Abstractions.UseCases;
using YachtSharing.BookingService.Domain.Exceptions;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.IntegrationTests;

public class ClientsControllerIntegrationTests
{
    [Fact]
    public async Task DeleteClient_ReturnsNoContent_WhenServiceSucceeds()
    {
        var bookingService = new FakeBookingService();
        using var factory = new BookingServiceWebApplicationFactory(bookingService);
        HttpClient client = factory.CreateClient();
        Guid clientId = Guid.NewGuid();

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/clients/{clientId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        bookingService.DeletedClientId.Should().Be(clientId);
    }

    [Fact]
    public async Task DeleteClient_ReturnsNotFound_WhenServiceThrowsKeyNotFound()
    {
        Guid clientId = Guid.NewGuid();
        var bookingService = new FakeBookingService
        {
            DeleteException = new KeyNotFoundException($"Client with ID {clientId} was not found."),
        };
        using var factory = new BookingServiceWebApplicationFactory(bookingService);
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/clients/{clientId}");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        content.Should().Contain("was not found");
    }

    [Fact]
    public async Task DeleteClient_ReturnsConflict_WhenClientHasActiveBookings()
    {
        Guid clientId = Guid.NewGuid();
        var bookingService = new FakeBookingService
        {
            DeleteException = new ClientHasActiveBookingsException(clientId),
        };
        using var factory = new BookingServiceWebApplicationFactory(bookingService);
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/clients/{clientId}");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        content.Should().Contain("active bookings");
        content.Should().Contain("CLIENT_HAS_ACTIVE_BOOKINGS");
    }

    private sealed class FakeBookingService : IBookingService
    {
        public Exception? DeleteException { get; set; }

        public Guid? DeletedClientId { get; private set; }

        public Task<Guid> RegisterClientAsync(RegisterClientDto dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            if (DeleteException is not null)
            {
                throw DeleteException;
            }

            DeletedClientId = clientId;
            return Task.CompletedTask;
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
            throw new NotImplementedException();
        }
    }
}
