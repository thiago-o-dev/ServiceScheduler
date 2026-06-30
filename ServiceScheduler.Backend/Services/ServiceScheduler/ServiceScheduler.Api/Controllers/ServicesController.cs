using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceScheduler.Api.Requests.Service;
using ServiceScheduler.Application.Features.Services;
using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ServicesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceCommand command, CancellationToken cancellationToken)
    {
        var id = await dispatcher.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateServiceCommand(id, request.Name, request.Description, request.Value);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var query = new ListServicesQuery();
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetServiceByIdQuery(id);
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/available-hours-multiple")]
    public async Task<IActionResult> GetAvailableHoursMultiple(
        Guid id,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        CancellationToken cancellationToken)
    {
        var query = new GetServiceAvailableHoursQuery(id, start, end);
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/available-hours")]
    public async Task<IActionResult> GetAvailableHours(
        Guid id,
        [FromQuery] DateTime date,
        [FromQuery] Guid workerId,
        CancellationToken cancellationToken)
    {
        var query = new GetServiceAvailableHoursQuery(id, date.Date, date.Date.AddHours(24), workerId);
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result.First().Value);
    }
}
