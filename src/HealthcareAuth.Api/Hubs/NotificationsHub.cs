using System.Security.Claims;
using HealthcareAuth.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HealthcareAuth.Api.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        if (user?.IsInRole(AppRoles.Admin) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AppRoles.Admin);
            await Groups.AddToGroupAsync(Context.ConnectionId, AppRoles.Reviewer);
        }

        if (user?.IsInRole(AppRoles.Reviewer) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AppRoles.Reviewer);
        }

        await base.OnConnectedAsync();
    }
}
