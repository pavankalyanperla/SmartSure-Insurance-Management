using PolicyService.Application.DTOs;
using PolicyService.Application.Interfaces;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Domain.Interfaces;

namespace PolicyService.Application.Services;

public class PolicyAppService : IPolicyService
{
    private readonly IPolicyRepository _repo;

    public PolicyAppService(IPolicyRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<PolicyTypeResponseDto>> GetAllPolicyTypesAsync()
    {
        var types = await _repo.GetAllPolicyTypesAsync();
        return types.Select(t => new PolicyTypeResponseDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            BaseAmount = t.BaseAmount,
            IsActive = t.IsActive
        }).ToList();
    }

    public async Task<PolicyTypeResponseDto?> GetPolicyTypeByIdAsync(int id)
    {
        var type = await _repo.GetPolicyTypeByIdAsync(id);
        if (type is null) return null;
        return new PolicyTypeResponseDto
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            BaseAmount = type.BaseAmount,
            IsActive = type.IsActive
        };
    }

    public async Task<PremiumResponseDto> CalculatePremiumAsync(PremiumCalculationDto dto)
    {
        var policyType = await _repo.GetPolicyTypeByIdAsync(dto.PolicyTypeId)
            ?? throw new InvalidOperationException("Policy type not found.");

        var totalDays = (dto.EndDate - dto.StartDate).TotalDays;
        var years = (int)Math.Ceiling(totalDays / 365.0);

        decimal ageFactor = dto.Age switch
        {
            <= 25 => 0.10m,
            <= 40 => 0.00m,
            <= 55 => 0.20m,
            _     => 0.50m
        };

        // Year 1 → 0.10, Year 2 → 1.10, Year 3 → 2.10, etc.
        decimal durationFactor = (years - 1) + 0.10m;

        string ageGroup = dto.Age switch
        {
            <= 25 => "18-25 years",
            <= 40 => "26-40 years",
            <= 55 => "41-55 years",
            _     => "56+ years"
        };

        decimal baseAmount          = policyType.BaseAmount;
        decimal ageFactorAmount     = Math.Round(baseAmount * ageFactor, 2);
        decimal durationFactorAmount = Math.Round(baseAmount * durationFactor, 2);
        decimal finalAmount          = baseAmount + ageFactorAmount + durationFactorAmount;

        return new PremiumResponseDto
        {
            BaseAmount           = baseAmount,
            AgeFactor            = ageFactor,
            AgeFactorAmount      = ageFactorAmount,
            DurationFactor       = durationFactor,
            DurationFactorAmount = durationFactorAmount,
            DurationYears        = years,
            FinalAmount          = Math.Round(finalAmount, 2),
            AgeGroup             = ageGroup,
            FormulaExplanation   = $"{baseAmount:0} + {ageFactorAmount:0} + {durationFactorAmount:0} = {Math.Round(finalAmount, 0)}"
        };
    }

    public async Task<PolicyResponseDto> CreatePolicyAsync(int userId, CreatePolicyDto dto)
    {
        var policyType = await _repo.GetPolicyTypeByIdAsync(dto.PolicyTypeId)
            ?? throw new InvalidOperationException("Policy type not found.");

        var premiumDto = new PremiumCalculationDto
        {
            PolicyTypeId = dto.PolicyTypeId,
            Age = dto.Age,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
        var premium = await CalculatePremiumAsync(premiumDto);

        var policy = new Policy
        {
            UserId = userId,
            PolicyTypeId = dto.PolicyTypeId,
            PolicyNumber = $"POL-{DateTime.UtcNow.Ticks}",
            Status = PolicyStatus.Active,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            PremiumAmount = premium.FinalAmount,
            Premium = new Premium
            {
                BaseAmount = premium.BaseAmount,
                AgeFactor = premium.AgeFactor,
                DurationFactor = premium.DurationFactor,
                FinalAmount = premium.FinalAmount
            }
        };

        var created = await _repo.CreatePolicyAsync(policy);

        var payment = new Payment
        {
            PolicyId = created.Id,
            UserId = userId,
            Amount = premium.FinalAmount,
            PaymentMethod = "Online",
            Status = "Success",
            TransactionId = $"TXN-{DateTime.UtcNow.Ticks}"
        };
        await _repo.CreatePaymentAsync(payment);

        return MapToResponse(created, policyType.Name);
    }

    public async Task<List<PolicyResponseDto>> GetMyPoliciesAsync(int userId)
    {
        var policies = await _repo.GetPoliciesByUserIdAsync(userId);
        return policies.Select(p => MapToResponse(p, p.PolicyType.Name)).ToList();
    }

    public async Task<PolicyResponseDto?> GetPolicyByIdAsync(int id)
    {
        var policy = await _repo.GetPolicyByIdAsync(id);
        if (policy is null) return null;
        return MapToResponse(policy, policy.PolicyType.Name);
    }

    public async Task<PolicyResponseDto> UpdatePolicyStatusAsync(int policyId, string status)
    {
        var policy = await _repo.GetPolicyByIdAsync(policyId)
            ?? throw new InvalidOperationException("Policy not found.");

        policy.Status = Enum.Parse<PolicyStatus>(status, ignoreCase: true);
        await _repo.UpdatePolicyAsync(policy);
        return MapToResponse(policy, policy.PolicyType.Name);
    }

    public Task<int> GetTotalPoliciesCountAsync()
    {
        return _repo.GetTotalPoliciesCountAsync();
    }

    public Task<decimal> GetTotalRevenueAsync()
    {
        return _repo.GetTotalRevenueAsync();
    }

    public async Task<PaymentResponseDto> GetPaymentByPolicyIdAsync(int policyId)
    {
        var payment = await _repo.GetPaymentByPolicyIdAsync(policyId)
            ?? throw new InvalidOperationException("Payment not found.");

        return MapPaymentToResponse(payment);
    }

    public async Task<List<PaymentResponseDto>> GetMyPaymentsAsync(int userId)
    {
        var payments = await _repo.GetPaymentsByUserIdAsync(userId);
        return payments.Select(MapPaymentToResponse).ToList();
    }

    public async Task<RenewalResponseDto> RenewPolicyAsync(int policyId, RenewPolicyDto dto, int userId)
    {
        var policy = await _repo.GetPolicyByIdAsync(policyId)
            ?? throw new KeyNotFoundException($"Policy {policyId} not found");

        if (policy.UserId != userId)
            throw new UnauthorizedAccessException("Not your policy");

        if (policy.Status != PolicyStatus.Active && policy.Status != PolicyStatus.Expired)
            throw new InvalidOperationException("Only Active or Expired policies can be renewed");

        var policyType = await _repo.GetPolicyTypeByIdAsync(policy.PolicyTypeId)
            ?? throw new KeyNotFoundException("Policy type not found");

        var newStartDate = policy.EndDate < DateTime.UtcNow
            ? DateTime.UtcNow
            : policy.EndDate;
        var newEndDate = newStartDate.AddYears(1);

        decimal ageFactor = dto.Age <= 25 ? 0.10m :
                            dto.Age <= 40 ? 0.00m :
                            dto.Age <= 55 ? 0.20m : 0.50m;
        decimal durationFactor = 0.10m;
        decimal newPremium = policyType.BaseAmount
                           + (policyType.BaseAmount * ageFactor)
                           + (policyType.BaseAmount * durationFactor);
        newPremium = Math.Round(newPremium, 2);

        policy.StartDate    = newStartDate;
        policy.EndDate      = newEndDate;
        policy.PremiumAmount = newPremium;
        policy.Status       = PolicyStatus.Active;
        policy.IsRenewed    = true;
        policy.RenewalCount += 1;
        await _repo.UpdatePolicyAsync(policy);

        var transactionId = $"TXN-RENEW-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        await _repo.CreatePaymentAsync(new Payment
        {
            PolicyId      = policyId,
            UserId        = userId,
            Amount        = newPremium,
            PaymentMethod = "Online",
            Status        = "Success",
            TransactionId = transactionId,
            PaymentDate   = DateTime.UtcNow
        });

        return new RenewalResponseDto
        {
            PolicyId         = policyId,
            PolicyNumber     = policy.PolicyNumber,
            NewStartDate     = newStartDate,
            NewEndDate       = newEndDate,
            NewPremiumAmount = newPremium,
            TransactionId    = transactionId,
            RenewalCount     = policy.RenewalCount,
            Message          = $"Policy renewed successfully for 1 year. New expiry: {newEndDate:MMM dd, yyyy}"
        };
    }

    private static PolicyResponseDto MapToResponse(Policy policy, string typeName) => new()
    {
        Id = policy.Id,
        PolicyNumber = policy.PolicyNumber,
        PolicyTypeName = typeName,
        Status = policy.Status.ToString(),
        StartDate = policy.StartDate,
        EndDate = policy.EndDate,
        PremiumAmount = policy.PremiumAmount,
        CreatedAt = policy.CreatedAt
    };

    private static PaymentResponseDto MapPaymentToResponse(Payment payment) => new()
    {
        Id = payment.Id,
        PolicyId = payment.PolicyId,
        Amount = payment.Amount,
        PaymentMethod = payment.PaymentMethod,
        Status = payment.Status,
        TransactionId = payment.TransactionId,
        PaymentDate = payment.PaymentDate
    };
}