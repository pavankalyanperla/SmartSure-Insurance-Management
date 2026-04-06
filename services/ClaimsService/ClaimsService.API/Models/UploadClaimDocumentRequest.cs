namespace ClaimsService.API.Models;

public class UploadClaimDocumentRequest
{
    public IFormFile File { get; set; } = null!;
}
