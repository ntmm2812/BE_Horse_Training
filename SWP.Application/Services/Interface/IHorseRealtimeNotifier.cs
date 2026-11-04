namespace SWP.Application.Services.Interface;

public interface IHorseRealtimeNotifier
{
    /// <summary> Gửi sự kiện tới chủ sở hữu của ngựa (lookup OwnerID từ horseId). </summary>
    Task NotifyHorseChangedAsync(int horseId, string kind, CancellationToken cancellationToken = default);

    /// <summary> Gửi trực tiếp tới owner khi đã biết UserID (vd. sau khi xóa ngựa). </summary>
    Task NotifyOwnerAsync(int ownerUserId, int horseId, string kind, CancellationToken cancellationToken = default);
}
