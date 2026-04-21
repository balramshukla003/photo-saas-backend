using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/photo")]
[Authorize]
[Produces("application/json")]
public sealed class PhotoController : ControllerBase
{
    private readonly IPhotoService            _photo;
    private readonly ILogger<PhotoController> _logger;

    private static readonly string[] AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    public PhotoController(IPhotoService photo, ILogger<PhotoController> logger)
    {
        _photo  = photo;
        _logger = logger;
    }

    /// <summary>
    /// Upload a photo. Returns BG-removed, enhanced, passport-cropped image as base64.
    /// Requires valid JWT + active license (enforced by LicenseMiddleware).
    /// </summary>
    [HttpPost("process")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
    [ProducesResponseType(typeof(PhotoProcessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PhotoProcessResponse), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Process(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new PhotoProcessResponse(false, null, null, "No file uploaded."));

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new PhotoProcessResponse(false, null, null,
                $"File too large. Maximum allowed size is {MaxFileSizeBytes / 1024 / 1024} MB."));

        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new PhotoProcessResponse(false, null, null,
                "Unsupported file type. Only JPEG, PNG, and WEBP are accepted."));

        _logger.LogInformation(
            "Processing photo: {Name}, {Size} bytes, {Type}",
            file.FileName, file.Length, file.ContentType);

        var result = await _photo.ProcessAsync(file, ct);

        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status500InternalServerError, result);
    }
}
