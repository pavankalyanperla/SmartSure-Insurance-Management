using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyService.Application.DTOs;
using PolicyService.Application.Interfaces;

namespace PolicyService.API.Controllers;

[ApiController]
[Route("api/policies/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminPolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public AdminPolicyController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetAllPolicyTypes()
    {
        var types = await _policyService.GetAllPolicyTypesForAdminAsync();
        return Ok(types);
    }

    [HttpPost("types")]
    public async Task<IActionResult> CreatePolicyType([FromBody] CreatePolicyTypeDto dto)
    {
        var result = await _policyService.CreatePolicyTypeAsync(dto);
        return Ok(result);
    }

    [HttpPut("types/{id}")]
    public async Task<IActionResult> UpdatePolicyType(int id, [FromBody] UpdatePolicyTypeDto dto)
    {
        var result = await _policyService.UpdatePolicyTypeAsync(id, dto);
        return Ok(result);
    }

    [HttpPut("types/{id}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, [FromQuery] bool isActive)
    {
        await _policyService.TogglePolicyTypeStatusAsync(id, isActive);
        return Ok(new { message = $"Policy type {(isActive ? "activated" : "deactivated")} successfully" });
    }

    [HttpDelete("types/{id}")]
    public async Task<IActionResult> DeletePolicyType(int id)
    {
        var result = await _policyService.DeletePolicyTypeAsync(id);
        if (!result) return NotFound(new { message = "Policy type not found" });
        return Ok(new { message = "Policy type deleted successfully" });
    }

    [HttpGet("types/{id}/stats")]
    public async Task<IActionResult> GetStats(int id)
    {
        var stats = await _policyService.GetPolicyTypeStatsAsync(id);
        return Ok(stats);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyService.Application.DTOs;
using PolicyService.Application.Interfaces;

namespace PolicyService.API.Controllers;

[ApiController]
[Route("api/policies/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminPolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public AdminPolicyController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetAllPolicyTypes()
    {
        var types = await _policyService.GetAllPolicyTypesForAdminAsync();
        return Ok(types);
    }

    [HttpPost("types")]
    public async Task<IActionResult> CreatePolicyType([FromBody] CreatePolicyTypeDto dto)
    {
        var result = await _policyService.CreatePolicyTypeAsync(dto);
        return Ok(result);
    }

    [HttpPut("types/{id}")]
    public async Task<IActionResult> UpdatePolicyType(int id, [FromBody] UpdatePolicyTypeDto dto)
    {
        var result = await _policyService.UpdatePolicyTypeAsync(id, dto);
        return Ok(result);
    }

    [HttpPut("types/{id}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, [FromQuery] bool isActive)
    {
        await _policyService.TogglePolicyTypeStatusAsync(id, isActive);
        return Ok(new { message = $"Policy type {(isActive ? "activated" : "deactivated")} successfully" });
    }

    [HttpDelete("types/{id}")]
    public async Task<IActionResult> DeletePolicyType(int id)
    {
        var result = await _policyService.DeletePolicyTypeAsync(id);
        if (!result) return NotFound(new { message = "Policy type not found" });
        return Ok(new { message = "Policy type deleted successfully" });
    }

    [HttpGet("types/{id}/stats")]
    public async Task<IActionResult> GetStats(int id)
    {
        var stats = await _policyService.GetPolicyTypeStatsAsync(id);
        return Ok(stats);
    }
}