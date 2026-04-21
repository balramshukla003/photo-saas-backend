using PhotoPrint.API.DTOs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PhotoPrint.API.Services;

public interface IPhotoService
{
    Task<PhotoProcessResponse> ProcessAsync(IFormFile file, CancellationToken ct = default);
}

public sealed class PhotoService : IPhotoService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _removeBgApiKey;
    private readonly ILogger<PhotoService> _logger;

    public PhotoService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<PhotoService> logger)
    {
        _httpFactory = httpFactory;
        _removeBgApiKey = config["RemoveBg:ApiKey"]
            ?? throw new InvalidOperationException("RemoveBg:ApiKey is not configured.");
        _logger = logger;
    }

    public async Task<PhotoProcessResponse> ProcessAsync(IFormFile file, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting background removal for: {Name}", file.FileName);
            var bgRemovedBytes = await RemoveBackgroundAsync(file, ct);

            _logger.LogInformation("Enhancing image.");
            var enhancedBytes = EnhanceAndCrop(bgRemovedBytes);

            var base64 = Convert.ToBase64String(enhancedBytes);
            _logger.LogInformation("Photo processing complete.");

            return new PhotoProcessResponse(true, base64, "image/png", "Processing complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo processing failed.");
            return new PhotoProcessResponse(false, null, null, $"Processing failed: {ex.Message}");
        }
    }

    private async Task<byte[]> RemoveBackgroundAsync(IFormFile file, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("RemoveBgService");

        await using var stream = file.OpenReadStream();
        var fileBytes = new byte[file.Length];
        _ = await stream.ReadAsync(fileBytes, ct);

        using var content = new MultipartFormDataContent();

        var imageContent = new ByteArrayContent(fileBytes);
        imageContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

        content.Add(imageContent, "image_file", file.FileName);
        content.Add(new StringContent("auto"), "size");

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.remove.bg/v1.0/removebg");
        request.Headers.Add("X-Api-Key", _removeBgApiKey);
        request.Content = content;

        _logger.LogInformation(
            "Calling remove.bg | File: {Name} | Size: {Size} bytes",
            file.FileName, fileBytes.Length);

        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("remove.bg error {Status}: {Body}",
                (int)response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"remove.bg API error {(int)response.StatusCode}: {errorBody}");
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private static byte[] EnhanceAndCrop(byte[] imageBytes)
    {
        using var image = Image.Load<Rgba32>(imageBytes);

        // ── Composite white background over transparency ───────────────────
        using var whiteBackground = new Image<Rgba32>(
            image.Width, image.Height, Color.White);

        whiteBackground.Mutate(ctx => ctx.DrawImage(image, 1f));

        // ── Enhance + crop to passport size ───────────────────────────────
        whiteBackground.Mutate(ctx => ctx
            .AutoOrient()
            .Brightness(1.08f)
            .Contrast(1.12f)
            .Saturate(1.05f)
            .GaussianSharpen(0.75f)
            .Resize(new ResizeOptions
            {
                Size = new Size(413, 531),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Top,
                Sampler = KnownResamplers.Lanczos3,
            })
        );

        using var ms = new System.IO.MemoryStream();
        whiteBackground.Save(ms, new PngEncoder
        {
            CompressionLevel = PngCompressionLevel.BestSpeed,
            ColorType = PngColorType.Rgb,
        });

        return ms.ToArray();
    }
}