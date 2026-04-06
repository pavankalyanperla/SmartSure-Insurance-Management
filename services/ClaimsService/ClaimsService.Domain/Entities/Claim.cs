using ClaimsService.Domain.Enums;

namespace ClaimsService.Domain.Entities;

public class Claim
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public int CustomerId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ClaimDocument> ClaimDocuments { get; set; } = new List<ClaimDocument>();
}
