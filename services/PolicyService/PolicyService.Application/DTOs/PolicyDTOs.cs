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
    public bool IsActive { get; set; }

    // Coverage details shown to the customer on the buy-policy page
    public string CoverageDetails { get; set; } = string.Empty;
    public string Exclusions { get; set; } = string.Empty;
    public decimal ClaimLimit { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public int DurationMonths { get; set; }
    public string RiskCategory { get; set; } = string.Empty;
    public bool AutoRenewal { get; set; }
    public int GracePeriodDays { get; set; }
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
    public decimal AgeFactorAmount { get; set; }
    public decimal DurationFactor { get; set; }
    public decimal DurationFactorAmount { get; set; }
    public int DurationYears { get; set; }
    public decimal FinalAmount { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
    public string FormulaExplanation { get; set; } = string.Empty;
}

public class RenewPolicyDto
{
    public int PolicyId { get; set; }
    public int Age { get; set; }
}

public class RenewalResponseDto
{
    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public DateTime NewStartDate { get; set; }
    public DateTime NewEndDate { get; set; }
    public decimal NewPremiumAmount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public int RenewalCount { get; set; }
    public string Message { get; set; } = string.Empty;
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