namespace AdminService.Domain.Interfaces;

using AdminService.Domain.Entities;

public interface IAdminRepository
{
    Task<List<AdminLog>> GetLogsAsync();
    Task<AdminLog> CreateLogAsync(AdminLog log);
    Task<Report> CreateReportAsync(Report report);
    Task<List<Report>> GetReportsAsync();
}
