using System.Text.Json;
using SWP.Application.Common;
using SWP.Application.DTOs.Approvals;
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

    public async Task<ServiceResult<RaceDto>> CreateRaceAsync(
        int managerId,
        CreateRaceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateNewRace(request, out var name, out var location, out var description);
        if (error is not null)
        {
            return ServiceResult<RaceDto>.Invalid(error);
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var race = new Race
        {
            RaceName = name,
            // RaceCode có unique index (SQL Server chỉ cho một NULL) nên luôn sinh mã riêng cho giải mới.
            RaceCode = $"RC-{Guid.NewGuid():N}"[..13].ToUpperInvariant(),
            Location = location,
            RaceDate = request.Ngay!.Value.ToDateTime(TimeOnly.MinValue),
            RegistrationDeadline = request.RegistrationDeadline!.Value.ToDateTime(TimeOnly.MinValue),
            DistanceMeters = request.DistanceMeters,
            MaxParticipants = request.MaxParticipants,
            EntryFee = request.EntryFee ?? 0,
            PrizeFirst = request.PrizeFirst,
            PrizeSecond = request.PrizeSecond,
            PrizeThird = request.PrizeThird,
            // Có nhập hạng thì tổng luôn tự tính (không tin số FE gửi); không thì dùng tổng nhập tay.
            PrizePool = HasRankPrize(request) ? RankPrizeTotal(request) : request.PrizePool,
            Description = description,
            Status = RaceCatalog.Open,
            UserId = managerId,
            CreatedAt = now,
        };

        _races.AddRace(race);
        await _races.SaveAsync(cancellationToken);
        return ServiceResult<RaceDto>.Ok(MapRace(race));
    }

    private string? ValidateNewRace(
        CreateRaceRequestDto request,
        out string name,
        out string? location,
        out string? description)
    {
        name = (request.TenGiai ?? string.Empty).Trim();
        location = string.IsNullOrWhiteSpace(request.DiaDiem) ? null : request.DiaDiem.Trim();
        description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var today = LocalToday();

        if (name.Length == 0) return "Vui lòng nhập tên giải.";
        if (name.Length > 150) return "Tên giải tối đa 150 ký tự.";
        if (location is { Length: > 255 }) return "Địa điểm tối đa 255 ký tự.";
        if (request.Ngay is not { } raceDay) return "Vui lòng chọn ngày đua.";
        if (raceDay <= today) return "Ngày đua phải sau hôm nay.";
        if (request.RegistrationDeadline is not { } deadline) return "Vui lòng chọn hạn đăng ký.";
        if (deadline < today) return "Hạn đăng ký không được ở quá khứ.";
        if (deadline > raceDay) return "Hạn đăng ký không được sau ngày đua.";
        if (request.DistanceMeters is not > 0) return "Cự ly phải lớn hơn 0 mét.";
        if (request.MaxParticipants is not (>= 1 and <= 50)) return "Số ngựa tối đa từ 1 đến 50.";
        if (request.EntryFee is < 0 or > MaxPrize) return "Lệ phí phải lớn hơn hoặc bằng 0.";
        var prizeError = ValidateRankPrizes(request);
        if (prizeError is not null) return prizeError;
        if (!HasRankPrize(request) && request.PrizePool is < 0 or > MaxPrize) return "Tổng giải thưởng không hợp lệ.";
        if (description is { Length: > 1000 }) return "Mô tả tối đa 1000 ký tự.";
        return null;
    }

    private static bool HasRankPrize(CreateRaceRequestDto request) =>
        request.PrizeFirst is not null || request.PrizeSecond is not null || request.PrizeThird is not null;

    private static decimal RankPrizeTotal(CreateRaceRequestDto request) =>
        (request.PrizeFirst ?? 0) + (request.PrizeSecond ?? 0) + (request.PrizeThird ?? 0);

    // Mỗi hạng >= 0; các hạng đã nhập phải giảm dần: Nhất >= Nhì >= Ba.
    private static string? ValidateRankPrizes(CreateRaceRequestDto request)
    {
        (string Name, decimal? Amount)[] ranks =
        [
            ("Nhất", request.PrizeFirst),
            ("Nhì", request.PrizeSecond),
            ("Ba", request.PrizeThird),
        ];

        foreach (var (name, amount) in ranks)
        {
            if (amount is < 0 or > MaxPrize) return $"Giải {name} phải lớn hơn hoặc bằng 0.";
        }

        var entered = ranks.Where(r => r.Amount is not null).ToArray();
        for (var i = 1; i < entered.Length; i++)
        {
            if (entered[i].Amount > entered[i - 1].Amount)
            {
                return $"Giải {entered[i].Name} không được lớn hơn giải {entered[i - 1].Name}.";
            }
        }

        return null;
    }

    public async Task<ServiceResult<RaceDto>> CancelRaceAsync(
        int raceId,
        int managerId,
        CancelRaceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length == 0)
        {
            return ServiceResult<RaceDto>.Invalid("Vui lòng nhập lý do hủy giải.");
        }

        if (reason.Length > 500)
        {
            return ServiceResult<RaceDto>.Invalid("Lý do hủy tối đa 500 ký tự.");
        }

        var race = await _races.GetRaceTrackedAsync(raceId, cancellationToken);
        if (race is null)
        {
            return ServiceResult<RaceDto>.NotFound("Không tìm thấy giải đua");
        }

        if (RaceCatalog.IsClosedRace(race.Status))
        {
            return ServiceResult<RaceDto>.Fail(ServiceErrorCodes.Conflict, "Giải đã được hủy trước đó.");
        }

        var raceDate = RaceDay(race);
        if (raceDate is null || raceDate.Value < LocalToday())
        {
            return ServiceResult<RaceDto>.Fail(ServiceErrorCodes.Conflict, "Giải đã diễn ra, không thể hủy.");
        }

        if (await _races.HasResultsAsync(raceId, cancellationToken))
        {
            return ServiceResult<RaceDto>.Fail(ServiceErrorCodes.Conflict, "Giải đã có kết quả, không thể hủy.");
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        race.Status = RaceCatalog.Cancelled;
        race.UpdatedAt = now;

        // Đăng ký còn chờ duyệt bị đóng bằng trạng thái Rejected có sẵn (HLV thấy "Giải đã hủy" ở lý do từ chối).
        var managerName = await _races.GetUserNameAsync(managerId, cancellationToken) ?? "Quản lý CLB";
        foreach (var proposal in await _races.ListPendingProposalsTrackedAsync(raceId, cancellationToken))
        {
            var history = JsonSerializer.Deserialize<List<ApprovalHistory>>(proposal.HistoryJson) ?? [];
            history.Add(new ApprovalHistory("Manager", "Từ chối", managerName, $"Giải đã hủy: {reason}", now));
            proposal.HistoryJson = JsonSerializer.Serialize(history);
            proposal.Status = "Rejected";
            proposal.RejectedBy = "Manager";
            proposal.RejectReason = "Giải đã hủy";
            proposal.TrainerSeen = false;
            proposal.Version = Guid.NewGuid().ToString();
        }

        await _races.SaveAsync(cancellationToken);
        return ServiceResult<RaceDto>.Ok(MapRace(race));
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

        // Hạng 0 = không xếp hạng. Giải có ngựa của nhiều CLB nên cận trên là MaxParticipants của giải
        // (không phải số ngựa của CLB đăng ký); MaxParticipants trống hoặc 0 = không giới hạn.
        if (rank is int value)
        {
            if (value < 0)
            {
                return ServiceResult<RaceResultUpdateDto>.Invalid("Thứ hạng phải là 0 (không xếp hạng) hoặc số nguyên từ 1 trở lên");
            }

            if (race.MaxParticipants is int max && max > 0 && value > max)
            {
                return ServiceResult<RaceResultUpdateDto>.Invalid($"Thứ hạng không được vượt quá {max} (số ngựa tối đa của giải)");
            }
        }

        if (rank is >= 1)
        {
            var holder = await _races.GetRankHolderNameAsync(raceId, rank.Value, registration.RegistrationId, cancellationToken);
            if (holder is not null)
            {
                return ServiceResult<RaceResultUpdateDto>.Fail(
                    ServiceErrorCodes.Conflict,
                    $"Thứ hạng {rank} đã được gán cho ngựa {holder} trong giải này");
            }
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
        RegistrationDeadline = race.RegistrationDeadline is DateTime deadline ? DateText(deadline) : null,
        EntryFee = race.EntryFee,
        MaxParticipants = race.MaxParticipants,
        Status = race.Status,
        DistanceMeters = race.DistanceMeters,
        PrizePool = race.PrizePool,
        PrizeFirst = race.PrizeFirst,
        PrizeSecond = race.PrizeSecond,
        PrizeThird = race.PrizeThird,
        Description = race.Description,
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
