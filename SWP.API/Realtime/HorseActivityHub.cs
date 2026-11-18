using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SWP.Application.Common;
using SWP.Domain.Common;

namespace SWP.API.Realtime;

[Authorize(Roles = RoleNames.HorseOwner + "," + RoleNames.HeadTrainer + "," + RoleNames.ClubManager + "," + RoleNames.Groom)]
public class HorseActivityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
        {
            if (Context.User!.IsInRole(RoleNames.HorseOwner))
                await Groups.AddToGroupAsync(Context.ConnectionId, HorseRealtimeGroups.Owner(userId));
            else if (Context.User.IsInRole(RoleNames.Groom))
                await Groups.AddToGroupAsync(Context.ConnectionId, "training-grooms");
            else
                await Groups.AddToGroupAsync(Context.ConnectionId, "approval-staff");
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
