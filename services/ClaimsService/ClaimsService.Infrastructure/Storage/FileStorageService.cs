using Microsoft.AspNetCore.Http;

namespace ClaimsService.Infrastructure.Storage;

public class FileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;

    public async Task<string> SaveFileAsync(IFormFile file, int claimId)
    {
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("File is required.");

        if (file.Length > MaxFileSize)
            throw new InvalidOperationException("File size exceeds 5MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only PDF, JPG, JPEG and PNG files are allowed.");

        var uploadsRoot = Path.Combine("wwwroot", "uploads", "claims", claimId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var safeName = Path.GetFileName(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}_{safeName}";
        var fullPath = Path.Combine(uploadsRoot, uniqueFileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return fullPath.Replace("\\", "/");
    }
}
