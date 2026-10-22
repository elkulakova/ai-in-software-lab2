using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.UseCases;
using YachtSharing.BookingService.Domain.Exceptions;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.BookingService.Controllers;

[ApiController]
[Route("api/v1/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking(
        [FromBody] BookingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            BookingResponseDto response = await _bookingService.CreateBookingAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetBookingById), new { id = response.BookingId }, response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (DomainException exception)
        {
            return StatusCode(exception.StatusCode, new { error = exception.Message });
        }
    }

    [HttpPost("{id:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            PaymentSessionResponseDto response = await _bookingService.PrepareCheckoutAsync(id, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (DomainException exception)
        {
            return StatusCode(exception.StatusCode, new { error = exception.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBookingById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            BookingResponseDto response = await _bookingService.GetBookingByIdAsync(id, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _bookingService.CancelBookingAsync(id, cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (InvalidBookingStatusTransitionException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}