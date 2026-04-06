namespace PolicyService.Domain.Entities;

public class Premium
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal AgeFactor { get; set; }
    public decimal DurationFactor { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public Policy Policy { get; set; } = null!;
}