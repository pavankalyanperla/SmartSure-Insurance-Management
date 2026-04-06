using ClaimsService.Application.DTOs;

namespace ClaimsService.Application.Interfaces;

public interface IClaimService
{
    Task<ClaimResponseDto> CreateClaimAsync(int customerId, CreateClaimDto dto);
    Task<ClaimResponseDto> SubmitClaimAsync(int claimId, int customerId);
    Task<ClaimResponseDto?> GetClaimByIdAsync(int id);
    Task<List<ClaimResponseDto>> GetMyClaimsAsync(int customerId);
    Task<List<ClaimResponseDto>> GetAllClaimsAsync();
    Task<ClaimResponseDto> UpdateClaimStatusAsync(int claimId, UpdateClaimStatusDto dto);
    Task<ClaimDocumentDto> AddDocumentAsync(int claimId, string fileName, string filePath, string fileType, long fileSize);
    Task<object> GetClaimsStatsAsync();
}
