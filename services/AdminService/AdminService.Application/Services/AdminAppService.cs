namespace AdminService.Application.Services;

using System.Security.Claims;
using System.Text.Json;
using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class AdminAppService : IAdminService
{
    private readonly IAdminRepository _repository;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminAppService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminAppService(
        IAdminRepository repository,
        HttpClient httpClient,
        IConfiguration config,
        ILogger<AdminAppService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var dto = new DashboardSummaryDto();
        var token = GetJwtToken();

        try
        {
            var userCountResponse = await GetServiceDataWithAuthAsync("http://localhost:5265/api/auth/admin/users/count", token);
            if (!string.IsNullOrEmpty(userCountResponse))
            {
                using var doc = JsonDocument.Parse(userCountResponse);
                if (doc.RootElement.TryGetProperty("totalUsers", out var totalUsersElement))
                {
                    dto.TotalUsers = totalUsersElement.GetInt32();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get user count from IdentityService: {Message}", ex.Message);
        }

        try
        {
            var policyResponse = await GetServiceDataWithAuthAsync("http://localhost:5145/api/policies/admin/count", token);
            if (!string.IsNullOrEmpty(policyResponse))
            {
                using var doc = JsonDocument.Parse(policyResponse);
                var root = doc.RootElement;
                if (root.TryGetProperty("totalPolicies", out var totalPoliciesElement))
                {
                    dto.TotalPolicies = totalPoliciesElement.GetInt32();
                }

                if (root.TryGetProperty("totalRevenue", out var totalRevenueElement))
                {
                    dto.TotalRevenue = totalRevenueElement.GetDecimal();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get policy count from PolicyService: {Message}", ex.Message);
        }

        try
        {
            var claimsStatsResponse = await GetServiceDataWithAuthAsync("http://localhost:5084/api/claims/admin/stats", token);
            if (!string.IsNullOrEmpty(claimsStatsResponse))
            {
                using var doc = JsonDocument.Parse(claimsStatsResponse);
                var root = doc.RootElement;
                if (root.TryGetProperty("totalClaims", out var totalClaimsElement))
                {
                    dto.TotalClaims = totalClaimsElement.GetInt32();
                }
                if (root.TryGetProperty("submittedClaims", out var submittedClaimsElement))
                {
                    var submittedCount = submittedClaimsElement.GetInt32();
                    dto.PendingClaims = submittedCount;
                }
                if (root.TryGetProperty("underReviewClaims", out var underReviewClaimsElement))
                {
                    var underReviewCount = underReviewClaimsElement.GetInt32();
                    dto.PendingClaims += underReviewCount;
                }
                if (root.TryGetProperty("approvedClaims", out var approvedClaimsElement))
                {
                    dto.ApprovedClaims = approvedClaimsElement.GetInt32();
                }
                if (root.TryGetProperty("rejectedClaims", out var rejectedClaimsElement))
                {
                    dto.RejectedClaims = rejectedClaimsElement.GetInt32();
                }
                if (root.TryGetProperty("closedClaims", out var closedClaimsElement))
                {
                    dto.ClosedClaims = closedClaimsElement.GetInt32();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get claims stats from ClaimsService: {Message}", ex.Message);
        }

        return dto;
    }

    public async Task<List<ClaimReviewDto>> GetPendingClaimsAsync()
    {
        var claims = new List<ClaimReviewDto>();

        try
        {
            var token = GetJwtToken();
            var response = await GetServiceDataWithAuthAsync("http://localhost:5084/api/claims", token);
            if (!string.IsNullOrEmpty(response))
            {
                var allClaims = JsonSerializer.Deserialize<List<ClaimReviewDto>>(response);
                if (allClaims != null)
                {
                    claims = allClaims.Where(c => c.Status == "Submitted" || c.Status == "UnderReview").ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get pending claims from ClaimsService: {Message}", ex.Message);
        }

        return claims;
    }

    public async Task<List<ClaimReviewDto>> GetAllClaimsAsync()
    {
        var claims = new List<ClaimReviewDto>();

        try
        {
            var token = GetJwtToken();
            var response = await GetServiceDataWithAuthAsync("http://localhost:5084/api/claims", token);
            if (!string.IsNullOrEmpty(response))
            {
                var allClaims = JsonSerializer.Deserialize<List<ClaimReviewDto>>(response);
                if (allClaims != null)
                {
                    claims = allClaims;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get all claims from ClaimsService: {Message}", ex.Message);
        }

        return claims;
    }

    public async Task<ClaimReviewDto> UpdateClaimStatusAsync(UpdateClaimStatusDto dto, int adminId)
    {
        try
        {
            var token = GetJwtToken();
            var url = $"http://localhost:5084/api/claims/{dto.ClaimId}/status";
            
            var updatePayload = new { status = dto.Status, adminNote = dto.AdminNote };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(updatePayload),
                System.Text.Encoding.UTF8,
                "application/json");

            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
            }

            using (var request = new HttpRequestMessage(HttpMethod.Put, url))
            {
                request.Content = jsonContent;
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }

            // Log the action
            var adminLog = new AdminLog
            {
                AdminId = adminId,
                Action = "UpdateClaimStatus",
                TargetType = "Claim",
                TargetId = dto.ClaimId,
                Notes = dto.AdminNote,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.CreateLogAsync(adminLog);

            return new ClaimReviewDto { ClaimId = dto.ClaimId };
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update claim status: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<UserManagementDto>> GetAllUsersAsync()
    {
        var users = new List<UserManagementDto>();

        try
        {
            var token = GetJwtToken();
            var response = await GetServiceDataWithAuthAsync("http://localhost:5265/api/auth/admin/users", token);
            if (!string.IsNullOrEmpty(response))
            {
                var allUsers = JsonSerializer.Deserialize<List<UserManagementDto>>(response);
                if (allUsers != null)
                {
                    users = allUsers;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get all users from IdentityService: {Message}", ex.Message);
        }

        return users;
    }

    public async Task<UserManagementDto> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto)
    {
        try
        {
            var token = GetJwtToken();
            var url = $"http://localhost:5265/api/auth/admin/users/{userId}/status";

            var updatePayload = new { isActive = dto.IsActive };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(updatePayload),
                System.Text.Encoding.UTF8,
                "application/json");

            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
            }

            using (var request = new HttpRequestMessage(HttpMethod.Put, url))
            {
                request.Content = jsonContent;
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }

            // Log the action
            var adminId = ExtractAdminIdFromContext();
            var adminLog = new AdminLog
            {
                AdminId = adminId,
                Action = "UpdateUserStatus",
                TargetType = "User",
                TargetId = userId,
                Notes = $"Set IsActive to {dto.IsActive}",
                CreatedAt = DateTime.UtcNow
            };
            await _repository.CreateLogAsync(adminLog);

            return new UserManagementDto { UserId = userId };
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update user status: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<DashboardSummaryDto> GenerateReportAsync(string reportType, int adminId)
    {
        try
        {
            var dashboardData = await GetDashboardSummaryAsync();
            var jsonData = JsonSerializer.Serialize(dashboardData);

            var report = new Report
            {
                ReportType = (Domain.Enums.ReportType)Enum.Parse(typeof(Domain.Enums.ReportType), reportType),
                GeneratedBy = adminId,
                GeneratedAt = DateTime.UtcNow,
                Data = jsonData
            };

            await _repository.CreateReportAsync(report);

            // Log the action
            var adminLog = new AdminLog
            {
                AdminId = adminId,
                Action = "GenerateReport",
                TargetType = "Report",
                TargetId = report.Id,
                Notes = $"Generated {reportType} report",
                CreatedAt = DateTime.UtcNow
            };
            await _repository.CreateLogAsync(adminLog);

            return dashboardData;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to generate report: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<AdminLogDto>> GetAdminLogsAsync()
    {
        try
        {
            var logs = await _repository.GetLogsAsync();
            return logs.Select(log => new AdminLogDto
            {
                Id = log.Id,
                AdminId = log.AdminId,
                Action = log.Action,
                TargetType = log.TargetType,
                TargetId = log.TargetId,
                Notes = log.Notes,
                CreatedAt = log.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get admin logs: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<string?> GetServiceDataAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        return null;
    }

    private async Task<string?> GetServiceDataWithAuthAsync(string url, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
        }

        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }
    }

    private string? GetJwtToken()
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context != null)
        {
            return context.Request.Headers["Authorization"].ToString();
        }
        return null;
    }

    private int ExtractAdminIdFromContext()
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context?.User != null)
        {
            var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                          context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out var id))
            {
                return id;
            }
        }
        return 0;
    }
}
