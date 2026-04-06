using ClaimsService.Domain.Entities;
using ClaimsService.Domain.Interfaces;
using ClaimsService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClaimsService.Infrastructure.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimDbContext _dbContext;

    public ClaimRepository(ClaimDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Claim?> GetByIdAsync(int id)
    {
        return await _dbContext.Claims
            .Include(c => c.ClaimDocuments)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Claim>> GetByCustomerIdAsync(int customerId)
    {
        return await _dbContext.Claims
            .Include(c => c.ClaimDocuments)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Claim>> GetAllAsync()
    {
        return await _dbContext.Claims
            .Include(c => c.ClaimDocuments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Claim> CreateAsync(Claim claim)
    {
        _dbContext.Claims.Add(claim);
        await _dbContext.SaveChangesAsync();
        return claim;
    }

    public async Task<Claim> UpdateAsync(Claim claim)
    {
        _dbContext.Claims.Update(claim);
        await _dbContext.SaveChangesAsync();
        return claim;
    }

    public async Task<ClaimDocument> AddDocumentAsync(ClaimDocument document)
    {
        _dbContext.ClaimDocuments.Add(document);
        await _dbContext.SaveChangesAsync();
        return document;
    }

    public async Task<List<ClaimDocument>> GetDocumentsByClaimIdAsync(int claimId)
    {
        return await _dbContext.ClaimDocuments
            .Where(d => d.ClaimId == claimId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<int> GetCountByStatusAsync(string status)
    {
        return await _dbContext.Claims
            .CountAsync(c => c.Status.ToString() == status);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _dbContext.Claims.CountAsync();
    }
}
