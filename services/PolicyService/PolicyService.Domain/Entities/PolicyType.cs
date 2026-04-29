namespace PolicyService.Domain.Entities;

public class PolicyType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CoverageDetails { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public decimal ClaimLimit { get; set; }
    public string Exclusions { get; set; } = string.Empty;
    public bool AutoRenewal { get; set; }
    public int GracePeriodDays { get; set; }
    public string RiskCategory { get; set; } = string.Empty;

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
