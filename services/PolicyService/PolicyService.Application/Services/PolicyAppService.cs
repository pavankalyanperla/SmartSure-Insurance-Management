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
            BaseAmount = t.BaseAmount
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
            BaseAmount = type.BaseAmount
        };
    }

    public async Task<PremiumResponseDto> CalculatePremiumAsync(PremiumCalculationDto dto)
    {
        var policyType = await _repo.GetPolicyTypeByIdAsync(dto.PolicyTypeId)
            ?? throw new InvalidOperationException("Policy type not found.");

        var durationMonths = ((dto.EndDate.Year - dto.StartDate.Year) * 12)
                           + dto.EndDate.Month - dto.StartDate.Month;

        decimal ageFactor = dto.Age switch
        {
            <= 25 => 1.1m,
            <= 40 => 1.0m,
            <= 55 => 1.2m,
            _ => 1.5m
        };

        decimal durationFactor = durationMonths switch
        {
            <= 6 => 1.0m,
            <= 12 => 1.05m,
            <= 24 => 1.1m,
            _ => 1.2m
        };

        decimal baseAmount = policyType.BaseAmount;
        decimal finalAmount = baseAmount * ageFactor * durationFactor;

        return new PremiumResponseDto
        {
            BaseAmount = baseAmount,
            AgeFactor = ageFactor,
            DurationFactor = durationFactor,
            FinalAmount = Math.Round(finalAmount, 2)
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