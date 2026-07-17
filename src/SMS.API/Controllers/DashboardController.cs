using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Dashboard.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Aggregate analytics for the admin dashboard.
/// </summary>
[Authorize]
public class DashboardController : BaseApiController
{
    [HttpGet]
    [RequirePermission(Permissions.ReportsDashboard)]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await Mediator.Send(new GetDashboardQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
