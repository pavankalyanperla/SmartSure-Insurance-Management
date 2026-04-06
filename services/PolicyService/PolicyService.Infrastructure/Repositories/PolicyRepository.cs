using Microsoft.EntityFrameworkCore;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Interfaces;
using PolicyService.Infrastructure.Data;

namespace PolicyService.Infrastructure.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly PolicyDbContext _db;

    public PolicyRepository(PolicyDbContext db)
    {
        _db = db;
    }

    public async Task<List<PolicyType>> GetAllPolicyTypesAsync()
        => await _db.PolicyTypes.Where(t => t.IsActive).ToListAsync();

    public async Task<PolicyType?> GetPolicyTypeByIdAsync(int id)
        => await _db.PolicyTypes.FindAsync(id);

    public async Task<Policy?> GetPolicyByIdAsync(int id)
        => await _db.Policies
            .Include(p => p.PolicyType)
            .Include(p => p.Premium)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Policy>> GetPoliciesByUserIdAsync(int userId)
        => await _db.Policies
            .Include(p => p.PolicyType)
            .Include(p => p.Premium)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<Policy> CreatePolicyAsync(Policy policy)
    {
        _db.Policies.Add(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    public async Task<Policy> UpdatePolicyAsync(Policy policy)
    {
        _db.Policies.Update(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    public async Task<bool> SaveChangesAsync()
        => await _db.SaveChangesAsync() > 0;

    public async Task<int> GetTotalPoliciesCountAsync()
        => await _db.Policies.CountAsync();

    public async Task<decimal> GetTotalRevenueAsync()
        => await _db.Policies.SumAsync(p => p.PremiumAmount);
}