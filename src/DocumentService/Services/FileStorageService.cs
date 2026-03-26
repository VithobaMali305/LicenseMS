namespace DocumentService.Services;

// ── Abstraction (swap LocalFileStorageService for AzureBlobStorageService in prod)
public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file);
}

// ── Local Filesystem Implementation ───────────────────────────────────────
public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileStorageService> _logger;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        { "application/pdf",  new byte[] { 0x25, 0x50, 0x44, 0x46 } },   // %PDF
        { "image/jpeg",       new byte[] { 0xFF, 0xD8, 0xFF } },
        { "image/png",        new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                              new byte[] { 0x50, 0x4B, 0x03, 0x04 } }    // ZIP/DOCX
    };

    public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        //// 1. MIME whitelist check
        //if (!AllowedMimeTypes.Contains(file.ContentType))
        //    throw new InvalidOperationException($"File type '{file.ContentType}' is not allowed.");

        // 2. Magic bytes validation (server-side, not trusting Content-Type header)
        await ValidateMagicBytesAsync(file);

        // 3. Determine extension
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };
        if (!allowedExtensions.Contains(ext)) ext = ".bin";

        // 4. Save with GUID filename
        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);

        var fileName  = $"{Guid.NewGuid()}{ext}";
        var fullPath  = Path.Combine(uploadsPath, fileName);
        var relativePath = $"uploads/{fileName}";

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        _logger.LogInformation("File saved: {RelativePath}", relativePath);
        return relativePath;
    }

    private async Task ValidateMagicBytesAsync(IFormFile file)
    {
        if (!MagicBytes.TryGetValue(file.ContentType, out var magic)) return;

        var buffer = new byte[magic.Length];
        using var stream = file.OpenReadStream();
        await stream.ReadAsync(buffer, 0, buffer.Length);

        if (!buffer.Take(magic.Length).SequenceEqual(magic))
            throw new InvalidOperationException("File content does not match declared type.");
    }
}
