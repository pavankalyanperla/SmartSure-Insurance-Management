namespace AdminService.Domain.Entities;

public class AdminLog
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public string Action { get; set; } = null!;
    public string TargetType { get; set; } = null!;
    public int TargetId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
