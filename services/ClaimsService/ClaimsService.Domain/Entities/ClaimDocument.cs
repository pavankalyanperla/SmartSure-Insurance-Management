namespace ClaimsService.Domain.Entities;

public class ClaimDocument
{
    public int Id { get; set; }
    public int ClaimId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Claim Claim { get; set; } = null!;
}
