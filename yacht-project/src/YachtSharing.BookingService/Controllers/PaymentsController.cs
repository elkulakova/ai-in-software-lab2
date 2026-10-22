using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.UseCases;
using YachtSharing.BookingService.Domain.Exceptions;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.BookingService.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public PaymentsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveBankWebhook(
        [FromBody] PaymentWebhookDto webhookDto,
        CancellationToken cancellationToken)
    {
        try
        {
            await _bookingService.ProcessPaymentWebhookAsync(webhookDto, cancellationToken);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }
}
