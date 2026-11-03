using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SWP.Application.Common;
using SWP.Domain.Common;

namespace SWP.API.Realtime;

[Authorize(Roles = RoleNames.HorseOwner)]
public class HorseActivityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, HorseRealtimeGroups.Owner(userId));
        }

        await base.OnConnectedAsync();
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var sub = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(sub) && int.TryParse(sub, out userId);
    }
}
