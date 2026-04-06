namespace AdminService.Application.DTOs;

public class DashboardSummaryDto
{
    public int TotalUsers { get; set; }
    public int TotalPolicies { get; set; }
    public int TotalClaims { get; set; }
    public int PendingClaims { get; set; }
    public int ApprovedClaims { get; set; }
    public int RejectedClaims { get; set; }
    public int ClosedClaims { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class ClaimReviewDto
{
    public int ClaimId { get; set; }
    public string ClaimNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public int PolicyId { get; set; }
    public DateTime IncidentDate { get; set; }
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class UpdateClaimStatusDto
{
    public int ClaimId { get; set; }
    public string Status { get; set; } = null!;
    public string? AdminNote { get; set; }
}

public class UserManagementDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserStatusDto
{
    public bool IsActive { get; set; }
}

public class ReportResponseDto
{
    public int Id { get; set; }
    public string ReportType { get; set; } = null!;
    public DateTime GeneratedAt { get; set; }
    public string Data { get; set; } = null!;
}

public class AdminLogDto
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public string Action { get; set; } = null!;
    public string TargetType { get; set; } = null!;
    public int TargetId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
