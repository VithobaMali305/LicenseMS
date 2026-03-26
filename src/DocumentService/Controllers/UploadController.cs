using DocumentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentService.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<UploadController> _logger;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadController(IFileStorageService storage, ILogger<UploadController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/upload/document
    /// Accepts a single file (multipart/form-data), validates and stores it.
    /// Returns the relative file path to be stored in the Licenses table.
    /// </summary>
    [HttpPost("document")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        if (file.Length > MaxFileSizeBytes)
            return StatusCode(413, new { message = "File exceeds the 10 MB size limit." });

        try
        {
            var filePath = await _storage.SaveFileAsync(file);
            _logger.LogInformation("Document uploaded successfully: {Path}", filePath);
            return Ok(new { filePath });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("File upload rejected: {Reason}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file upload.");
            return StatusCode(500, new { message = "An error occurred while saving the file." });
        }
    }
}
