namespace ClaimsService.Application.DTOs;

public class CreateClaimDto
{
    public int PolicyId { get; set; }
    public DateTime IncidentDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ClaimResponseDto
{
    public int Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public int PolicyId { get; set; }
    public int CustomerId { get; set; }
    public DateTime IncidentDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ClaimDocumentDto> Documents { get; set; } = new();
}

public class ClaimDocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class UpdateClaimStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
}

public class SubmitClaimDto
{
    public int ClaimId { get; set; }
}
