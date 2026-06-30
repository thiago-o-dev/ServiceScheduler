using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceScheduler.Api.Requests.Worker;
using ServiceScheduler.Application.Features.Workers;
using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WorkersController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkerCommand command, CancellationToken cancellationToken)
    {
        var id = await dispatcher.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkerRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateWorkerCommand(id, request.Name, request.Phone, request.Email, request.Cpf);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/available-periods")]
    public async Task<IActionResult> AddAvailablePeriod(Guid id, [FromBody] AddAvailablePeriodRequest request, CancellationToken cancellationToken)
    {
        var command = new AddAvailablePeriodCommand(id, request.DayOfWeek, request.StartTime, request.EndTime);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/available-periods")]
    public async Task<IActionResult> RemoveAvailablePeriod(Guid id, [FromBody] RemoveAvailablePeriodRequest request, CancellationToken cancellationToken)
    {
        var command = new RemoveAvailablePeriodCommand(id, request.DayOfWeek, request.StartTime, request.EndTime);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unavailable-periods")]
    public async Task<IActionResult> AddUnavailablePeriod(Guid id, [FromBody] AddUnavailablePeriodRequest request, CancellationToken cancellationToken)
    {
        var command = new AddUnavailablePeriodCommand(id, request.Start, request.End, request.Reason);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/unavailable-periods")]
    public async Task<IActionResult> RemoveUnavailablePeriod(Guid id, [FromBody] RemoveUnavailablePeriodRequest request, CancellationToken cancellationToken)
    {
        var command = new RemoveUnavailablePeriodCommand(id, request.Start, request.End);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unavailable-periods/preempt")]
    public async Task<IActionResult> PreemptUnavailablePeriod(Guid id, [FromBody] PreemptUnavailablePeriodRequest request, CancellationToken cancellationToken)
    {
        var command = new PreemptUnavailablePeriodCommand(id, request.End);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var query = new ListWorkersQuery();
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetWorkerByIdQuery(id);
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/available-periods")]
    public async Task<IActionResult> GetAvailablePeriods(
        Guid id,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkerAvailablePeriodsQuery(id, start, end);
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }
}
