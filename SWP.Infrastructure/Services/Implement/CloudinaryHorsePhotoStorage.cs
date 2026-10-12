using System.Text.RegularExpressions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using SWP.Infrastructure.Options;
using SWP.Infrastructure.Services.Interface;

namespace SWP.Infrastructure.Services.Implement;

public partial class CloudinaryHorsePhotoStorage : IHorsePhotoStorage
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;

    public CloudinaryHorsePhotoStorage(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
        var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
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
        if (IsRemoteUrl(trimmed) && string.Equals(trimmed, currentStoredUrl, StringComparison.OrdinalIgnoreCase))
        {
            return currentStoredUrl;
        }

        if (IsRemoteUrl(trimmed) && !trimmed.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ chấp nhận URL ảnh từ Cloudinary hoặc upload file mới.");
        }

        if (IsRemoteUrl(trimmed))
        {
            return trimmed;
        }

        if (!TryParseDataUrl(trimmed, out var bytes, out var mimeType))
        {
            throw new InvalidOperationException("Định dạng ảnh không hợp lệ.");
        }

        await using var stream = new MemoryStream(bytes);
        var publicId = BuildPublicId(horseId);
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription($"horse-{horseId}.{ExtensionFromMime(mimeType)}", stream),
            Folder = _settings.Folder,
            PublicId = publicId,
            Overwrite = true,
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
        if (uploadResult.Error is not null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl?.ToString();
    }

    private async Task DeleteIfExistsAsync(int horseId, string? currentStoredUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentStoredUrl))
        {
            return;
        }

        var deleteParams = new DeletionParams(FullPublicId(horseId))
        {
            ResourceType = ResourceType.Image,
        };

        _ = cancellationToken;
        await _cloudinary.DestroyAsync(deleteParams);
    }

    private string BuildPublicId(int horseId) => $"horse_{horseId}";

    private string FullPublicId(int horseId) => $"{_settings.Folder}/{BuildPublicId(horseId)}";

    private static bool IsRemoteUrl(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

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
