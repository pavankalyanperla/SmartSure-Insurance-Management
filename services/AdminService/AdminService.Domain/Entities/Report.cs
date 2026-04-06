namespace AdminService.Domain.Entities;

using AdminService.Domain.Enums;

public class Report
{
    public int Id { get; set; }
    public ReportType ReportType { get; set; }
    public int GeneratedBy { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Data { get; set; } = null!;
}
