using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceScheduler.Api.Requests.Admin;
using ServiceScheduler.Application.Features.Schedules;
using SharedKernel.Abstractions.CQRS;

namespace ServiceScheduler.Api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize]
public class AdminsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpPut("schedules/{id:guid}")]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] AdminUpdateScheduleRequest request, CancellationToken cancellationToken)
    {
        var command = new AdminUpdateScheduleCommand(
            id,
            request.CustomerId,
            request.WorkerId,
            request.ServiceIds,
            request.ScheduledAt,
            request.Duration,
            request.OverrideNetValue
        );
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPatch("schedules/{id:guid}/services/{serviceId:guid}/status")]
    public async Task<IActionResult> UpdateServiceStatus(Guid id, Guid serviceId, [FromBody] UpdateServiceStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateServiceStatusInScheduleCommand(id, serviceId, request.Status);
        await dispatcher.SendAsync(command, cancellationToken);
        return NoContent();
    }
}
