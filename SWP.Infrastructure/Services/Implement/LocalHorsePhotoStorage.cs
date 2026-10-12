using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using SWP.Infrastructure.Options;
using SWP.Infrastructure.Services.Interface;

namespace SWP.Infrastructure.Services.Implement;

/// <summary>Lưu ảnh trên disk (wwwroot/media/horses) khi chưa cấu hình Cloudinary.</summary>
public partial class LocalHorsePhotoStorage : IHorsePhotoStorage
{
    private readonly IWebHostEnvironment _environment;
    private readonly CloudinarySettings _settings;

    public LocalHorsePhotoStorage(IWebHostEnvironment environment, IOptions<CloudinarySettings> settings)
    {
        _environment = environment;
        _settings = settings.Value;
    }

    public async Task<string?> SyncHorsePhotoAsync(
        int horseId,
        string? photoInput,
        string? currentStoredUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(photoInput))
        {
            await DeleteIfExistsAsync(horseId, currentStoredUrl, cancellationToken);
            return null;
        }

        var trimmed = photoInput.Trim();
        if (IsSameStoredUrl(trimmed, currentStoredUrl))
        {
            return currentStoredUrl;
        }

        if (IsStoredRelativeUrl(trimmed))
        {
            return trimmed;
        }

        if (TryParseDataUrl(trimmed, out var bytes, out var mimeType))
        {
            await using var stream = new MemoryStream(bytes);
            return await SaveStreamAsync(horseId, stream, mimeType, cancellationToken);
        }

        throw new InvalidOperationException("Định dạng ảnh không hợp lệ. Hãy chọn file JPG, PNG hoặc WebP.");
    }

    public async Task<string?> UploadAsync(
        int horseId,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        return await SaveStreamAsync(horseId, stream, contentType, cancellationToken);
    }

    private async Task<string?> SaveStreamAsync(
        int horseId,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var directory = GetHorseMediaDirectory();
        Directory.CreateDirectory(directory);

        var extension = ExtensionFromMime(contentType);
        var fileName = $"horse_{horseId}.{extension}";
        var fullPath = Path.Combine(directory, fileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        _ = cancellationToken;
        return BuildPublicUrl(fileName);
    }

    private Task DeleteIfExistsAsync(int horseId, string? currentStoredUrl, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var directory = GetHorseMediaDirectory();
        foreach (var ext in new[] { "jpg", "jpeg", "png", "webp", "gif" })
        {
            var path = Path.Combine(directory, $"horse_{horseId}.{ext}");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        if (!string.IsNullOrWhiteSpace(currentStoredUrl) && IsStoredRelativeUrl(currentStoredUrl))
        {
            var fileName = Path.GetFileName(currentStoredUrl);
            var legacy = Path.Combine(directory, fileName);
            if (File.Exists(legacy))
            {
                File.Delete(legacy);
            }
        }

        return Task.CompletedTask;
    }

    private string GetHorseMediaDirectory()
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "media", _settings.Folder);
    }

    private string BuildPublicUrl(string fileName) => $"/media/{_settings.Folder}/{fileName}";

    private static bool IsStoredRelativeUrl(string value) =>
        value.StartsWith("/media/", StringComparison.OrdinalIgnoreCase);

    private static bool IsSameStoredUrl(string input, string? stored) =>
        !string.IsNullOrWhiteSpace(stored)
        && string.Equals(input, stored, StringComparison.OrdinalIgnoreCase);

    private static bool TryParseDataUrl(string input, out byte[] bytes, out string mimeType)
    {
        bytes = Array.Empty<byte>();
        mimeType = "image/jpeg";

        var match = DataUrlRegex().Match(input);
        if (!match.Success)
        {
            return false;
        }

        mimeType = match.Groups["mime"].Value;
        bytes = Convert.FromBase64String(match.Groups["data"].Value);
        return bytes.Length > 0;
    }

    private static string ExtensionFromMime(string mimeType) =>
        mimeType.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => "jpg",
        };

    [GeneratedRegex(@"^data:(?<mime>[\w/+.-]+);base64,(?<data>.+)$", RegexOptions.Singleline)]
    private static partial Regex DataUrlRegex();
}
