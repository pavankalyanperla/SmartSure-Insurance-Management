namespace AdminService.API.Controllers;

using System.Security.Claims;
using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var summary = await _adminService.GetDashboardSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("claims/pending")]
    public async Task<IActionResult> GetPendingClaims()
    {
        var claims = await _adminService.GetPendingClaimsAsync();
        return Ok(claims);
    }

    [HttpGet("claims")]
    public async Task<IActionResult> GetAllClaims()
    {
        var claims = await _adminService.GetAllClaimsAsync();
        return Ok(claims);
    }

    [HttpPut("claims/status")]
    public async Task<IActionResult> UpdateClaimStatus([FromBody] UpdateClaimStatusDto dto)
    {
        var adminId = ExtractAdminId();
        var result = await _adminService.UpdateClaimStatusAsync(dto, adminId);
        return Ok(result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _adminService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPut("users/{userId}/status")]
    public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] UpdateUserStatusDto dto)
    {
        var result = await _adminService.UpdateUserStatusAsync(userId, dto);
        return Ok(result);
    }

    [HttpGet("reports/generate")]
    public async Task<IActionResult> GenerateReport([FromQuery] string reportType)
    {
        var adminId = ExtractAdminId();
        var result = await _adminService.GenerateReportAsync(reportType, adminId);
        return Ok(result);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetAdminLogs()
    {
        var logs = await _adminService.GetAdminLogsAsync();
        return Ok(logs);
    }

    private int ExtractAdminId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out var id))
        {
            return id;
        }
        return 0;
    }
}
