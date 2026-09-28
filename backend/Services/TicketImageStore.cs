using Microsoft.Extensions.Options;

namespace TicketApi.Services;

/// <summary>
/// Writes a customer-supplied ticket image to disk and returns the URL it is served
/// from, rejecting anything oversized or not recognisably an image.
/// </summary>
public sealed class TicketImageStore
{
    // ADR-0003 section 1, what counts as an acceptable image
    public const long MaxBytes = 5 * 1024 * 1024;

    public const string UrlPrefix = "uploads";

    private static readonly (byte[] Signature, string Extension)[] ImageSignatures =
    [
        ([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], ".png"),
        ([0xFF, 0xD8, 0xFF], ".jpg"),
        ([0x47, 0x49, 0x46, 0x38], ".gif"),
    ];

    private const int WebpRiffLength = 12;

    private readonly string uploadDirectory;

    public TicketImageStore(IOptions<TicketStoreOptions> options)
    {
        uploadDirectory = Path.GetFullPath(options.Value.UploadDirectory);
    }

    public async Task<string> SaveAsync(IFormFile image, CancellationToken ct = default)
    {
        if (image.Length > MaxBytes)
            throw ValidationException.Single(
                "Image",
                $"The image must be {MaxBytes / (1024 * 1024)} MB or smaller."
            );

        await using var uploaded = image.OpenReadStream();
        var header = new byte[WebpRiffLength];
        var headerLength = await uploaded.ReadAtLeastAsync(
            header,
            header.Length,
            throwOnEndOfStream: false,
            ct
        );

        var extension = ExtensionOf(header.AsSpan(0, headerLength))
            ?? throw ValidationException.Single(
                "Image",
                "The file must be a PNG, JPEG, GIF or WebP image."
            );

        var fileName = $"{Guid.NewGuid()}{extension}";
        await using var target = File.Create(Path.Combine(uploadDirectory, fileName));
        await target.WriteAsync(header.AsMemory(0, headerLength), ct);
        await uploaded.CopyToAsync(target, ct);

        return $"{UrlPrefix}/{fileName}";
    }

    private static string? ExtensionOf(ReadOnlySpan<byte> header)
    {
        foreach (var (signature, extension) in ImageSignatures)
            if (header.StartsWith(signature))
                return extension;

        if (
            header.Length == WebpRiffLength
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8)
        )
            return ".webp";

        return null;
    }
}
