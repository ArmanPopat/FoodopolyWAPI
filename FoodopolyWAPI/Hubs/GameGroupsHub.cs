using FoodopolyWAPI.Records;
using FoodopolyWAPI.Services;
using GameClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using FoodopolyClasses.PlayerClasses;
using System;
using System.Diagnostics;

namespace FoodopolyWAPI.Hubs;

[Authorize]
//Base Hub Contains Group stuff
public class GameGroupsHub : Hub
{
    private RunningGameService _runningGameService;
    private GroupsService _groupsService;

    public GameGroupsHub(RunningGameService runningGameService, GroupsService groupsService)
    {
        _runningGameService = runningGameService;
        _groupsService = groupsService;
    }

    //Used in OnConnected override
    private async Task JoinGroupAsync(string username, string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("RecieveMessage", $"{username} has connected to the game.");
        Debug.Write($"{username} has connected to the game {groupName}.");

    }

    //Used in OnDisconnected override
    private async Task RemoveFromGroupAsync(string username, string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("RecieveMessage", $"{username} has disconnected from the game.");  //figure this out
        Debug.Write($"{username} has disconnected from the game {groupName}.");
    }



    //public async Task SendObjectAsync(string groupName, object objectItem)
    //{
    //    await Clients.Group(groupName).SendAsync("RecieveObject", objectItem);  //figure this out
    //}
    //ADD TO RETURN CALL METHOD ON CLIENT TO FORCE DISCONNECT

    //[Authorize(Policy = "GameGroupsAuthorisationConnect")]
    //[Authorize("GameGroupsTestAuthorizationRequirement")]
    public override async Task OnConnectedAsync()
    {

        //bool connectionSuccess = Context.Items.TryGetValue("connection", out var connectionTemp);


        //if (!connectionSuccess || connectionTemp == null)
        //{
        //    Debug.WriteLine("no connection record found with this context");
        //    //DoSomethingHere

        //    //TEMRINATE CONNECTION HERE
        //    return;
        //}

        ConnectionRecord connection;
        GameClass game;
        try 
        {
            var identity = Context.User.Identities.First(ident => ident.AuthenticationType == "Connection");
            string username = identity.Claims.First(claim => claim.Type == "username").Value;
            string password = identity.Claims.First(claim => claim.Type == "password").Value;
            string gameId = identity.Claims.First(claim => claim.Type == "gameId").Value;
            int gameIdInt = int.Parse(gameId);
            game = await _runningGameService.GameIdentifierAsync(gameIdInt);
            connection = new ConnectionRecord(username, password, gameIdInt, game);
            Context.Items.Add("connection", connection);
        }
        catch (Exception ex)
        {
            //TERMINATE CONNECTION HERE
            Debug.Write(ex.Message + "\nERROR in Game Groups Hub");
            return;
        }

        string gameIdString;

        await base.OnConnectedAsync();
        try
        {
            //ConnectionRecord connection = new ConnectionRecord(username, password, Context.ConnectionId, gameIdInt);

            //connection = (ConnectionRecord)connectionTemp;
            gameIdString = connection.gameId.ToString();
            await JoinGroupAsync(connection.username, gameIdString);
            await _groupsService.JoinOrCreateAndJoinGroupDataAsync(gameIdString, connection);
            
            //Did thia so it can be easily accessed in my override of the ondisconnected() method
            
            
            //Context.Items.Add("connection", connection);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return; 
        }

        await Clients.Group(gameIdString).SendAsync("RecieveGame", game);
        //As Above Sends Probs will serialize manually and send out, we will see if this will solve the problem
        //string gameJsonString = JsonConvert.SerializeObject(game);
        //await Clients.Group(gameIdString).SendAsync("RecieveGame2", gameJsonString);
    }

    //onreconnected no longer a method so shall be handeled serverside, calling a method, here?

    public override async Task OnDisconnectedAsync(Exception? exception)
    {

        bool connectionSuccess = Context.Items.TryGetValue("connection", out var connectionTemp);
       
        
        if (!connectionSuccess || connectionTemp == null)
        {
            Debug.WriteLine("no connection record found with this context");
            //DoSomethingHere

            //put this here, maybe add to a log before??
            await base.OnDisconnectedAsync(exception);
            return;
        }


        try
        {
            ConnectionRecord connection = (ConnectionRecord)connectionTemp;
            await RemoveFromGroupAsync(connection.username, connection.gameId.ToString());
            await _groupsService.RemoveOrRemoveFromAndDeleteGroupDataAsync(connection.gameId.ToString(), connection);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            //Maybe log this?
        }

        await base.OnDisconnectedAsync(exception);
    }
}

