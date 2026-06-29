using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceScheduler.Api.Requests.ServiceBundle;
using ServiceScheduler.Application.Features.ServiceBundles;
using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ServiceBundlesController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceBundleCommand command, CancellationToken cancellationToken)
    {
        var id = await dispatcher.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceBundleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateServiceBundleCommand(id, request.Name, request.Description, request.ServiceIds, request.Price);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var query = new ListServiceBundlesQuery();
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetServiceBundleByIdQuery(id);
        var result = await dispatcher.SendAsync(query, cancellationToken);
        return Ok(result);
    }
}
