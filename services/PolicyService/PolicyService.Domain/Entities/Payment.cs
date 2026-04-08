namespace PolicyService.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Online";
    public string Status { get; set; } = "Success";
    public string TransactionId { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public Policy Policy { get; set; } = null!;
}