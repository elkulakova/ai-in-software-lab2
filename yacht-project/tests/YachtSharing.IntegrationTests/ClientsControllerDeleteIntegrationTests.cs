using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using YachtSharing.BookingService.Domain.Enums;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.IntegrationTests;

public class ClientsControllerDeleteIntegrationTests
{
    [Fact]
    public async Task DeleteClient_ReturnsNoContent_WhenClientHasNoBookings()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/clients/{clientId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await factory.ClientExistsAsync(clientId)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteClient_ReturnsNoContent_WhenOnlyCancelledOrExpiredBookings()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync("Maria Petrova", "maria@example.com");

        BookingRequestDto request = CreateBookingRequest(clientId);
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/bookings", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        BookingResponseDto? createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponseDto>();
        createdBooking.Should().NotBeNull();
        BookingResponseDto created = createdBooking
            ?? throw new InvalidOperationException("Expected booking creation response payload.");
        Guid bookingId = created.BookingId;

        await factory.SetBookingStatusAsync(bookingId, BookingStatus.Cancelled);

        HttpResponseMessage createExpiredResponse = await client.PostAsJsonAsync(
            "/api/v1/bookings",
            CreateBookingRequest(clientId, yachtId: 8));
        createExpiredResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        BookingResponseDto? secondBooking = await createExpiredResponse.Content.ReadFromJsonAsync<BookingResponseDto>();
        secondBooking.Should().NotBeNull();
        BookingResponseDto expiredBooking = secondBooking
            ?? throw new InvalidOperationException("Expected second booking creation response payload.");
        await factory.SetBookingStatusAsync(expiredBooking.BookingId, BookingStatus.Expired);

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/clients/{clientId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await factory.ClientExistsAsync(clientId)).Should().BeFalse();
        (await factory.GetBookingsCountAsync()).Should().Be(0);
        (await factory.GetCrewCountForBookingAsync(bookingId)).Should().Be(0);
    }

    [Fact]
    public async Task DeleteClient_ReturnsNotFound_WhenClientDoesNotExist()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/clients/{Guid.NewGuid()}");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        content.Should().Contain("NOT_FOUND");
    }

    [Fact]
    public async Task DeleteClient_ReturnsConflict_WhenClientHasPendingPaymentBooking()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/bookings",
            CreateBookingRequest(clientId));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/clients/{clientId}");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        content.Should().Contain("CLIENT_HAS_ACTIVE_BOOKINGS");
        (await factory.ClientExistsAsync(clientId)).Should().BeTrue();
        (await factory.GetBookingsCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteClient_ReturnsConflict_WhenClientHasConfirmedBooking()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/bookings",
            CreateBookingRequest(clientId));
        BookingResponseDto? createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponseDto>();
        createdBooking.Should().NotBeNull();
        BookingResponseDto created = createdBooking
            ?? throw new InvalidOperationException("Expected booking creation response payload.");

        PaymentWebhookDto webhook = new(created.BookingId, "tx_delete_conflict", PaymentStatus.Success, 20000m);
        HttpResponseMessage webhookResponse = await client.PostAsJsonAsync("/api/v1/payments/webhook", webhook);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/clients/{clientId}");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        content.Should().Contain("CLIENT_HAS_ACTIVE_BOOKINGS");
        (await factory.ClientExistsAsync(clientId)).Should().BeTrue();
        (await factory.GetPaymentsCountAsync(created.BookingId)).Should().Be(1);
    }

    [Fact]
    public async Task DeleteClient_ReturnsNotFound_WhenCalledTwice()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        HttpResponseMessage first = await client.DeleteAsync($"/api/v1/clients/{clientId}");
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage second = await client.DeleteAsync($"/api/v1/clients/{clientId}");
        second.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static BookingRequestDto CreateBookingRequest(Guid clientId, int yachtId = 7)
    {
        return new BookingRequestDto(
            clientId,
            yachtId,
            new DateTime(2026, 06, 25, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 06, 25, 14, 0, 0, DateTimeKind.Utc),
            new PassportDto("4512", "123456"),
            new List<CrewMemberDto>
            {
                new("Petr Petrov", new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)),
            });
    }
}
