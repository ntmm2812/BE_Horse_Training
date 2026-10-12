using SWP.Application.DTOs.Horses;

namespace SWP.Application.Services.Interface;

public interface IHorseService
{
    Task<IReadOnlyList<HorseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HorseDto>> GetByOwnerAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<HorseDto?> GetByIdAsync(int horseId, CancellationToken cancellationToken = default);

    Task<HorseDto?> GetByIdForOwnerAsync(int horseId, int ownerUserId, CancellationToken cancellationToken = default);

    Task<OwnerHorseDetailDto?> GetDetailForOwnerAsync(
        int horseId,
        int ownerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HorseOwnerDto>> GetHorseOwnersAsync(CancellationToken cancellationToken = default);

    Task<(bool Success, HorseDto? Data, string? Error)> CreateAsync(
        SaveHorseRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, HorseDto? Data, string? Error)> UpdateAsync(
        int horseId,
        SaveHorseRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int horseId, CancellationToken cancellationToken = default);

    Task<(bool Success, HorseDto? Data, string? Error)> UploadPhotoAsync(
        int horseId,
        Stream fileStream,
        string contentType,
        long fileLength,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HorseHealthStatusDto>> GetHealthStatusesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HorseHealthStatusDto>> GetHealthStatusesForOwnerAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default);
}
