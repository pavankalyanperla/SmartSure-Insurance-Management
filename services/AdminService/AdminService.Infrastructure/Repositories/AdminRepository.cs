namespace AdminService.Infrastructure.Repositories;

using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using AdminService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class AdminRepository : IAdminRepository
{
    private readonly AdminDbContext _context;

    public AdminRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<List<AdminLog>> GetLogsAsync()
    {
        return await _context.AdminLogs
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync();
    }

    public async Task<AdminLog> CreateLogAsync(AdminLog log)
    {
        _context.AdminLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<Report> CreateReportAsync(Report report)
    {
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<List<Report>> GetReportsAsync()
    {
        return await _context.Reports
            .OrderByDescending(report => report.GeneratedAt)
            .ToListAsync();
    }
}
