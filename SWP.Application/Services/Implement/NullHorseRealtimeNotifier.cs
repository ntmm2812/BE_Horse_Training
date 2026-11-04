using SWP.Application.Services.Interface;

namespace SWP.Application.Services.Implement;

/// <summary> Fallback khi API chưa đăng ký SignalR (vd. test Application đơn lẻ). </summary>
public sealed class NullHorseRealtimeNotifier : IHorseRealtimeNotifier
{
    public Task NotifyHorseChangedAsync(int horseId, string kind, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyOwnerAsync(int ownerUserId, int horseId, string kind, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
