using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceScheduler.Application.Features.Dashboard;
using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet("weekly-performance")]
    public async Task<IActionResult> GetWeeklyPerformance([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        var query = new GetWeeklyPerformanceQuery(date);
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }
}
