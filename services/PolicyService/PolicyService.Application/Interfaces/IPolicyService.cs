using PolicyService.Application.DTOs;

namespace PolicyService.Application.Interfaces;

public interface IPolicyService
{
    Task<List<PolicyTypeResponseDto>> GetAllPolicyTypesAsync();
    Task<PolicyTypeResponseDto?> GetPolicyTypeByIdAsync(int id);
    Task<PremiumResponseDto> CalculatePremiumAsync(PremiumCalculationDto dto);
    Task<PolicyResponseDto> CreatePolicyAsync(int userId, CreatePolicyDto dto);
    Task<List<PolicyResponseDto>> GetMyPoliciesAsync(int userId);
    Task<PolicyResponseDto?> GetPolicyByIdAsync(int id);
    Task<PolicyResponseDto> UpdatePolicyStatusAsync(int policyId, string status);
    Task<int> GetTotalPoliciesCountAsync();
    Task<decimal> GetTotalRevenueAsync();
    Task<PaymentResponseDto> GetPaymentByPolicyIdAsync(int policyId);
    Task<List<PaymentResponseDto>> GetMyPaymentsAsync(int userId);
}