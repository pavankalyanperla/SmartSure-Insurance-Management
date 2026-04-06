namespace AdminService.Application.Interfaces;

using AdminService.Application.DTOs;

public interface IAdminService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<List<ClaimReviewDto>> GetPendingClaimsAsync();
    Task<List<ClaimReviewDto>> GetAllClaimsAsync();
    Task<ClaimReviewDto> UpdateClaimStatusAsync(UpdateClaimStatusDto dto, int adminId);
    Task<List<UserManagementDto>> GetAllUsersAsync();
    Task<UserManagementDto> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto);
    Task<DashboardSummaryDto> GenerateReportAsync(string reportType, int adminId);
    Task<List<AdminLogDto>> GetAdminLogsAsync();
}
