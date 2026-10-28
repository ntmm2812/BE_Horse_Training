using SWP.Application.DTOs.Races;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;

namespace SWP.Application.Common;

public static class RaceMapper
{
    public static RaceDto ToDto(Race race)
    {
        return new RaceDto
        {
            RaceId = race.RaceId,
            RaceName = race.RaceName,
            RaceCode = race.RaceCode,
            RaceType = race.RaceType,
            Location = race.Location,
            RaceDate = race.RaceDate,
            RegistrationDeadline = race.RegistrationDeadline,
            DistanceMeters = race.DistanceMeters,
            MaxParticipants = race.MaxParticipants,
            CurrentParticipants = race.RaceRegistrations?.Count(r => r.RegistrationStatus != RaceRegistrationStatuses.Cancelled) ?? 0,
            EntryFee = race.EntryFee,
            PrizePool = race.PrizePool,
            Status = race.Status,
            Description = race.Description,
            UserId = race.UserId,
            CreatedByName = race.User?.FullName,
            CreatedAt = race.CreatedAt,
            UpdatedAt = race.UpdatedAt,
            Registrations = race.RaceRegistrations?
                .Select(ToDto)
                .OrderBy(r => r.HorseNumber)
                .ThenBy(r => r.FinishPosition)
                .ToList() ?? new List<RaceRegistrationDto>()
        };
    }

    public static RaceRegistrationDto ToDto(RaceRegistration reg)
    {
        return new RaceRegistrationDto
        {
            RegistrationId = reg.RegistrationId,
            RaceId = reg.RaceId,
            RaceName = reg.Race?.RaceName,
            RaceCode = reg.Race?.RaceCode,
            RaceDate = reg.Race?.RaceDate,
            Location = reg.Race?.Location,
            DistanceMeters = reg.Race?.DistanceMeters,
            HorseId = reg.HorseId,
            HorseName = reg.Horse?.HorseName ?? string.Empty,
            HorseCode = null,
            OwnerUserId = reg.Horse?.UserId,
            OwnerName = reg.Horse?.User?.FullName,
            RegisteredByUserId = reg.UserId,
            RegisteredByName = reg.User?.FullName,
            RegistrationDate = reg.RegistrationDate,
            RegistrationStatus = reg.RegistrationStatus,
            HorseNumber = reg.HorseNumber,
            FinishPosition = reg.FinishPosition,
            FinishTimeSeconds = reg.FinishTimeSeconds,
            PrizeMoney = reg.PrizeMoney,
            ResultStatus = reg.ResultStatus,
            Notes = reg.Notes,
            CreatedAt = reg.CreatedAt,
            UpdatedAt = reg.UpdatedAt
        };
    }

    public static Race ToEntity(CreateRaceDto dto, int userId)
    {
        var now = DateTime.UtcNow;
        var code = !string.IsNullOrWhiteSpace(dto.RaceCode)
            ? dto.RaceCode.Trim()
            : $"RACE-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";

        return new Race
        {
            RaceName = dto.RaceName.Trim(),
            RaceCode = code,
            RaceType = dto.RaceType?.Trim(),
            Location = dto.Location?.Trim(),
            RaceDate = dto.RaceDate,
            RegistrationDeadline = dto.RegistrationDeadline ?? dto.RaceDate.AddDays(-1),
            DistanceMeters = dto.DistanceMeters,
            MaxParticipants = dto.MaxParticipants,
            EntryFee = dto.EntryFee,
            PrizePool = dto.PrizePool,
            Status = RaceStatuses.OpenForRegistration,
            Description = dto.Description?.Trim(),
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
