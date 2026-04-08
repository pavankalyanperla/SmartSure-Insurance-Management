using PolicyService.Domain.Enums;

namespace PolicyService.Application.DTOs;

public class CreatePolicyDto
{
    public int PolicyTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Age { get; set; }
}

public class PolicyResponseDto
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PremiumAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PolicyTypeResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
}

public class PremiumCalculationDto
{
    public int PolicyTypeId { get; set; }
    public int Age { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class PremiumResponseDto
{
    public decimal BaseAmount { get; set; }
    public decimal AgeFactor { get; set; }
    public decimal DurationFactor { get; set; }
    public decimal FinalAmount { get; set; }
}

public class PaymentResponseDto
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
}