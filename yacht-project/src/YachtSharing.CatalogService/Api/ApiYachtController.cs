using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YachtSharing.CatalogService.Entities;
using YachtSharing.CatalogService.Repositories;

namespace YachtSharing.CatalogService.Api;

[ApiController]
[Route("api/v1/yachts")]
public class ApiYachtController : ControllerBase
{
    private readonly IYachtRepository _repository;

    public ApiYachtController(IYachtRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableYachts(
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset end)
    {
        if (start >= end)
        {
            return BadRequest(new { error = "Время начала должно быть меньше времени окончания" });
        }

        List<Yacht> yachts = await _repository.GetAvailableInInterval(start, end);

        return Ok(yachts);
    }
}
