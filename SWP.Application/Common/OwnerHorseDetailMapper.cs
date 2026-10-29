using System.Globalization;
using SWP.Application.DTOs.Horses;
using SWP.Domain.Models.Entity;

namespace SWP.Application.Common;

public static class OwnerHorseDetailMapper
{
    public static OwnerHorseDetailDto ToDetailDto(
        Horse horse,
        string? stableLabel,
        IReadOnlyList<CareTask> careTasks,
        IReadOnlyList<FeedingPlanDetail> feedingDetails,
        IReadOnlyList<(RaceRegistration Registration, Race Race)> races)
    {
        return new OwnerHorseDetailDto
        {
            HorseID = horse.HorseId,
            Ten = horse.HorseName,
            Giong = horse.Breed ?? string.Empty,
            DongDoi = HorseMapper.ToDto(horse).DongDoi,
            Tuoi = HorseMapper.ToDto(horse).Tuoi,
            CanNang = horse.Weight ?? 0,
            PhotoUrl = horse.ImageUrl,
            GioiTinh = FormatGender(horse.Gender),
            MauLong = horse.Color,
            NgaySinh = FormatDateOfBirth(horse.DateOfBirth, HorseMapper.ToDto(horse).Tuoi),
            SoChuong = stableLabel,
            LichSinhHoat = MapSchedule(careTasks),
            KhauPhan = MapDiet(feedingDetails),
            ThanhTichThiDau = MapRaces(races),
        };
    }

    private static IReadOnlyList<OwnerDailyScheduleItemDto> MapSchedule(IReadOnlyList<CareTask> tasks)
    {
        return tasks
            .Where(t => t.ScheduledAt.HasValue)
            .Select(t => new OwnerDailyScheduleItemDto
            {
                Gio = t.ScheduledAt!.Value.ToLocalTime().ToString("HH:mm"),
                CongViec = string.IsNullOrWhiteSpace(t.TaskName) ? (t.Description ?? "—") : t.TaskName,
            })
            .ToList();
    }

    private static IReadOnlyList<string> MapDiet(IReadOnlyList<FeedingPlanDetail> details)
    {
        if (details.Count == 0)
        {
            return Array.Empty<string>();
        }

        var groups = details
            .GroupBy(d => d.MealName ?? "Bữa ăn")
            .OrderBy(g => g.Min(x => x.FeedingTime?.ToTimeSpan() ?? TimeSpan.MaxValue));

        return groups
            .Select(group =>
            {
                var parts = group
                    .Select(d =>
                    {
                        var qty = d.Quantity.ToString("0.##", CultureInfo.InvariantCulture);
                        var unit = string.IsNullOrWhiteSpace(d.Unit) ? "" : $" {d.Unit}";
                        var feed = d.FeedName ?? "thức ăn";
                        return $"{qty}{unit} {feed}".Trim();
                    });

                var meal = group.Key;
                return $"{string.Join(" · ", parts)} · {meal}";
            })
            .ToList();
    }

    private static IReadOnlyList<OwnerRaceAchievementDto> MapRaces(
        IReadOnlyList<(RaceRegistration Registration, Race Race)> races)
    {
        var now = DateTime.UtcNow;
        return races
            .Select(pair =>
            {
                var raceDate = pair.Race.RaceDate;
                var ngay = raceDate.HasValue
                    ? raceDate.Value.ToLocalTime().ToString("dd/MM")
                    : "—";

                var trangThai = ResolveRaceStatus(pair.Registration, pair.Race, now);

                return new OwnerRaceAchievementDto
                {
                    Ngay = ngay,
                    TenCuocDua = pair.Race.RaceName,
                    TrangThai = trangThai,
                };
            })
            .ToList();
    }

    private static string ResolveRaceStatus(RaceRegistration reg, Race race, DateTime nowUtc)
    {
        if (reg.FinishPosition is > 0)
        {
            return $"Hạng {reg.FinishPosition}";
        }

        if (reg.FinishPosition == 0)
        {
            return "Không xếp hạng";
        }

        if (race.RaceDate.HasValue && race.RaceDate.Value > nowUtc)
        {
            return "Sắp diễn ra";
        }

        return "Chưa có kết quả";
    }

    private static string? FormatGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return null;
        }

        return gender.Trim().ToLowerInvariant() switch
        {
            "male" or "stallion" or "m" or "đực" => "Ngựa đực",
            "female" or "mare" or "f" or "cái" => "Ngựa cái",
            _ => gender,
        };
    }

    private static string? FormatDateOfBirth(DateOnly? dateOfBirth, int tuoi)
    {
        if (dateOfBirth is null)
        {
            return tuoi > 0 ? $"({tuoi} tuổi)" : null;
        }

        var formatted = dateOfBirth.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return tuoi > 0 ? $"{formatted} ({tuoi} tuổi)" : formatted;
    }
}
