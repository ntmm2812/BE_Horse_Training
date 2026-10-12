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
    private readonly IHorseHealthStatusRepository _horseHealthStatusRepository;

    public HorseService(
        IHorseRepository horseRepository,
        IUserRepository userRepository,
        IHorsePhotoStorage horsePhotoStorage,
        IHorseHealthStatusRepository horseHealthStatusRepository)
    {
        _horseRepository = horseRepository;
        _userRepository = userRepository;
        _horsePhotoStorage = horsePhotoStorage;
        _horseHealthStatusRepository = horseHealthStatusRepository;
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

        var healthError = await ApplyHealthStatusAsync(
            created.HorseId,
            request.TrangThaiSucKhoe,
            applyDefaultWhenMissing: true,
            cancellationToken);
        if (healthError is not null)
        {
            return (false, null, healthError);
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

        var healthError = await ApplyHealthStatusAsync(
            horse.HorseId,
            request.TrangThaiSucKhoe,
            applyDefaultWhenMissing: false,
            cancellationToken);
        if (healthError is not null)
        {
            return (false, null, healthError);
        }

        return (true, HorseMapper.ToDto(horse), null);
    }

    public async Task<IReadOnlyList<HorseHealthStatusDto>> GetHealthStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _horseHealthStatusRepository.GetAllAsync(cancellationToken);
        return rows.Select(HorseHealthStatusMapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<HorseHealthStatusDto>> GetHealthStatusesForOwnerAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _horseHealthStatusRepository.GetByOwnerUserIdAsync(ownerUserId, cancellationToken);
        return rows.Select(HorseHealthStatusMapper.ToDto).ToList();
    }

    private const long MaxPhotoBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedPhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public async Task<(bool Success, HorseDto? Data, string? Error)> UploadPhotoAsync(
        int horseId,
        Stream fileStream,
        string contentType,
        long fileLength,
        CancellationToken cancellationToken = default)
    {
        if (fileLength <= 0 || fileLength > MaxPhotoBytes)
        {
            return (false, null, "Ảnh phải lớn hơn 0 và không quá 5 MB.");
        }

        if (!AllowedPhotoContentTypes.Contains(contentType))
        {
            return (false, null, "Chỉ nhận ảnh JPG, PNG hoặc WebP.");
        }

        var horse = await _horseRepository.GetByIdForUpdateAsync(horseId, cancellationToken);
        if (horse is null)
        {
            return (false, null, "Không tìm thấy ngựa.");
        }

        try
        {
            var imageUrl = await _horsePhotoStorage.UploadAsync(horseId, fileStream, contentType, cancellationToken);
            horse.ImageUrl = imageUrl;
            await _horseRepository.UpdateAsync(horse, cancellationToken);
            return (true, HorseMapper.ToDto(horse), null);
        }
        catch (InvalidOperationException ex)
        {
            return (false, null, ex.Message);
        }
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

        if (string.IsNullOrWhiteSpace(request.NgaySinh) && request.Tuoi <= 0)
        {
            return "Vui lòng nhập ngày sinh.";
        }

        if (!string.IsNullOrWhiteSpace(request.NgaySinh))
        {
            if (!DateOnly.TryParse(request.NgaySinh.Trim(), out var dateOfBirth))
            {
                return "Ngày sinh không hợp lệ.";
            }

            if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return "Ngày sinh không thể ở tương lai.";
            }
        }
        else if (request.Tuoi <= 0)
        {
            return "Tuổi phải là số dương.";
        }

        if (request.CanNang <= 0)
        {
            return "Cân nặng phải là số dương.";
        }

        if (request.ChieuCao is < 0)
        {
            return "Chiều cao không hợp lệ.";
        }

        if (!string.IsNullOrWhiteSpace(request.TrangThaiSucKhoe)
            && !HorseHealthStatuses.IsValid(request.TrangThaiSucKhoe))
        {
            return "Trạng thái sức khỏe không hợp lệ.";
        }

        return null;
    }

    private async Task<string?> ApplyHealthStatusAsync(
        int horseId,
        string? trangThaiSucKhoe,
        bool applyDefaultWhenMissing,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trangThaiSucKhoe))
        {
            if (!applyDefaultWhenMissing)
            {
                return null;
            }

            trangThaiSucKhoe = HorseHealthStatuses.DuDieuKien;
        }

        var status = trangThaiSucKhoe.Trim();

        try
        {
            await _horseHealthStatusRepository.UpsertAsync(horseId, status, cancellationToken);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
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
