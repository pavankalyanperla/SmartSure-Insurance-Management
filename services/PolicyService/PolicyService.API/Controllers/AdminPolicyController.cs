using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolicyService.Domain.Entities;
using PolicyService.Infrastructure.Data;

namespace PolicyService.API.Controllers;

[ApiController]
[Route("api/policies/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminPolicyController : ControllerBase
{
    private readonly PolicyDbContext _db;

    public AdminPolicyController(PolicyDbContext db)
    {
        _db = db;
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetAllPolicyTypes()
    {
        var types = await _db.PolicyTypes
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.BaseAmount,
                t.IsActive,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(types);
    }

    [HttpPost("types")]
    public async Task<IActionResult> CreatePolicyType([FromBody] PolicyType dto)
    {
        var entity = new PolicyType
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            BaseAmount = dto.BaseAmount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.PolicyTypes.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(entity);
    }

    [HttpPut("types/{id}")]
    public async Task<IActionResult> UpdatePolicyType(int id, [FromBody] PolicyType dto)
    {
        var entity = await _db.PolicyTypes.FindAsync(id);
        if (entity is null)
        {
            return NotFound(new { message = "Policy type not found" });
        }

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description.Trim();
        entity.BaseAmount = dto.BaseAmount;

        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpPut("types/{id}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, [FromQuery] bool isActive)
    {
        var entity = await _db.PolicyTypes.FindAsync(id);
        if (entity is null)
        {
            return NotFound(new { message = "Policy type not found" });
        }

        entity.IsActive = isActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Policy type {(isActive ? "activated" : "deactivated")} successfully" });
    }

    [HttpDelete("types/{id}")]
    public async Task<IActionResult> DeletePolicyType(int id)
    {
        var entity = await _db.PolicyTypes.FindAsync(id);
        if (entity is null)
        {
            return NotFound(new { message = "Policy type not found" });
        }

        _db.PolicyTypes.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Policy type deleted successfully" });
    }

    [HttpGet("types/{id}/stats")]
    public async Task<IActionResult> GetStats(int id)
    {
        var exists = await _db.PolicyTypes.AnyAsync(t => t.Id == id);
        if (!exists)
        {
            return NotFound(new { message = "Policy type not found" });
        }

        var totalPolicies = await _db.Policies.CountAsync(p => p.PolicyTypeId == id);
        var activePolicies = await _db.Policies.CountAsync(p => p.PolicyTypeId == id && p.Status == Domain.Enums.PolicyStatus.Active);
        var totalPremium = await _db.Policies
            .Where(p => p.PolicyTypeId == id)
            .SumAsync(p => (decimal?)p.PremiumAmount) ?? 0m;

        var stats = new
        {
            policyTypeId = id,
            totalPolicies,
            activePolicies,
            totalPremium
        };

        return Ok(stats);
    }
}
