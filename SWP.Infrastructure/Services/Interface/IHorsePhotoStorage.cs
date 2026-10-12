namespace SWP.Infrastructure.Services.Interface;

public interface IHorsePhotoStorage
{
    /// <param name="photoInput">URL đã lưu, data URL base64, hoặc null/empty để xóa ảnh.</param>
    Task<string?> SyncHorsePhotoAsync(
        int horseId,
        string? photoInput,
        string? currentStoredUrl,
        CancellationToken cancellationToken = default);

    Task<string?> UploadAsync(
        int horseId,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);
}
