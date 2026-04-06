using ClaimsService.Application.DTOs;
using ClaimsService.Application.Interfaces;
using ClaimsService.Domain.Entities;
using ClaimsService.Domain.Enums;
using ClaimsService.Domain.Interfaces;

namespace ClaimsService.Application.Services;

public class ClaimAppService : IClaimService
{
    private readonly IClaimRepository _repository;

    public ClaimAppService(IClaimRepository repository)
    {
        _repository = repository;
    }

    public async Task<ClaimResponseDto> CreateClaimAsync(int customerId, CreateClaimDto dto)
    {
        var claim = new Claim
        {
            PolicyId = dto.PolicyId,
            CustomerId = customerId,
            ClaimNumber = $"CLM-{DateTime.UtcNow.Ticks}",
            IncidentDate = dto.IncidentDate,
            Description = dto.Description.Trim(),
            Status = ClaimStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(claim);
        return MapToResponse(created);
    }

    public async Task<ClaimResponseDto> SubmitClaimAsync(int claimId, int customerId)
    {
        var claim = await _repository.GetByIdAsync(claimId)
            ?? throw new InvalidOperationException("Claim not found.");

        if (claim.CustomerId != customerId)
            throw new UnauthorizedAccessException("You are not allowed to submit this claim.");

        if (claim.Status != ClaimStatus.Draft)
            throw new InvalidOperationException("Claim is already submitted.");

        claim.Status = ClaimStatus.Submitted;
        claim.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(claim);
        return MapToResponse(updated);
    }

    public async Task<ClaimResponseDto?> GetClaimByIdAsync(int id)
    {
        var claim = await _repository.GetByIdAsync(id);
        return claim is null ? null : MapToResponse(claim);
    }

    public async Task<List<ClaimResponseDto>> GetMyClaimsAsync(int customerId)
    {
        var claims = await _repository.GetByCustomerIdAsync(customerId);
        return claims.Select(MapToResponse).ToList();
    }

    public async Task<List<ClaimResponseDto>> GetAllClaimsAsync()
    {
        var claims = await _repository.GetAllAsync();
        return claims.Select(MapToResponse).ToList();
    }

    public async Task<ClaimResponseDto> UpdateClaimStatusAsync(int claimId, UpdateClaimStatusDto dto)
    {
        var claim = await _repository.GetByIdAsync(claimId)
            ?? throw new InvalidOperationException("Claim not found.");

        if (!Enum.TryParse<ClaimStatus>(dto.Status, true, out var newStatus))
            throw new InvalidOperationException("Invalid claim status.");

        // Enforce strict transition rules
        var isValidTransition = (claim.Status, newStatus) switch
        {
            // Submitted can only go to UnderReview
            (ClaimStatus.Submitted, ClaimStatus.UnderReview) => true,
            // UnderReview can go to Approved or Rejected
            (ClaimStatus.UnderReview, ClaimStatus.Approved) => true,
            (ClaimStatus.UnderReview, ClaimStatus.Rejected) => true,
            // Approved or Rejected can go to Closed
            (ClaimStatus.Approved, ClaimStatus.Closed) => true,
            (ClaimStatus.Rejected, ClaimStatus.Closed) => true,
            // Any other transition is invalid
            _ => false
        };

        if (!isValidTransition)
            throw new InvalidOperationException($"Invalid status transition from {claim.Status} to {newStatus}");

        claim.Status = newStatus;
        claim.AdminNote = dto.AdminNote;
        claim.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(claim);
        return MapToResponse(updated);
    }

    public async Task<ClaimDocumentDto> AddDocumentAsync(int claimId, string fileName, string filePath, string fileType, long fileSize)
    {
        var claim = await _repository.GetByIdAsync(claimId)
            ?? throw new InvalidOperationException("Claim not found.");

        var document = new ClaimDocument
        {
            ClaimId = claimId,
            FileName = fileName,
            FilePath = filePath,
            FileType = fileType,
            FileSize = fileSize,
            UploadedAt = DateTime.UtcNow
        };

        var created = await _repository.AddDocumentAsync(document);

        claim.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(claim);

        return new ClaimDocumentDto
        {
            Id = created.Id,
            FileName = created.FileName,
            FileType = created.FileType,
            FileSize = created.FileSize,
            UploadedAt = created.UploadedAt
        };
    }


    public async Task<object> GetClaimsStatsAsync()
    {
        var totalClaims = await _repository.GetTotalCountAsync();
        var draftClaims = await _repository.GetCountByStatusAsync(ClaimStatus.Draft.ToString());
        var submittedClaims = await _repository.GetCountByStatusAsync(ClaimStatus.Submitted.ToString());
        var underReviewClaims = await _repository.GetCountByStatusAsync(ClaimStatus.UnderReview.ToString());
        var approvedClaims = await _repository.GetCountByStatusAsync(ClaimStatus.Approved.ToString());
        var rejectedClaims = await _repository.GetCountByStatusAsync(ClaimStatus.Rejected.ToString());
        var closedClaims = await _repository.GetCountByStatusAsync(ClaimStatus.Closed.ToString());

        return new
        {
            totalClaims,
            draftClaims,
            submittedClaims,
            underReviewClaims,
            approvedClaims,
            rejectedClaims,
            closedClaims
        };
    }
    private static ClaimResponseDto MapToResponse(Claim claim)
    {
        return new ClaimResponseDto
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            PolicyId = claim.PolicyId,
            CustomerId = claim.CustomerId,
            IncidentDate = claim.IncidentDate,
            Description = claim.Description,
            Status = claim.Status.ToString(),
            AdminNote = claim.AdminNote,
            CreatedAt = claim.CreatedAt,
            UpdatedAt = claim.UpdatedAt,
            Documents = claim.ClaimDocuments
                .Select(d => new ClaimDocumentDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    UploadedAt = d.UploadedAt
                })
                .ToList()
        };
    }
}
