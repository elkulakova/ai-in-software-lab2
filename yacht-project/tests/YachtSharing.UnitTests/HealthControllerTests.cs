using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Xunit;
using YachtSharing.BookingService.Controllers;

namespace YachtSharing.UnitTests;

public class HealthControllerTests
{
    [Fact]
    public void CheckHealth_ReturnsOkStatusPayload()
    {
        var controller = new HealthController();

        IActionResult result = controller.CheckHealth();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        string json = JsonSerializer.Serialize(okResult.Value);

        json.Should().Contain("booking-user");
        json.Should().Contain("ok");
        json.Should().Contain("connected");
        json.Should().Contain("reachable");
        json.Should().Contain("running");
    }
}
