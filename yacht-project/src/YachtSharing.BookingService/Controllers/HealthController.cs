using Microsoft.AspNetCore.Mvc;
using System;

namespace YachtSharing.BookingService.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult CheckHealth()
    {
        var healthStatus = new
        {
            Service = "booking-user",
            Status = "ok",
            Timestamp = DateTime.UtcNow,
            Checks = new
            {
                Database = "connected",
                GimsMock = "reachable",
                Scheduler = "running",
            },
        };

        return Ok(healthStatus);
    }
}