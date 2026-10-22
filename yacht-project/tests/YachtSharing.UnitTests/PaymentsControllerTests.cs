using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YachtSharing.BookingService.Application.Abstractions.UseCases;
using YachtSharing.BookingService.Controllers;
using YachtSharing.BookingService.Domain.Enums;
using YachtSharing.BookingService.Domain.Exceptions;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.UnitTests;

public class PaymentsControllerTests
{
    [Fact]
    public async Task ReceiveBankWebhook_ReturnsOk_WhenWebhookProcessed()
    {
        var bookingService = new Mock<IBookingService>();
        var controller = new PaymentsController(bookingService.Object);
        PaymentWebhookDto dto = CreateWebhookDto();

        IActionResult result = await controller.ReceiveBankWebhook(dto, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        bookingService.Verify(
            service => service.ProcessPaymentWebhookAsync(dto, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveBankWebhook_ReturnsNotFound_WhenBookingDoesNotExist()
    {
        var bookingService = new Mock<IBookingService>();
        var controller = new PaymentsController(bookingService.Object);
        PaymentWebhookDto dto = CreateWebhookDto();

        bookingService
            .Setup(service => service.ProcessPaymentWebhookAsync(dto, CancellationToken.None))
            .ThrowsAsync(new KeyNotFoundException("Booking not found."));

        IActionResult result = await controller.ReceiveBankWebhook(dto, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReceiveBankWebhook_ReturnsConflict_WhenAmountIsInvalid()
    {
        var bookingService = new Mock<IBookingService>();
        var controller = new PaymentsController(bookingService.Object);
        PaymentWebhookDto dto = CreateWebhookDto();

        bookingService
            .Setup(service => service.ProcessPaymentWebhookAsync(dto, CancellationToken.None))
            .ThrowsAsync(new PaymentAmountMismatchException(dto.BookingId, 1000m, dto.Amount));

        IActionResult result = await controller.ReceiveBankWebhook(dto, CancellationToken.None);

        ObjectResult objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task ReceiveBankWebhook_ReturnsRequestTimeout_WhenBookingExpired()
    {
        var bookingService = new Mock<IBookingService>();
        var controller = new PaymentsController(bookingService.Object);
        PaymentWebhookDto dto = CreateWebhookDto();

        bookingService
            .Setup(service => service.ProcessPaymentWebhookAsync(dto, CancellationToken.None))
            .ThrowsAsync(new PaymentTimeoutException(dto.BookingId));

        IActionResult result = await controller.ReceiveBankWebhook(dto, CancellationToken.None);

        ObjectResult objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(408);
    }

    private static PaymentWebhookDto CreateWebhookDto()
    {
        return new PaymentWebhookDto(
            Guid.NewGuid(),
            "tx_777888999",
            PaymentStatus.Success,
            15000m);
    }
}
