using ClaimsService.Application.DTOs;
using ClaimsService.Application.Interfaces;
using ClaimsService.API.Models;
using ClaimsService.Infrastructure.Messaging;
using ClaimsService.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClaimsService.API.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly FileStorageService _fileStorageService;
    private readonly RabbitMQPublisher _publisher;

    public ClaimsController(
        IClaimService claimService,
        FileStorageService fileStorageService,
        RabbitMQPublisher publisher)
    {
        _claimService = claimService;
        _fileStorageService = fileStorageService;
        _publisher = publisher;
    }

    [HttpPost]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> CreateClaim([FromBody] CreateClaimDto dto)
    {
        var customerId = ExtractCustomerId();
        if (customerId is null)
            return Unauthorized();

        try
        {
            var result = await _claimService.CreateClaimAsync(customerId.Value, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> SubmitClaim(int id)
    {
        var customerId = ExtractCustomerId();
        if (customerId is null)
            return Unauthorized();

        try
        {
            var result = await _claimService.SubmitClaimAsync(id, customerId.Value);
            _publisher.PublishClaimSubmitted(result.Id, result.CustomerId, result.ClaimNumber);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/documents")]
    [Authorize(Roles = "CUSTOMER")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(int id, [FromForm] UploadClaimDocumentRequest request)
    {
        var customerId = ExtractCustomerId();
        if (customerId is null)
            return Unauthorized();

        var claim = await _claimService.GetClaimByIdAsync(id);
        if (claim is null)
            return NotFound();

        if (claim.CustomerId != customerId.Value)
            return Forbid();

        try
        {
            var savedPath = await _fileStorageService.SaveFileAsync(request.File, id);
            var document = await _claimService.AddDocumentAsync(
                id,
                request.File.FileName,
                savedPath,
                request.File.ContentType,
                request.File.Length);

            return Ok(document);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> GetMyClaims()
    {
        var customerId = ExtractCustomerId();
        if (customerId is null)
            return Unauthorized();

        var claims = await _claimService.GetMyClaimsAsync(customerId.Value);
        return Ok(claims);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetClaimById(int id)
    {
        var claim = await _claimService.GetClaimByIdAsync(id);
        return claim is null ? NotFound() : Ok(claim);
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllClaims()
    {
        var claims = await _claimService.GetAllClaimsAsync();
        return Ok(claims);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateClaimStatus(int id, [FromBody] UpdateClaimStatusDto dto)
    {
        try
        {
            var result = await _claimService.UpdateClaimStatusAsync(id, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("admin/stats")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetClaimsStats()
    {
        var stats = await _claimService.GetClaimsStatsAsync();
        return Ok(stats);
    }

    private int? ExtractCustomerId()
    {
        var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

        return int.TryParse(customerIdClaim, out var customerId) ? customerId : null;
    }
}
