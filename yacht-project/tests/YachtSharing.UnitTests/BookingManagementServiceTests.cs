using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YachtSharing.BookingService.Application.Abstractions.Repositories;
using YachtSharing.BookingService.Application.Abstractions.Services;
using YachtSharing.BookingService.Application.Services;
using YachtSharing.BookingService.Domain.Entities;
using YachtSharing.BookingService.Domain.Enums;
using YachtSharing.BookingService.Domain.Exceptions;
using YachtSharing.BookingService.Domain.ValueObjects;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.UnitTests;

public class BookingManagementServiceTests
{
    [Fact]
    public async Task ProcessPaymentWebhookAsync_DoesNothing_WhenTransactionAlreadyProcessed()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        PaymentWebhookDto dto = CreateWebhookDto();
        paymentRepository
            .Setup(x => x.GetByTransactionIdAsync(dto.TransactionId, CancellationToken.None))
            .ReturnsAsync(new Payment(dto.BookingId, dto.Amount, dto.TransactionId, DateTime.UtcNow));

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        await service.ProcessPaymentWebhookAsync(dto, CancellationToken.None);

        bookingRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        bookingRepository.Verify(x => x.Update(It.IsAny<Booking>()), Times.Never);
        paymentRepository.Verify(x => x.Add(It.IsAny<Payment>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentWebhookAsync_ConfirmsBookingAndCreatesPayment_WhenWebhookIsSuccessful()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        Booking booking = CreateBooking(15000m);
        PaymentWebhookDto dto = new PaymentWebhookDto(booking.Id, "tx_success", PaymentStatus.Success, booking.FinalCost);
        DateTime paymentTime = booking.CreatedAt.AddMinutes(20);

        paymentRepository
            .Setup(x => x.GetByTransactionIdAsync(dto.TransactionId, CancellationToken.None))
            .ReturnsAsync((Payment?)null);
        bookingRepository
            .Setup(x => x.GetByIdAsync(dto.BookingId, CancellationToken.None))
            .ReturnsAsync(booking);
        dateTimeProvider.SetupGet(x => x.UtcNow).Returns(paymentTime);

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        await service.ProcessPaymentWebhookAsync(dto, CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        bookingRepository.Verify(x => x.Update(booking), Times.Once);
        paymentRepository.Verify(
            x => x.Add(It.Is<Payment>(payment =>
                payment.BookingId == booking.Id &&
                payment.Amount == booking.FinalCost &&
                payment.TransactionId == dto.TransactionId &&
                payment.PaymentDate == paymentTime)),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentWebhookAsync_DoesNotPersistPayment_WhenWebhookFailed()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        Booking booking = CreateBooking(15000m);
        PaymentWebhookDto dto = new PaymentWebhookDto(booking.Id, "tx_failed", PaymentStatus.Failed, booking.FinalCost);

        paymentRepository
            .Setup(x => x.GetByTransactionIdAsync(dto.TransactionId, CancellationToken.None))
            .ReturnsAsync((Payment?)null);
        bookingRepository
            .Setup(x => x.GetByIdAsync(dto.BookingId, CancellationToken.None))
            .ReturnsAsync(booking);

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        await service.ProcessPaymentWebhookAsync(dto, CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.PendingPayment);
        bookingRepository.Verify(x => x.Update(It.IsAny<Booking>()), Times.Never);
        paymentRepository.Verify(x => x.Add(It.IsAny<Payment>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentWebhookAsync_Throws_WhenBookingDoesNotExist()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        PaymentWebhookDto dto = CreateWebhookDto();

        paymentRepository
            .Setup(x => x.GetByTransactionIdAsync(dto.TransactionId, CancellationToken.None))
            .ReturnsAsync((Payment?)null);
        bookingRepository
            .Setup(x => x.GetByIdAsync(dto.BookingId, CancellationToken.None))
            .ReturnsAsync((Booking?)null);

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        Func<Task> action = async () => await service.ProcessPaymentWebhookAsync(dto, CancellationToken.None);

        await action.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ProcessPaymentWebhookAsync_Throws_WhenAmountDoesNotMatch()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        Booking booking = CreateBooking(15000m);
        PaymentWebhookDto dto = new PaymentWebhookDto(booking.Id, "tx_wrong_amount", PaymentStatus.Success, 9999m);

        paymentRepository
            .Setup(x => x.GetByTransactionIdAsync(dto.TransactionId, CancellationToken.None))
            .ReturnsAsync((Payment?)null);
        bookingRepository
            .Setup(x => x.GetByIdAsync(dto.BookingId, CancellationToken.None))
            .ReturnsAsync(booking);

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        Func<Task> action = async () => await service.ProcessPaymentWebhookAsync(dto, CancellationToken.None);

        await action.Should().ThrowAsync<PaymentAmountMismatchException>();
        booking.Status.Should().Be(BookingStatus.PendingPayment);
        paymentRepository.Verify(x => x.Add(It.IsAny<Payment>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentWebhookAsync_ConfirmsBooking_WhenSuccessfulWebhookArrivesAfterOneHour()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();
        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);
        Booking booking = CreateBooking(15000m);
        PaymentWebhookDto dto = new PaymentWebhookDto(booking.Id, "tx_too_late", PaymentStatus.Success, booking.FinalCost);
        DateTime paymentTime = booking.CreatedAt.AddHours(2);
        paymentRepository
            .Setup(x => x.GetByTransactionIdAsync(dto.TransactionId, CancellationToken.None))
            .ReturnsAsync((Payment?)null);
        bookingRepository
            .Setup(x => x.GetByIdAsync(dto.BookingId, CancellationToken.None))
            .ReturnsAsync(booking);
        dateTimeProvider.SetupGet(x => x.UtcNow).Returns(paymentTime);
        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);
        await service.ProcessPaymentWebhookAsync(dto, CancellationToken.None);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        bookingRepository.Verify(x => x.Update(booking), Times.Once);
        paymentRepository.Verify(
            x => x.Add(It.Is<Payment>(payment =>
                payment.BookingId == booking.Id &&
                payment.Amount == booking.FinalCost &&
                payment.TransactionId == dto.TransactionId &&
                payment.PaymentDate == paymentTime)),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }
    [Fact]
    public async Task ProcessPaymentWebhookAsync_ThrowsInvalidStatusTransition_WhenBookingAlreadyExpired()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();
        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);
        Booking booking = CreateBooking(15000m);
        booking.Expire();
        PaymentWebhookDto dto = new PaymentWebhookDto(booking.Id, "tx_already_expired", PaymentStatus.Success, booking.FinalCost);
        paymentRepository
            .Setup(x => x.GetByTransactionIdAsync(dto.TransactionId, CancellationToken.None))
            .ReturnsAsync((Payment?)null);
        bookingRepository
            .Setup(x => x.GetByIdAsync(dto.BookingId, CancellationToken.None))
            .ReturnsAsync(booking);
        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);
        Func<Task> action = async () => await service.ProcessPaymentWebhookAsync(dto, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidBookingStatusTransitionException>();
        paymentRepository.Verify(x => x.Add(It.IsAny<Payment>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task DeleteClientAsync_RemovesClient_WhenNoBookingsExist()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        UserClient client = new("Anna Ivanova", "anna@example.com");
        clientRepository
            .Setup(x => x.GetByIdAsync(client.Id, CancellationToken.None))
            .ReturnsAsync(client);
        bookingRepository
            .Setup(x => x.GetByClientIdAsync(client.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<Booking>());

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        await service.DeleteClientAsync(client.Id, CancellationToken.None);

        bookingRepository.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<Booking>>()), Times.Never);
        clientRepository.Verify(x => x.Remove(client), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeleteClientAsync_RemovesClientAndInactiveBookings_WhenOnlyCancelledOrExpired()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        UserClient client = new("Anna Ivanova", "anna@example.com");
        Booking cancelled = CreateBookingForClient(client.Id, 10000m);
        cancelled.Cancel(cancelled.CreatedAt);
        Booking expired = CreateBookingForClient(client.Id, 12000m);
        expired.Expire();

        clientRepository
            .Setup(x => x.GetByIdAsync(client.Id, CancellationToken.None))
            .ReturnsAsync(client);
        bookingRepository
            .Setup(x => x.GetByClientIdAsync(client.Id, CancellationToken.None))
            .ReturnsAsync(new List<Booking> { cancelled, expired });

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        crewRepository
            .Setup(x => x.GetByBookingIdsAsync(
                It.Is<IReadOnlyList<Guid>>(ids => ids.Contains(cancelled.Id) && ids.Contains(expired.Id)),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<CrewMember>());
        paymentRepository
            .Setup(x => x.GetByBookingIdsAsync(
                It.Is<IReadOnlyList<Guid>>(ids => ids.Contains(cancelled.Id) && ids.Contains(expired.Id)),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<Payment>());

        await service.DeleteClientAsync(client.Id, CancellationToken.None);

        bookingRepository.Verify(
            x => x.RemoveRange(It.Is<IEnumerable<Booking>>(bookings =>
                bookings.Contains(cancelled) && bookings.Contains(expired))),
            Times.Once);
        clientRepository.Verify(x => x.Remove(client), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeleteClientAsync_ThrowsKeyNotFound_WhenClientDoesNotExist()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        Guid missingClientId = Guid.NewGuid();
        clientRepository
            .Setup(x => x.GetByIdAsync(missingClientId, CancellationToken.None))
            .ReturnsAsync((UserClient?)null);

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        Func<Task> action = async () => await service.DeleteClientAsync(missingClientId, CancellationToken.None);

        await action.Should().ThrowAsync<KeyNotFoundException>();
        clientRepository.Verify(x => x.Remove(It.IsAny<UserClient>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteClientAsync_ThrowsConflict_WhenClientHasActiveBooking()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        UserClient client = new("Anna Ivanova", "anna@example.com");
        Booking activeBooking = CreateBookingForClient(client.Id, 15000m);

        clientRepository
            .Setup(x => x.GetByIdAsync(client.Id, CancellationToken.None))
            .ReturnsAsync(client);
        bookingRepository
            .Setup(x => x.GetByClientIdAsync(client.Id, CancellationToken.None))
            .ReturnsAsync(new List<Booking> { activeBooking });

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        Func<Task> action = async () => await service.DeleteClientAsync(client.Id, CancellationToken.None);

        await action.Should().ThrowAsync<ClientHasActiveBookingsException>();
        clientRepository.Verify(x => x.Remove(It.IsAny<UserClient>()), Times.Never);
        bookingRepository.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<Booking>>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PrepareCheckoutAsync_ReturnsPaymentSession_WhenBookingStillActive()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        Booking booking = CreateBooking(15000m);
        bookingRepository
            .Setup(x => x.GetByIdAsync(booking.Id, CancellationToken.None))
            .ReturnsAsync(booking);
        dateTimeProvider.SetupGet(x => x.UtcNow).Returns(booking.CreatedAt.AddMinutes(30));

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        PaymentSessionResponseDto result = await service.PrepareCheckoutAsync(booking.Id, CancellationToken.None);

        result.BookingId.Should().Be(booking.Id);
        result.Amount.Should().Be(booking.FinalCost);
        result.PaymentUrl.ToString().Should().Contain(booking.Id.ToString());
        result.ExpirationTime.Should().Be(booking.CreatedAt.AddHours(1));
    }

    [Fact]
    public async Task PrepareCheckoutAsync_ExpiresBookingAndThrows_WhenPaymentWindowIsOver()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clientRepository = new Mock<IClientRepository>();
        var crewRepository = new Mock<ICrewRepository>();
        var catalogService = new Mock<ICatalogIntegrationService>();
        var gimsService = new Mock<IGimsIntegrationService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var logger = new Mock<ILogger<BookingManagementService>>();

        unitOfWork.SetupGet(x => x.BookingRepository).Returns(bookingRepository.Object);
        unitOfWork.SetupGet(x => x.PaymentRepository).Returns(paymentRepository.Object);

        Booking booking = CreateBooking(15000m);
        bookingRepository
            .Setup(x => x.GetByIdAsync(booking.Id, CancellationToken.None))
            .ReturnsAsync(booking);
        dateTimeProvider.SetupGet(x => x.UtcNow).Returns(booking.CreatedAt.AddHours(2));

        BookingManagementService service = CreateService(
            unitOfWork,
            clientRepository,
            crewRepository,
            catalogService,
            gimsService,
            dateTimeProvider,
            logger);

        Func<Task> action = async () => await service.PrepareCheckoutAsync(booking.Id, CancellationToken.None);

        await action.Should().ThrowAsync<PaymentTimeoutException>();
        booking.Status.Should().Be(BookingStatus.Expired);
        bookingRepository.Verify(x => x.Update(booking), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    private static BookingManagementService CreateService(
        Mock<IUnitOfWork> unitOfWork,
        Mock<IClientRepository> clientRepository,
        Mock<ICrewRepository> crewRepository,
        Mock<ICatalogIntegrationService> catalogService,
        Mock<IGimsIntegrationService> gimsService,
        Mock<IDateTimeProvider> dateTimeProvider,
        Mock<ILogger<BookingManagementService>> logger)
    {
        return new BookingManagementService(
            unitOfWork.Object,
            clientRepository.Object,
            crewRepository.Object,
            catalogService.Object,
            gimsService.Object,
            dateTimeProvider.Object,
            logger.Object);
    }

    private static PaymentWebhookDto CreateWebhookDto()
    {
        return new PaymentWebhookDto(
            Guid.NewGuid(),
            "tx_777888999",
            PaymentStatus.Success,
            15000m);
    }

    private static Booking CreateBooking(decimal finalCost)
    {
        return CreateBookingForClient(Guid.NewGuid(), finalCost);
    }

    private static Booking CreateBookingForClient(Guid clientId, decimal finalCost)
    {
        return new Booking(
            clientId,
            7,
            new DateTime(2026, 06, 21, 10, 00, 00, DateTimeKind.Utc),
            new DateTime(2026, 06, 21, 14, 00, 00, DateTimeKind.Utc),
            new Passport("4512", "123456"),
            finalCost);
    }
}

