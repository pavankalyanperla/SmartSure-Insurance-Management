using PolicyService.Domain.Entities;

namespace PolicyService.Domain.Interfaces;

public interface IPolicyRepository
{
    Task<List<PolicyType>> GetAllPolicyTypesAsync();
    Task<PolicyType?> GetPolicyTypeByIdAsync(int id);
    Task<Policy?> GetPolicyByIdAsync(int id);
    Task<List<Policy>> GetPoliciesByUserIdAsync(int userId);
    Task<Policy> CreatePolicyAsync(Policy policy);
    Task<Policy> UpdatePolicyAsync(Policy policy);
    Task<bool> SaveChangesAsync();
    Task<int> GetTotalPoliciesCountAsync();
    Task<decimal> GetTotalRevenueAsync();
}