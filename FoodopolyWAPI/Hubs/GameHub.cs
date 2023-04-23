using BoardClasses;
using FoodopolyWAPI.Records;
using FoodopolyWAPI.Services;
using GameClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PlayerClasses;
using SetClasses;
using System.Diagnostics;
using System.IO.Pipelines;

namespace FoodopolyWAPI.Hubs;

//has gamespecific methods, as all games must have group stuff but the game group hub could technically be reused
[Authorize]
public class GameHub : GameGroupsHub
{
    public GameHub(RunningGameService runningGameService, GroupsService groupsService) : base(runningGameService, groupsService)
    { }

    public async Task SendMethod(string method, string groupName, List<Object?>? args)
    {
        await Clients.Group(groupName).SendAsync(method, args);
    }

    private async Task<string> GettingGroupName()
    {
        return await Task<string>.Run(() =>
        {
            string groupName = string.Empty;
            try
            {
                ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
                groupName = connection.gameId.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Error();
            }
            return groupName;
        });
        
    }

    //[Authorize("GameGroupsAuthorisationConnect")]
    //public override async Task OnConnectedAsync()
    //{
    //    await base.OnConnectedAsync();
    //}

    //This increases the count of methods sent by server too keep track of by client
    //private Task<int> ConnectionServerMethodCount()
    //{
    //    return Task<int>.Run(() =>
    //    {
    //        int methodSentCount = 0;
    //        try
    //        {
    //            methodSentCount = (int)Context.Items["methodSentCount"];
    //            methodSentCount++;
    //            Context.Items["methodSentCount"] = methodSentCount;
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.WriteLine(ex.Message);
    //            Error();
    //        }
    //        return methodSentCount;
    //    });
    //}

    public async Task SendMessage(int msgCount, string msg)
    {
        //int methodSentCount = await ConnectionServerMethodCount();
        //maybe add some validation for group name etc


        await Clients.Group(await GettingGroupName()).SendAsync("Recieve Message", msgCount, msg);
    }

    private void Error()
    {
        throw new HubException("Error in connection, please reconnect.");
    }

    //Authorises it is the current players turn, throws error if not
    private void TurnAuthorisation()
    {
        try
        {
            ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
            if (connection.game.CurrentTurnPlayer.Name != connection.username)
            {
                Error();
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Error();
        }
        

    }

    //First Argument must be PLAYERAUTHORISATIONRECORD - The player they wish to move or do stuff as
    [Authorize(Policy = "GameMethodAuthorisation")]
    public async Task Buy(PlayerAuthorisationRecord player, int methodCount, int playerPos)
    {
        await Task.Run(async () =>
        {
            try
            {
                ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
                
                
                
                //Validation
                TurnAuthorisation();
                if (!(playerPos == connection.game.CurrentTurnPlayer.PlayerPos))
                {
                    Error();
                    Debug.WriteLine("player position not same on client and server");
                    return;
                }

                //Other Validtion occurs on the Buy Method - will by caught by try catch



                //Implementation -Moved to player class, ignore returned string
                connection.game.CurrentTurnPlayer.BuyPlayer(connection.game);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Error();
                return;
            }

            await Clients.Group(await GettingGroupName()).SendAsync("Bought", methodCount, player.username, playerPos);
        });
       
    }




    //First Argument must be PLAYERAUTHORISATIONRECORD - The player they wish to move or do stuff as
    [Authorize(Policy = "GameMethodAuthorisation")]
    public async Task CallStandardRollDiceEvent(PlayerAuthorisationRecord player, int methodCount)
    {
        GameClass game;
        PlayerClass playerClass;
        try
        {
            ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
            game = connection.game;
            //get connection playerclass
            playerClass = game.PlayerList.First<PlayerClass>(playerClass => playerClass.Name==connection.username);
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            Error();
            return;
        }
        await Task.Run(async () =>
        {
            TurnAuthorisation();
            (string Msg, bool Double, bool Diet, int TotalRoll) msgAndDoubleAndJailAndTotal = playerClass.StandardRollDiceEvent(game);
            //game.Turn.turnMsgCount++;
            //await SendMessage()
            Clients.Group(await GettingGroupName()).SendAsync("RecieveStandardDiceRoll", msgAndDoubleAndJailAndTotal, methodCount);


            //Calls the appropriate landevent-done Independently on server and client side, sse if it works
            await game.CurrentTurnPlayer.LandEventAsync(game);
        });

    }
}
