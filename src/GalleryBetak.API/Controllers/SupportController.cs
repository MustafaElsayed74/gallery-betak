using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Admin;
using GalleryBetak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GalleryBetak.API.Controllers;

/// <summary>
/// Public customer support endpoints.
/// </summary>
[ApiController]
[Route("api/v1/support")]
[Produces("application/json")]
public class SupportController : ControllerBase
{
    private readonly IAdminDashboardService _adminService;

    public SupportController(IAdminDashboardService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>Submits a customer service message.</summary>
    [HttpPost("messages")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CustomerServiceMessageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitMessage([FromBody] CreateCustomerServiceMessageRequest request)
    {
        var result = await _adminService.CreateCustomerServiceMessageAsync(request);
        return StatusCode(result.StatusCode, result);
    }
}
