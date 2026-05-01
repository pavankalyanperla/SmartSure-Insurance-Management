using PolicyService.Domain.Enums;

namespace PolicyService.Domain.Entities;

public class Policy
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PolicyTypeId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public PolicyStatus Status { get; set; } = PolicyStatus.Draft;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PremiumAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRenewed { get; set; } = false;
    public int? RenewedFromPolicyId { get; set; }
    public int RenewalCount { get; set; } = 0;

    public PolicyType PolicyType { get; set; } = null!;
    public Premium? Premium { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}