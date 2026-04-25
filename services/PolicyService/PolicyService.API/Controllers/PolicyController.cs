using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyService.Application.DTOs;
using PolicyService.Application.Interfaces;
using PolicyService.Domain.Enums;
using PolicyService.Infrastructure.Messaging;
using PolicyService.Infrastructure.Repositories;
using System.Security.Claims;

namespace PolicyService.API.Controllers;

[ApiController]
[Route("api/policies")]
[Authorize]
public class PolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;
    private readonly RabbitMQPublisher _publisher;
    private readonly AdoPolicyRepository _adoRepository;

    public PolicyController(IPolicyService policyService, RabbitMQPublisher publisher, AdoPolicyRepository adoRepository)
    {
        _policyService = policyService;
        _publisher = publisher;
        _adoRepository = adoRepository;
    }

    [HttpGet("types")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPolicyTypes()
    {
        var types = await _policyService.GetAllPolicyTypesAsync();
        return Ok(types);
    }

    [HttpGet("types/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPolicyType(int id)
    {
        var type = await _policyService.GetPolicyTypeByIdAsync(id);
        return type is null ? NotFound() : Ok(type);
    }

    [HttpPost("calculate-premium")]
    public async Task<IActionResult> CalculatePremium([FromBody] PremiumCalculationDto dto)
    {
        try
        {
            var result = await _policyService.CalculatePremiumAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var result = await _policyService.CreatePolicyAsync(userId, dto);
            _publisher.PublishPolicyCreated(result.Id, userId, result.PolicyNumber);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyPolicies()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var policies = await _policyService.GetMyPoliciesAsync(userId);
        return Ok(policies);
    }

    [HttpGet("my/payments")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> GetMyPayments()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var payments = await _policyService.GetMyPaymentsAsync(userId);
        return Ok(payments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPolicy(int id)
    {
        var policy = await _policyService.GetPolicyByIdAsync(id);
        return policy is null ? NotFound() : Ok(policy);
    }

    [HttpGet("{id}/payment")]
    public async Task<IActionResult> GetPolicyPayment(int id)
    {
        try
        {
            var payment = await _policyService.GetPaymentByPolicyIdAsync(id);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
    {
        try
        {
            // Validate status is a valid PolicyStatus enum value
            if (!Enum.TryParse<PolicyStatus>(status, true, out _))
                return BadRequest(new { message = "Invalid policy status" });

            var result = await _policyService.UpdatePolicyStatusAsync(id, status);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound(new { message = "Policy not found" });
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("admin/count")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAdminPolicyCount()
    {
        var totalPolicies = await _policyService.GetTotalPoliciesCountAsync();
        var totalRevenue = await _policyService.GetTotalRevenueAsync();

        return Ok(new
        {
            totalPolicies,
            totalRevenue
        });
    }

    [HttpGet("admin/ado/stats")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetStatsViaAdo()
    {
        var count = await _adoRepository.GetTotalPoliciesCountAsync();
        var revenue = await _adoRepository.GetTotalRevenueAsync();
        var activePolicies = await _adoRepository.GetPoliciesByStatusAsync(1);

        return Ok(new
        {
            message = "Data fetched using ADO.NET (raw SQL - no EF Core)",
            totalPoliciesViaAdo = count,
            totalRevenueViaAdo = revenue,
            activePoliciesCount = activePolicies.Rows.Count
        });
    }
}