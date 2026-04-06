using IdentityService.API.Helpers;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthRepository _repository;
    private readonly JwtHelper _jwtHelper;

    public AuthController(IAuthService authService, IAuthRepository repository, JwtHelper jwtHelper)
    {
        _authService = authService;
        _repository = repository;
        _jwtHelper = jwtHelper;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto);
            
            // Get the created user and generate token
            var user = await _repository.GetByEmailAsync(dto.Email.ToLower());
            if (user is not null)
            {
                var (token, expiresAt) = _jwtHelper.GenerateToken(user);
                result.Token = token;
                result.ExpiresAt = expiresAt;
            }
            
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            
            // Get the user and generate token
            var user = await _repository.GetByEmailAsync(dto.Email.ToLower());
            if (user is not null)
            {
                var (token, expiresAt) = _jwtHelper.GenerateToken(user);
                result.Token = token;
                result.ExpiresAt = expiresAt;
            }
            
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var profile = await _authService.GetProfileAsync(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = "ADMIN")]
    public IActionResult AdminTest()
        => Ok(new { message = "You are an admin!" });

    [HttpGet("admin/users")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("admin/users/count")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetUsersCount()
    {
        var count = await _authService.GetUsersCountAsync();
        return Ok(new { totalUsers = count });
    }

    [HttpPut("admin/users/{userId}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] UpdateUserStatusRequestDto dto)
    {
        var updated = await _authService.UpdateUserStatusAsync(userId, dto.IsActive);
        return updated ? Ok(new { success = true }) : NotFound();
    }
}