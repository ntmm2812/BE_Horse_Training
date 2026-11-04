using Microsoft.AspNetCore.SignalR;
using SWP.Application.Common;
using SWP.Application.Services.Interface;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.API.Realtime;

public class HorseRealtimeNotifier : IHorseRealtimeNotifier
{
    private readonly IHubContext<HorseActivityHub> _hub;
    private readonly IHorseRepository _horses;
    private readonly ILogger<HorseRealtimeNotifier> _logger;

    public HorseRealtimeNotifier(
        IHubContext<HorseActivityHub> hub,
        IHorseRepository horses,
        ILogger<HorseRealtimeNotifier> logger)
    {
        _hub = hub;
        _horses = horses;
        _logger = logger;
    }

    public async Task NotifyHorseChangedAsync(int horseId, string kind, CancellationToken cancellationToken = default)
    {
        var horse = await _horses.GetByIdAsync(horseId, cancellationToken);
        if (horse is null)
        {
            return;
        }

        await NotifyOwnerAsync(horse.UserId, horseId, kind, cancellationToken);
    }

    public async Task NotifyOwnerAsync(int ownerUserId, int horseId, string kind, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(HorseRealtimeGroups.Owner(ownerUserId))
                .SendAsync(
                    HorseRealtimeEvents.HorseChanged,
                    new { horseId, kind },
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không gửi được realtime horseChanged cho owner {OwnerId}, horse {HorseId}", ownerUserId, horseId);
        }
    }
}
