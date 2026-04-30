namespace AdminService.Application.Services;

using System.Net.Http.Headers;
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

    private string IdentityBase => _config["ServiceUrls:IdentityService"] ?? "http://localhost:5265";
    private string PolicyBase  => _config["ServiceUrls:PolicyService"]   ?? "http://localhost:5145";
    private string ClaimsBase  => _config["ServiceUrls:ClaimsService"]   ?? "http://localhost:5084";

    // Case-insensitive so camelCase JSON from downstream services maps to PascalCase C# DTOs
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

        // Fire all three downstream calls in parallel to avoid sequential wait
        var userTask    = GetWithAuthAsync($"{IdentityBase}/api/auth/admin/users/count", token);
        var policyTask  = GetWithAuthAsync($"{PolicyBase}/api/policies/admin/count", token);
        var claimsTask  = GetWithAuthAsync($"{ClaimsBase}/api/claims/admin/stats", token);
        await Task.WhenAll(userTask, policyTask, claimsTask);

        try
        {
            var body = await userTask;
            if (!string.IsNullOrEmpty(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("totalUsers",    out var el))  dto.TotalUsers    = el.GetInt32();
                if (doc.RootElement.TryGetProperty("activeUsers",   out var el2)) dto.ActiveUsers   = el2.GetInt32();
                if (doc.RootElement.TryGetProperty("inactiveUsers", out var el3)) dto.InactiveUsers = el3.GetInt32();
            }
        }
        catch (Exception ex) { _logger.LogWarning("IdentityService user count failed: {Message}", ex.Message); }

        try
        {
            var body = await policyTask;
            if (!string.IsNullOrEmpty(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("totalPolicies", out var el))  dto.TotalPolicies = el.GetInt32();
                if (doc.RootElement.TryGetProperty("totalRevenue",  out var el2)) dto.TotalRevenue   = el2.GetDecimal();
            }
        }
        catch (Exception ex) { _logger.LogWarning("PolicyService count failed: {Message}", ex.Message); }

        try
        {
            var body = await claimsTask;
            if (!string.IsNullOrEmpty(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("totalClaims",       out var el))  dto.TotalClaims     = el.GetInt32();
                if (root.TryGetProperty("submittedClaims",   out var el2)) dto.PendingClaims   = el2.GetInt32();
                if (root.TryGetProperty("underReviewClaims", out var el3)) dto.PendingClaims  += el3.GetInt32();
                if (root.TryGetProperty("approvedClaims",    out var el4)) dto.ApprovedClaims  = el4.GetInt32();
                if (root.TryGetProperty("rejectedClaims",    out var el5)) dto.RejectedClaims  = el5.GetInt32();
                if (root.TryGetProperty("closedClaims",      out var el6)) dto.ClosedClaims    = el6.GetInt32();
            }
        }
        catch (Exception ex) { _logger.LogWarning("ClaimsService stats failed: {Message}", ex.Message); }

        return dto;
    }

    public async Task<List<ClaimReviewDto>> GetPendingClaimsAsync()
    {
        var claims = new List<ClaimReviewDto>();
        try
        {
            var token = GetJwtToken();
            var body = await GetWithAuthAsync($"{ClaimsBase}/api/claims", token);
            if (!string.IsNullOrEmpty(body))
            {
                var all = JsonSerializer.Deserialize<List<ClaimsApiItem>>(body, CaseInsensitiveOptions);
                if (all != null)
                    claims = all.Where(c => c.Status == "Submitted" || c.Status == "UnderReview")
                                .Select(MapToClaim).ToList();
            }
        }
        catch (Exception ex) { _logger.LogWarning("ClaimsService pending claims failed: {Message}", ex.Message); }
        return claims;
    }

    public async Task<List<ClaimReviewDto>> GetAllClaimsAsync()
    {
        var claims = new List<ClaimReviewDto>();
        try
        {
            var token = GetJwtToken();
            var body = await GetWithAuthAsync($"{ClaimsBase}/api/claims", token);
            if (!string.IsNullOrEmpty(body))
            {
                var all = JsonSerializer.Deserialize<List<ClaimsApiItem>>(body, CaseInsensitiveOptions);
                if (all != null)
                    claims = all.Select(MapToClaim).ToList();
            }
        }
        catch (Exception ex) { _logger.LogWarning("ClaimsService all claims failed: {Message}", ex.Message); }
        return claims;
    }

    public async Task<ClaimReviewDto> UpdateClaimStatusAsync(UpdateClaimStatusDto dto, int adminId)
    {
        try
        {
            var token = GetJwtToken();
            var url     = $"{ClaimsBase}/api/claims/{dto.ClaimId}/status";
            var payload = JsonSerializer.Serialize(new { status = dto.Status, adminNote = dto.AdminNote });

            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
            };
            SetAuthHeader(request, token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            await _repository.CreateLogAsync(new AdminLog
            {
                AdminId    = adminId,
                Action     = "UpdateClaimStatus",
                TargetType = "Claim",
                TargetId   = dto.ClaimId,
                Notes      = dto.AdminNote,
                CreatedAt  = DateTime.UtcNow
            });

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
            var body = await GetWithAuthAsync($"{IdentityBase}/api/auth/admin/users", token);
            if (!string.IsNullOrEmpty(body))
            {
                // IdentityService returns { id, fullName, … } (not userId), so use a local shape
                var raw = JsonSerializer.Deserialize<List<IdentityUserItem>>(body, CaseInsensitiveOptions);
                if (raw != null)
                    users = raw.Select(u => new UserManagementDto
                    {
                        UserId    = u.Id,
                        FullName  = u.FullName,
                        Email     = u.Email,
                        Role      = u.Role,
                        IsActive  = u.IsActive,
                        CreatedAt = u.CreatedAt
                    }).ToList();
            }
        }
        catch (Exception ex) { _logger.LogWarning("IdentityService users failed: {Message}", ex.Message); }
        return users;
    }

    private sealed class IdentityUserItem
    {
        public int      Id        { get; set; }
        public string   FullName  { get; set; } = string.Empty;
        public string   Email     { get; set; } = string.Empty;
        public string   Role      { get; set; } = string.Empty;
        public bool     IsActive  { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public async Task<UserManagementDto> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto)
    {
        try
        {
            var token   = GetJwtToken();
            var url     = $"{IdentityBase}/api/auth/admin/users/{userId}/status";
            var payload = JsonSerializer.Serialize(new { isActive = dto.IsActive });

            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
            };
            SetAuthHeader(request, token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            await _repository.CreateLogAsync(new AdminLog
            {
                AdminId    = ExtractAdminIdFromContext(),
                Action     = "UpdateUserStatus",
                TargetType = "User",
                TargetId   = userId,
                Notes      = $"Set IsActive to {dto.IsActive}",
                CreatedAt  = DateTime.UtcNow
            });

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

            // TryParse so an unknown reportType string doesn't throw
            if (!Enum.TryParse<Domain.Enums.ReportType>(reportType, ignoreCase: true, out var parsedType))
                parsedType = Domain.Enums.ReportType.ClaimsSummary;

            var report = new Report
            {
                ReportType  = parsedType,
                GeneratedBy = adminId,
                GeneratedAt = DateTime.UtcNow,
                Data        = JsonSerializer.Serialize(dashboardData)
            };
            await _repository.CreateReportAsync(report);

            await _repository.CreateLogAsync(new AdminLog
            {
                AdminId    = adminId,
                Action     = "GenerateReport",
                TargetType = "Report",
                TargetId   = report.Id,
                Notes      = $"Generated {reportType} report",
                CreatedAt  = DateTime.UtcNow
            });

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
                Id         = log.Id,
                AdminId    = log.AdminId,
                Action     = log.Action,
                TargetType = log.TargetType,
                TargetId   = log.TargetId,
                Notes      = log.Notes,
                CreatedAt  = log.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get admin logs: {Message}", ex.Message);
            throw;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string?> GetWithAuthAsync(string url, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        SetAuthHeader(request, token);
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
    }

    // Sets Authorization header on the individual request (avoids mutating shared DefaultRequestHeaders)
    private static void SetAuthHeader(HttpRequestMessage request, string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var bearer = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token["Bearer ".Length..]
            : token;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
    }

    private string? GetJwtToken()
        => _httpContextAccessor?.HttpContext?.Request.Headers["Authorization"].ToString();

    private int ExtractAdminIdFromContext()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user == null) return 0;
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        return int.TryParse(raw, out var id) ? id : 0;
    }

    private static ClaimReviewDto MapToClaim(ClaimsApiItem c) => new()
    {
        ClaimId      = c.Id,
        ClaimNumber  = c.ClaimNumber,
        CustomerName = $"Customer #{c.CustomerId}",
        PolicyId     = c.PolicyId,
        IncidentDate = c.IncidentDate,
        Description  = c.Description,
        Status       = c.Status,
        AdminNote    = c.AdminNote,
        CreatedAt    = c.CreatedAt
    };

    // Matches the shape ClaimsService actually returns (id, not claimId)
    private sealed class ClaimsApiItem
    {
        public int      Id          { get; set; }
        public string   ClaimNumber { get; set; } = string.Empty;
        public int      PolicyId    { get; set; }
        public int      CustomerId  { get; set; }
        public DateTime IncidentDate{ get; set; }
        public string   Description { get; set; } = string.Empty;
        public string   Status      { get; set; } = string.Empty;
        public string?  AdminNote   { get; set; }
        public DateTime CreatedAt   { get; set; }
    }
}
