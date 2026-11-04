namespace SWP.Application.Common;

public static class HorseRealtimeKinds
{
    public const string TapLuyen = "TapLuyen";
    public const string ThiDau = "ThiDau";
    public const string TiemPhong = "TiemPhong";
    public const string KhamBenh = "KhamBenh";
    public const string KhoaTap = "KhoaTap";
    public const string ChamSoc = "ChamSoc";
    public const string SuCo = "SuCo";
    public const string HoSo = "HoSo";
}

public static class HorseRealtimeEvents
{
    public const string HorseChanged = "horseChanged";
}

public static class HorseRealtimeGroups
{
    public static string Owner(int ownerUserId) => $"owner:{ownerUserId}";
}
