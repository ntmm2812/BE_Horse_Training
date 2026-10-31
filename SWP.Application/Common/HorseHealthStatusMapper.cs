using SWP.Application.DTOs.Horses;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;

namespace SWP.Application.Common;

public static class HorseHealthStatusMapper
{
    public static HorseHealthStatusDto ToDto(HorseHealthStatus status)
    {
        return new HorseHealthStatusDto
        {
            HorseID = status.HorseId,
            TrangThai = ToClientHealthStatus(status.TrangThai),
            NgayCapNhat = status.NgayCapNhat.ToString("yyyy-MM-dd"),
        };
    }

    private static string ToClientHealthStatus(string? status) => status?.Trim() switch
    {
        HorseHealthStatuses.DuDieuKien or HorseHealthStatuses.Tot => HorseHealthStatuses.Tot,
        HorseHealthStatuses.CanTheoDoi => HorseHealthStatuses.CanTheoDoi,
        HorseHealthStatuses.ChanThuong or HorseHealthStatuses.CachLy or HorseHealthStatuses.DangDieuTri => HorseHealthStatuses.DangDieuTri,
        var value => value ?? string.Empty,
    };
}
