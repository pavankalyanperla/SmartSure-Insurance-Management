using ClaimsService.Domain.Entities;

namespace ClaimsService.Domain.Interfaces;

public interface IClaimRepository
{
    Task<Claim?> GetByIdAsync(int id);
    Task<List<Claim>> GetByCustomerIdAsync(int customerId);
    Task<List<Claim>> GetAllAsync();
    Task<Claim> CreateAsync(Claim claim);
    Task<Claim> UpdateAsync(Claim claim);
    Task<ClaimDocument> AddDocumentAsync(ClaimDocument document);
    Task<List<ClaimDocument>> GetDocumentsByClaimIdAsync(int claimId);
    Task<bool> SaveChangesAsync();
    Task<int> GetCountByStatusAsync(string status);
    Task<int> GetTotalCountAsync();
}
