using SWP.Application.Common;
using SWP.Application.DTOs.Race;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public class RaceService : IRaceService
{
    private const decimal MaxPrize = 999_999_999_999m;

    private readonly IRaceRepository _races;
    private readonly TimeProvider _clock;

    public RaceService(IRaceRepository races, TimeProvider clock)
    {
        _races = races;
        _clock = clock;
    }

    public async Task<IReadOnlyList<RaceDto>> GetRacesAsync(CancellationToken cancellationToken = default)
    {
        var races = await _races.ListRacesAsync(cancellationToken);
        return races.Select(MapRace).ToList();
    }

    public async Task<IReadOnlyList<RaceRegistrationDto>> GetRegistrationsAsync(
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _races.ListRegistrationsAsync(ownerUserId, cancellationToken);
        return rows.Select(MapRegistration).ToList();
    }

    public async Task<ServiceResult<RaceRegistrationDto>> RegisterAsync(
        int raceId,
        int horseId,
        int trainerId,
        CancellationToken cancellationToken = default)
    {
        var race = await _races.GetRaceTrackedAsync(raceId, cancellationToken);
        if (race is null)
        {
            return ServiceResult<RaceRegistrationDto>.NotFound("Không tìm thấy giải đua");
        }

        if (RaceCatalog.IsClosedRace(race.Status))
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, "Giải đua đã hủy, không thể đăng ký");
        }

        var raceDate = RaceDay(race);
        if (raceDate is null || raceDate.Value < LocalToday())
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, "Giải đua đã diễn ra, không thể đăng ký");
        }

        if (race.RegistrationDeadline is DateTime deadline && DateOnly.FromDateTime(WallClock(deadline)) < LocalToday())
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, "Đã quá hạn đăng ký");
        }

        if (horseId <= 0 || !await _races.HorseExistsAsync(horseId, cancellationToken))
        {
            return ServiceResult<RaceRegistrationDto>.NotFound("Không tìm thấy ngựa");
        }

        if (await _races.HasActiveLockAsync(horseId, cancellationToken))
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, "Ngựa đang khóa tập, không thể đăng ký thi đấu");
        }

        var health = await _races.GetHealthStatusAsync(horseId, cancellationToken);
        if (RaceCatalog.BlocksEntry(health))
        {
            return ServiceResult<RaceRegistrationDto>.Fail(
                ServiceErrorCodes.Conflict,
                "Ngựa đang chấn thương hoặc cách ly, không thể đăng ký thi đấu");
        }

        var existing = await _races.GetRegistrationTrackedAsync(raceId, horseId, cancellationToken);
        if (existing is not null && !RaceCatalog.IsInactiveRegistration(existing.RegistrationStatus))
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, "Ngựa này đã đăng ký giải đua này");
        }

        if (race.MaxParticipants is int max && max > 0)
        {
            var taken = await _races.CountActiveRegistrationsAsync(raceId, cancellationToken);
            if (taken >= max)
            {
                return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, "Giải đã đủ số ngựa đăng ký");
            }
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var registration = existing ?? new RaceRegistration { RaceId = raceId, HorseId = horseId, CreatedAt = now };
        registration.UserId = trainerId;
        registration.RegistrationDate = LocalToday().ToDateTime(TimeOnly.MinValue);
        registration.RegistrationStatus = RaceCatalog.Registered;
        registration.FinishPosition = null;
        registration.PrizeMoney = null;
        registration.ResultStatus = null;
        registration.UpdatedAt = now;
        if (existing is null)
        {
            _races.AddRegistration(registration);
        }

        await _races.SaveAsync(cancellationToken);
        return ServiceResult<RaceRegistrationDto>.Ok(MapRegistration(registration));
    }

    public async Task<ServiceResult<bool>> CancelAsync(int raceId, int horseId, CancellationToken cancellationToken = default)
    {
        var race = await _races.GetRaceTrackedAsync(raceId, cancellationToken);
        if (race is null)
        {
            return ServiceResult<bool>.NotFound("Không tìm thấy giải đua");
        }

        var raceDate = RaceDay(race);
        if (raceDate is null || raceDate.Value < LocalToday())
        {
            return ServiceResult<bool>.Fail(ServiceErrorCodes.Conflict, "Giải đua đã diễn ra, không thể hủy đăng ký");
        }

        var registration = await _races.GetRegistrationTrackedAsync(raceId, horseId, cancellationToken);
        if (registration is null || RaceCatalog.IsInactiveRegistration(registration.RegistrationStatus))
        {
            return ServiceResult<bool>.NotFound("Ngựa này không đăng ký giải đua này");
        }

        _races.RemoveRegistration(registration);
        await _races.SaveAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<RaceResultUpdateDto>> UpdateResultAsync(
        int raceId,
        int horseId,
        int? rank,
        CancellationToken cancellationToken = default)
    {
        var race = await _races.GetRaceTrackedAsync(raceId, cancellationToken);
        if (race is null)
        {
            return ServiceResult<RaceResultUpdateDto>.NotFound("Không tìm thấy giải đua");
        }

        var raceDate = RaceDay(race);
        if (raceDate is null || raceDate.Value >= LocalToday())
        {
            return ServiceResult<RaceResultUpdateDto>.Fail(ServiceErrorCodes.Conflict, "Giải đua chưa diễn ra, chưa thể nhập kết quả");
        }

        var registration = await _races.GetRegistrationTrackedAsync(raceId, horseId, cancellationToken);
        if (registration is null || RaceCatalog.IsInactiveRegistration(registration.RegistrationStatus))
        {
            return ServiceResult<RaceResultUpdateDto>.NotFound("Ngựa này không đăng ký giải đua này");
        }

        var field = await _races.CountActiveRegistrationsAsync(raceId, cancellationToken);
        if (rank is int value && (value < 0 || value > field))
        {
            return ServiceResult<RaceResultUpdateDto>.Invalid("Thứ hạng không hợp lệ");
        }

        if (rank is >= 1 && await _races.RankTakenAsync(raceId, rank.Value, registration.RegistrationId, cancellationToken))
        {
            return ServiceResult<RaceResultUpdateDto>.Fail(
                ServiceErrorCodes.Conflict,
                $"Thứ hạng {rank} đã được gán cho ngựa khác trong giải này");
        }

        var changed = registration.FinishPosition != rank;
        var hadPrize = registration.PrizeMoney is not null;
        var clearsPrize = changed || rank is null || rank < 1;
        var prizeCleared = false;
        if (clearsPrize)
        {
            prizeCleared = hadPrize;
            registration.PrizeMoney = null;
            if (prizeCleared)
            {
                registration.ResultStatus = RaceCatalog.PrizeReview;
            }
        }

        registration.FinishPosition = rank;
        registration.UpdatedAt = _clock.GetUtcNow().UtcDateTime;
        await _races.SaveAsync(cancellationToken);
        return ServiceResult<RaceResultUpdateDto>.Ok(new RaceResultUpdateDto
        {
            Registration = MapRegistration(registration),
            PrizeCleared = prizeCleared,
        });
    }

    public async Task<ServiceResult<RaceRegistrationDto>> SetPrizeAsync(
        int raceId,
        int horseId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount != decimal.Truncate(amount) || amount <= 0 || amount > MaxPrize)
        {
            return ServiceResult<RaceRegistrationDto>.Invalid("Tiền thưởng phải là số nguyên lớn hơn 0");
        }

        var race = await _races.GetRaceTrackedAsync(raceId, cancellationToken);
        if (race is null)
        {
            return ServiceResult<RaceRegistrationDto>.NotFound("Không tìm thấy giải đua");
        }

        var raceDate = RaceDay(race);
        if (raceDate is null || raceDate.Value >= LocalToday())
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, "Giải đua chưa diễn ra, chưa thể nhập tiền thưởng");
        }

        var registration = await _races.GetRegistrationTrackedAsync(raceId, horseId, cancellationToken);
        if (registration is null || RaceCatalog.IsInactiveRegistration(registration.RegistrationStatus))
        {
            return ServiceResult<RaceRegistrationDto>.NotFound("Ngựa này không đăng ký giải đua này");
        }

        if (registration.FinishPosition is not >= 1)
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, "Chỉ nhập tiền thưởng khi ngựa có thứ hạng");
        }

        registration.PrizeMoney = amount;
        registration.ResultStatus = null;
        registration.UpdatedAt = _clock.GetUtcNow().UtcDateTime;
        await _races.SaveAsync(cancellationToken);
        return ServiceResult<RaceRegistrationDto>.Ok(MapRegistration(registration));
    }

    public async Task<IReadOnlyList<PrizeAlertDto>> GetPrizeAlertsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _races.ListPrizeReviewsAsync(cancellationToken);
        return rows.Select(row => new PrizeAlertDto
        {
            AlertID = row.RegistrationId,
            HorseID = row.HorseId,
            RaceID = row.RaceId,
            Ngay = DateText(row.UpdatedAt ?? row.CreatedAt),
        }).ToList();
    }

    public async Task<ServiceResult<bool>> DismissPrizeAlertAsync(int alertId, CancellationToken cancellationToken = default)
    {
        var registration = await _races.GetRegistrationTrackedByIdAsync(alertId, cancellationToken);
        if (registration is null)
        {
            return ServiceResult<bool>.NotFound("Không tìm thấy thông báo tiền thưởng");
        }

        if (registration.ResultStatus == RaceCatalog.PrizeReview)
        {
            registration.ResultStatus = null;
            registration.UpdatedAt = _clock.GetUtcNow().UtcDateTime;
            await _races.SaveAsync(cancellationToken);
        }

        return ServiceResult<bool>.Ok(true);
    }

    private RaceDto MapRace(Race race) => new()
    {
        RaceID = race.RaceId,
        TenGiai = race.RaceName,
        Ngay = race.RaceDate is DateTime date ? DateText(date) : string.Empty,
        DiaDiem = race.Location ?? string.Empty,
    };

    private RaceRegistrationDto MapRegistration(RaceRegistration registration) => new()
    {
        HorseID = registration.HorseId,
        RaceID = registration.RaceId,
        NgayDangKy = DateText(registration.RegistrationDate),
        ThuHang = registration.FinishPosition,
        TienThuong = registration.PrizeMoney,
    };

    private DateOnly? RaceDay(Race race) =>
        race.RaceDate is DateTime date ? DateOnly.FromDateTime(WallClock(date)) : null;

    private DateOnly LocalToday() => DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);

    private string DateText(DateTime value) => DateOnly.FromDateTime(WallClock(value)).ToString("yyyy-MM-dd");

    private DateTime WallClock(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTime(new DateTimeOffset(value, TimeSpan.Zero), _clock.LocalTimeZone).DateTime
            : value;
}
