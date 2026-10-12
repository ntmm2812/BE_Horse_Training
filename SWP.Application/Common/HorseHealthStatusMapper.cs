using SWP.Application.DTOs.Horses;
using SWP.Domain.Models.Entity;

namespace SWP.Application.Common;

public static class HorseHealthStatusMapper
{
    public static HorseHealthStatusDto ToDto(HorseHealthStatus status)
    {
        return new HorseHealthStatusDto
        {
            HorseID = status.HorseId,
            TrangThai = status.TrangThai,
            NgayCapNhat = status.NgayCapNhat.ToString("yyyy-MM-dd"),
        };
    }
}
