using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using YachtSharing.BookingService.Domain.Enums;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.IntegrationTests;

public class BookingFlowE2eTests
{
    [Fact]
    public async Task SuccessFlow_CreatesBooking_Checkout_ConfirmsBooking()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        BookingRequestDto request = CreateBookingRequest(clientId);

        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/bookings", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        BookingResponseDto? createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponseDto>();
        createdBooking.Should().NotBeNull();
        BookingResponseDto created = createdBooking
            ?? throw new InvalidOperationException("Expected booking creation response payload.");
        created.Status.Should().Be(BookingStatus.PendingPayment);

        HttpResponseMessage checkoutResponse = await client.PostAsync($"/api/v1/bookings/{created.BookingId}/checkout", null);
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        PaymentSessionResponseDto? session = await checkoutResponse.Content.ReadFromJsonAsync<PaymentSessionResponseDto>();
        session.Should().NotBeNull();
        PaymentSessionResponseDto checkoutSession = session
            ?? throw new InvalidOperationException("Expected checkout session payload.");
        checkoutSession.Amount.Should().Be(20000m);

        PaymentWebhookDto webhook = new(created.BookingId, "tx_e2e_success", PaymentStatus.Success, checkoutSession.Amount);
        HttpResponseMessage webhookResponse = await client.PostAsJsonAsync("/api/v1/payments/webhook", webhook);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/bookings/{created.BookingId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        BookingResponseDto? confirmedBooking = await getResponse.Content.ReadFromJsonAsync<BookingResponseDto>();
        confirmedBooking.Should().NotBeNull();
        BookingResponseDto confirmed = confirmedBooking
            ?? throw new InvalidOperationException("Expected confirmed booking payload.");
        confirmed.Status.Should().Be(BookingStatus.Confirmed);

        int paymentsCount = await factory.GetPaymentsCountAsync(created.BookingId);
        paymentsCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateBooking_ReturnsForbidden_WhenGimsVerificationFails()
    {
        using var factory = new BookingServiceE2eFactory();
        factory.Gims.IsValid = false;

        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        BookingRequestDto request = CreateBookingRequest(clientId);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/bookings", request);
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        content.Should().Contain("license");
        content.Should().Contain("GIMS");
        (await factory.GetBookingsCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Checkout_ReturnsRequestTimeout_WhenBookingExpiredAfterOneHour()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        BookingRequestDto request = CreateBookingRequest(clientId);
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/bookings", request);
        BookingResponseDto? createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponseDto>();

        createdBooking.Should().NotBeNull();
        BookingResponseDto created = createdBooking
            ?? throw new InvalidOperationException("Expected booking creation response payload.");

        YachtSharing.BookingService.Domain.Entities.Booking? booking = await factory.GetBookingAsync(created.BookingId);
        booking.Should().NotBeNull();
        YachtSharing.BookingService.Domain.Entities.Booking persistedBooking = booking
            ?? throw new InvalidOperationException("Expected persisted booking.");

        factory.Clock.UtcNow = persistedBooking.CreatedAt.AddHours(2);

        HttpResponseMessage checkoutResponse = await client.PostAsync($"/api/v1/bookings/{created.BookingId}/checkout", null);
        string content = await checkoutResponse.Content.ReadAsStringAsync();

        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
        content.Should().Contain("expired");

        YachtSharing.BookingService.Domain.Entities.Booking? expiredBooking = await factory.GetBookingAsync(created.BookingId);
        expiredBooking.Should().NotBeNull();
        YachtSharing.BookingService.Domain.Entities.Booking expired = expiredBooking
            ?? throw new InvalidOperationException("Expected expired booking.");
        expired.Status.Should().Be(BookingStatus.Expired);
    }

    [Fact]
    public async Task Webhook_DuplicateTransactionId_IsIgnoredWithoutSecondPayment()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        BookingRequestDto request = CreateBookingRequest(clientId);
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/bookings", request);
        BookingResponseDto? createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponseDto>();

        createdBooking.Should().NotBeNull();
        BookingResponseDto created = createdBooking
            ?? throw new InvalidOperationException("Expected booking creation response payload.");

        PaymentWebhookDto webhook = new(created.BookingId, "tx_duplicate_e2e", PaymentStatus.Success, 20000m);

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/v1/payments/webhook", webhook);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/v1/payments/webhook", webhook);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        int paymentsCount = await factory.GetPaymentsCountAsync(created.BookingId);
        paymentsCount.Should().Be(1);

        YachtSharing.BookingService.Domain.Entities.Booking? booking = await factory.GetBookingAsync(created.BookingId);
        booking.Should().NotBeNull();
        YachtSharing.BookingService.Domain.Entities.Booking confirmedBooking = booking
            ?? throw new InvalidOperationException("Expected confirmed booking.");
        confirmedBooking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Webhook_ConfirmsBooking_WhenSuccessfulWebhookArrivesAfterOneHour()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        BookingRequestDto request = CreateBookingRequest(clientId);
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/bookings", request);
        BookingResponseDto? createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponseDto>();

        createdBooking.Should().NotBeNull();
        BookingResponseDto created = createdBooking
            ?? throw new InvalidOperationException("Expected booking creation response payload.");

        YachtSharing.BookingService.Domain.Entities.Booking? booking = await factory.GetBookingAsync(created.BookingId);
        booking.Should().NotBeNull();
        YachtSharing.BookingService.Domain.Entities.Booking persistedBooking = booking
            ?? throw new InvalidOperationException("Expected persisted booking.");

        factory.Clock.UtcNow = persistedBooking.CreatedAt.AddHours(2);

        PaymentWebhookDto webhook = new(created.BookingId, "tx_webhook_too_late", PaymentStatus.Success, 20000m);
        HttpResponseMessage webhookResponse = await client.PostAsJsonAsync("/api/v1/payments/webhook", webhook);

        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        YachtSharing.BookingService.Domain.Entities.Booking? confirmedBooking = await factory.GetBookingAsync(created.BookingId);
        confirmedBooking.Should().NotBeNull();
        YachtSharing.BookingService.Domain.Entities.Booking confirmed = confirmedBooking
            ?? throw new InvalidOperationException("Expected confirmed booking.");
        confirmed.Status.Should().Be(BookingStatus.Confirmed);

        int paymentsCount = await factory.GetPaymentsCountAsync(created.BookingId);
        paymentsCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateBooking_ReturnsBadRequest_WhenCrewInsuranceExpired()
    {
        using var factory = new BookingServiceE2eFactory();
        HttpClient client = factory.CreateClient();
        Guid clientId = await factory.SeedClientAsync();

        BookingRequestDto request = CreateBookingRequest(
            clientId,
            new DateTime(2026, 06, 25, 9, 0, 0, DateTimeKind.Utc));

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/bookings", request);
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("insurance");
        (await factory.GetBookingsCountAsync()).Should().Be(0);
    }

    private static BookingRequestDto CreateBookingRequest(Guid clientId, DateTime? insuranceExpiry = null)
    {
        return new BookingRequestDto(
            clientId,
            7,
            new DateTime(2026, 06, 25, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 06, 25, 14, 0, 0, DateTimeKind.Utc),
            new PassportDto("4512", "123456"),
            new List<CrewMemberDto>
            {
                new("Petr Petrov", insuranceExpiry ?? new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)),
            });
    }
}
