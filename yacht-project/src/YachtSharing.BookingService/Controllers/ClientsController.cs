using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.UseCases;
using YachtSharing.BookingService.DTOs;

namespace YachtSharing.BookingService.Controllers;

[ApiController]
[Route("api/v1/clients")]
public class ClientsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public ClientsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterClient(
        [FromBody] RegisterClientDto dto,
        CancellationToken cancellationToken)
    {
        Guid clientId = await _bookingService.RegisterClientAsync(dto, cancellationToken);
        return Ok(new { clientId });
    }

    [HttpDelete("{clientId:guid}")]
    public async Task<IActionResult> DeleteClient(
        [FromRoute] Guid clientId,
        CancellationToken cancellationToken)
    {
        await _bookingService.DeleteClientAsync(clientId, cancellationToken);
        return NoContent();
    }
}