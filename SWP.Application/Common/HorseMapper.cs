using SWP.Application.DTOs.Horses;
using SWP.Domain.Models.Entity;

namespace SWP.Application.Common;

public static class HorseMapper
{
    private const string DefaultStatus = "Active";
    private const string PedigreePrefix = "Dòng dõi: ";

    public static HorseDto ToDto(Horse horse)
    {
        return new HorseDto
        {
            HorseID = horse.HorseId,
            Ten = horse.HorseName,
            Giong = horse.Breed ?? string.Empty,
            Tuoi = CalculateAge(horse.DateOfBirth),
            CanNang = horse.Weight ?? 0,
            DongDoi = ExtractPedigree(horse.Description),
            OwnerID = horse.UserId,
            PhotoUrl = horse.ImageUrl,
        };
    }

    public static Horse ToEntity(SaveHorseRequestDto request)
    {
        return new Horse
        {
            HorseName = request.Ten.Trim(),
            Breed = request.Giong.Trim(),
            Weight = request.CanNang,
            DateOfBirth = AgeToDateOfBirth(request.Tuoi),
            Description = BuildDescription(request.DongDoi),
            UserId = request.OwnerID,
            HorseStatus = DefaultStatus,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static void ApplyUpdate(Horse horse, SaveHorseRequestDto request)
    {
        horse.HorseName = request.Ten.Trim();
        horse.Breed = request.Giong.Trim();
        horse.Weight = request.CanNang;
        horse.DateOfBirth = AgeToDateOfBirth(request.Tuoi);
        horse.Description = BuildDescription(request.DongDoi);
        horse.UserId = request.OwnerID;
        horse.UpdatedAt = DateTime.UtcNow;
    }

    private static string BuildDescription(string dongDoi) =>
        string.IsNullOrWhiteSpace(dongDoi) ? string.Empty : PedigreePrefix + dongDoi.Trim();

    private static string ExtractPedigree(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        return description.StartsWith(PedigreePrefix, StringComparison.OrdinalIgnoreCase)
            ? description[PedigreePrefix.Length..].Trim()
            : description.Trim();
    }

    private static DateOnly? AgeToDateOfBirth(int tuoi)
    {
        if (tuoi <= 0)
        {
            return null;
        }

        var year = DateTime.UtcNow.Year - tuoi;
        return new DateOnly(year, 1, 1);
    }

    private static int CalculateAge(DateOnly? dateOfBirth)
    {
        if (dateOfBirth is null)
        {
            return 0;
        }

        var age = DateTime.UtcNow.Year - dateOfBirth.Value.Year;
        return age > 0 ? age : 1;
    }
}
