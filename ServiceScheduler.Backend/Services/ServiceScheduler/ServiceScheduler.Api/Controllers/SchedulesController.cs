using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceScheduler.Api.Requests.Schedule;
using ServiceScheduler.Application.Features.Schedules;
using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SchedulesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScheduleCommand command, CancellationToken cancellationToken)
    {
        var result = await dispatcher.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Schedule.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateScheduleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateScheduleCommand(id, request.WorkerId, request.ServiceIds, request.ScheduledAt, request.Duration);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? workerId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = new ListSchedulesQuery(customerId, workerId, startDate, endDate, status);
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetScheduleByIdQuery(id);
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var command = new ConfirmScheduleCommand(id);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var command = new CancelScheduleCommand(id);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }
}
