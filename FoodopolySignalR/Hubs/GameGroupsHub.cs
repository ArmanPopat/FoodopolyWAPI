﻿using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace FoodopolySignalR.Hubs;

public class GameGroupsHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("Send", $"{Context.ConnectionId} has joined the group {groupName}.");
        Debug.Write($"{Context.ConnectionId} has joined the group {groupName}.");
    }
    public async Task RemoveFromGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync("Send", $"{Context.ConnectionId} has left the group {groupName}.");
    }

}
