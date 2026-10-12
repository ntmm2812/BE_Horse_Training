using SWP.Application.Common;
using SWP.Application.DTOs.Horses;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Infrastructure.Repositories.Interface;
using SWP.Infrastructure.Services.Interface;

namespace SWP.Application.Services.Implement;

public class HorseService : IHorseService
{
    private readonly IHorseRepository _horseRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHorsePhotoStorage _horsePhotoStorage;

    public HorseService(
        IHorseRepository horseRepository,
        IUserRepository userRepository,
        IHorsePhotoStorage horsePhotoStorage)
    {
        _horseRepository = horseRepository;
        _userRepository = userRepository;
        _horsePhotoStorage = horsePhotoStorage;
    }

    public async Task<IReadOnlyList<HorseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var horses = await _horseRepository.GetAllAsync(cancellationToken);
        return horses.Select(HorseMapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<HorseDto>> GetByOwnerAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var horses = await _horseRepository.GetByOwnerUserIdAsync(ownerUserId, cancellationToken);
        return horses.Select(HorseMapper.ToDto).ToList();
    }

    public async Task<HorseDto?> GetByIdAsync(int horseId, CancellationToken cancellationToken = default)
    {
        var horse = await _horseRepository.GetByIdAsync(horseId, cancellationToken);
        return horse is null ? null : HorseMapper.ToDto(horse);
    }

    public async Task<HorseDto?> GetByIdForOwnerAsync(
        int horseId,
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var horse = await _horseRepository.GetByIdForOwnerAsync(horseId, ownerUserId, cancellationToken);
        return horse is null ? null : HorseMapper.ToDto(horse);
    }

    public async Task<OwnerHorseDetailDto?> GetDetailForOwnerAsync(
        int horseId,
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var horse = await _horseRepository.GetByIdForOwnerAsync(horseId, ownerUserId, cancellationToken);
        if (horse is null)
        {
            return null;
        }

        var stable = await _horseRepository.GetActiveStableLabelAsync(horseId, cancellationToken);
        var careTasks = await _horseRepository.GetDailyCareTasksAsync(horseId, cancellationToken);
        var feeding = await _horseRepository.GetActiveFeedingDetailsAsync(horseId, cancellationToken);
        var races = await _horseRepository.GetRaceAchievementsAsync(horseId, cancellationToken);

        return OwnerHorseDetailMapper.ToDetailDto(horse, stable, careTasks, feeding, races);
    }

    public async Task<IReadOnlyList<HorseOwnerDto>> GetHorseOwnersAsync(CancellationToken cancellationToken = default)
    {
        var owners = await _userRepository.GetActiveUsersByCanonicalRoleAsync(
            RoleNames.HorseOwner,
            cancellationToken);

        return owners
            .Select(u => new HorseOwnerDto
            {
                UserID = u.UserId,
                HoTen = u.FullName,
                Email = u.Email,
            })
            .ToList();
    }

    public async Task<(bool Success, HorseDto? Data, string? Error)> CreateAsync(
        SaveHorseRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return (false, null, validationError);
        }

        if (!await IsValidHorseOwnerAsync(request.OwnerID, cancellationToken))
        {
            return (false, null, "Chủ sở hữu không hợp lệ hoặc không phải Chủ ngựa đang hoạt động.");
        }

        var entity = HorseMapper.ToEntity(request);
        var created = await _horseRepository.AddAsync(entity, cancellationToken);

        var photoError = await ApplyPhotoAsync(created, request.PhotoUrl, currentStoredUrl: null, cancellationToken);
        if (photoError is not null)
        {
            return (false, null, photoError);
        }

        return (true, HorseMapper.ToDto(created), null);
    }

    public async Task<(bool Success, HorseDto? Data, string? Error)> UpdateAsync(
        int horseId,
        SaveHorseRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return (false, null, validationError);
        }

        var horse = await _horseRepository.GetByIdForUpdateAsync(horseId, cancellationToken);
        if (horse is null)
        {
            return (false, null, "Không tìm thấy ngựa.");
        }

        if (!await IsValidHorseOwnerAsync(request.OwnerID, cancellationToken))
        {
            return (false, null, "Chủ sở hữu không hợp lệ hoặc không phải Chủ ngựa đang hoạt động.");
        }

        var photoError = await ApplyPhotoAsync(horse, request.PhotoUrl, horse.ImageUrl, cancellationToken);
        if (photoError is not null)
        {
            return (false, null, photoError);
        }

        HorseMapper.ApplyUpdate(horse, request);
        await _horseRepository.UpdateAsync(horse, cancellationToken);
        return (true, HorseMapper.ToDto(horse), null);
    }

    public async Task<bool> DeleteAsync(int horseId, CancellationToken cancellationToken = default)
    {
        var horse = await _horseRepository.GetByIdAsync(horseId, cancellationToken);
        if (horse is null)
        {
            return false;
        }

        try
        {
            await _horsePhotoStorage.SyncHorsePhotoAsync(horseId, photoInput: null, horse.ImageUrl, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Vẫn xóa hồ sơ ngựa nếu Cloudinary lỗi; log có thể bổ sung sau.
            _ = ex.Message;
        }

        await _horseRepository.DeleteAsync(horseId, cancellationToken);
        return true;
    }

    private static string? ValidateRequest(SaveHorseRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Ten))
        {
            return "Vui lòng nhập tên ngựa.";
        }

        if (request.OwnerID <= 0)
        {
            return "Vui lòng chọn chủ sở hữu.";
        }

        if (request.Tuoi <= 0)
        {
            return "Tuổi phải là số dương.";
        }

        if (request.CanNang <= 0)
        {
            return "Cân nặng phải là số dương.";
        }

        return null;
    }

    private async Task<string?> ApplyPhotoAsync(
        Domain.Models.Entity.Horse horse,
        string? photoInput,
        string? currentStoredUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(photoInput) && string.IsNullOrWhiteSpace(currentStoredUrl))
        {
            return null;
        }

        try
        {
            var imageUrl = await _horsePhotoStorage.SyncHorsePhotoAsync(
                horse.HorseId,
                photoInput,
                currentStoredUrl,
                cancellationToken);

            if (!string.Equals(imageUrl, horse.ImageUrl, StringComparison.Ordinal))
            {
                horse.ImageUrl = imageUrl;
                await _horseRepository.UpdateAsync(horse, cancellationToken);
            }

            return null;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }

    private async Task<bool> IsValidHorseOwnerAsync(int ownerId, CancellationToken cancellationToken)
    {
        if (!await _horseRepository.OwnerExistsAsync(ownerId, cancellationToken))
        {
            return false;
        }

        var owners = await _userRepository.GetActiveUsersByCanonicalRoleAsync(
            RoleNames.HorseOwner,
            cancellationToken);

        return owners.Any(o => o.UserId == ownerId);
    }
}
