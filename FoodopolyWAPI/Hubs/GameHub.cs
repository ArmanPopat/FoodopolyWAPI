﻿using BoardClasses;
using FoodopolyClasses;
using FoodopolyWAPI.Records;
using FoodopolyWAPI.Services;
using GameClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;
using FoodopolyClasses.PlayerClasses;
using SetClasses;
using System.Diagnostics;
using System.IO.Pipelines;
using FoodopolyClasses.Records;
using FoodopolyClasses.TradeClasses;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

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

    //might change this to send gameclass over first
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

    public async Task EndTurn(PlayerAuthorisationRecord player, int methodCount)
    {
        await Task.Run(async () =>
        {
            try
            {
                ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];



                //Validation
                TurnAuthorisation();
                if (!connection.game.Turn.RollEventDone) //Might Need Checking
                {
                    Error();
                    return;
                }
                string msg = $"{connection.game.CurrentTurnPlayer.Name} has ended their turn.";
                CalledGameMethods.NextTurn(connection.game);
                await Clients.Group(await GettingGroupName()).SendAsync("StartTurn", methodCount, connection.game, msg);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Error();
                return;
            }
        });
    }

    //First Argument must be PLAYERAUTHORISATIONRECORD - The player they wish to move or do stuff as
    [Authorize(Policy = "GameMethodAuthorisation")]
    public async Task TradeInitiated(PlayerAuthorisationRecord player, int methodCount, InitiateTradeRecord tradeRecord)
    {
        //Validation
        await Task.Run(async () => 
        {
            try
            {
                ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
                if (!Trade.ValidateTrade(connection.game, player.username, tradeRecord))
                {
                    await Clients.Group(await GettingGroupName()).SendAsync("TradeError", methodCount, player.username, tradeRecord, connection.game);
                    Debug.WriteLine("Trade Error");
                    return;
                }
                //Add to GameClass TradeRecords
                int recordKey;
                if (connection.game.TradeRecords.Count == 0)
                {
                    recordKey = 1;
                }
                else
                {
                    recordKey = connection.game.TradeRecords.Keys.Max() + 1;
                }

                connection.game.TradeRecords.Add(recordKey, tradeRecord);
                await Clients.Group(await GettingGroupName()).SendAsync("TradeReceived", methodCount, player.username,recordKey, tradeRecord, connection.game);
                return;
                
                

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Error();
                return;
            }
        });

    }

    //First Argument must be PLAYERAUTHORISATIONRECORD - The player they wish to move or do stuff as
    [Authorize(Policy = "GameMethodAuthorisation")]
    public async Task TradeConfirmed(PlayerAuthorisationRecord player, int methodCount, int tradeRecordKey, InitiateTradeRecord tradeRecordSent, List<(int BoardPosition, bool ToUnmortgage)> mortPropsToDo)
    {
        //Validation
        await Task.Run(async () =>
        {
            
            
            try
            {
                ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
                InitiateTradeRecord tradeRecord;
                try
                {
                    tradeRecord = connection.game.TradeRecords[tradeRecordKey];
                    if (tradeRecord != tradeRecordSent)
                    {
                        throw new InvalidDataException();
                    }
                }
                catch
                {
                    Debug.WriteLine("Can't find trade record from the tradeRecordKey or trade Record Different");
                    await Clients.Caller.SendAsync("TradeNoLongerValid", methodCount, tradeRecordKey, tradeRecordSent, connection.game);
                    return;
                }

                //check player is the proposed to
                if (tradeRecord.otherPlayerName != player.username)
                {
                    Debug.WriteLine("The player trying to accept is no the one being proposed to.");
                    Error();
                    return;
                }


                if (!Trade.ValidateTrade(connection.game, player.username, tradeRecord))
                {
                    connection.game.TradeRecords.Remove(tradeRecordKey);
                    await Clients.Caller.SendAsync("TradeNoLongerValid", methodCount, tradeRecordKey, tradeRecord, connection.game);
                    Debug.WriteLine("TradeNoLongerValid");
                    return;
                }
                //Confirm Trade
                await Trade.AcceptTradeAsync(tradeRecordKey,tradeRecord, connection.game, mortPropsToDo);
                await Clients.Group(await GettingGroupName()).SendAsync("TradeConfirmed", methodCount, tradeRecordKey, tradeRecord, mortPropsToDo);
                return;



            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Error();
                return;
            }
        });

    }

    //First Argument must be PLAYERAUTHORISATIONRECORD - The player they wish to move or do stuff as
    [Authorize(Policy = "GameMethodAuthorisation")]
    public async Task UnmortgageOrFeeTradeResponse(PlayerAuthorisationRecord player, int methodCount, InitiateTradeRecord tradeRecordSent, List<(int BoardPosition, bool ToUnmortgage)> mortPropsToDo)
    {
        PlayerClass playerClass;
        GameClass game;
        await Task.Run(async () =>
        {
            try
            {
                ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
                game = connection.game;
                //get connection playerclass
                playerClass = game.PlayerList.First<PlayerClass>(playerClass => playerClass.Name == connection.username);
                (List<Station> selectedStations, List<Utility> selectedUtilities, List<Property> selectedProperties) = await Trade.IdentifyTheOwnedStuff(tradeRecordSent.theirSelectedStationsBoardPos,
                tradeRecordSent.theirSelectedUtilitiesBoardPos, tradeRecordSent.theirSelectedPropertiesBoardPos, game);
                playerClass.MorgatgeFeesNotPaidOrUnMortgaged.Remove(tradeRecordSent);
                await Trade.UnMortgageOrPayFee(playerClass, selectedStations, selectedUtilities, selectedProperties, mortPropsToDo);
                await Clients.Group(await GettingGroupName()).SendAsync("UnmortgageOrFeeTradeResponded", methodCount, tradeRecordSent, mortPropsToDo);
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Error();
                return;
            }
            
        });
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
    public async Task Mortgage(PlayerAuthorisationRecord player, int methodCount, int boardPosition)
    {
        var undesiderdPos = new int[] { 7, 22, 36, 2, 17, 33, 0, 10, 20, 30, 4, 38 };
        if (undesiderdPos.Any(o => o == boardPosition))
        {
            Debug.WriteLine($"Error: Trying To Mortgage not a Property.");
            Error();
            return;
        }
        GameClass game;
        try
        {
            ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
            game = connection.game;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            Error();
            return;
        }
        await Task.Run(async () =>
        {
            if (boardPosition == 12 || boardPosition == 28)
            {
                Utility utility = game.utilities.Properties.First(o => o.BoardPosition == boardPosition);
                if (!utility.Owned)
                {
                    Debug.WriteLine("Error: Property Unowned");
                    Error();
                    return;
                }
                if (utility.Owner.Name != player.username)
                {
                    Debug.WriteLine("Error: Property Owned by Another Player");
                    Error();
                    return;
                }
                if (utility.Mortgaged)
                {
                    Debug.WriteLine("Error: Property already mortgaged.");
                    Error();
                    return;
                }

                utility.Mortgage();
                await Clients.Group(await GettingGroupName()).SendAsync("Mortgage", boardPosition, methodCount);
                return;
            }
            else if (boardPosition == 5 || boardPosition == 15 || boardPosition == 25 || boardPosition == 35)
            {
                Station station = game.stations.Properties.First(o => o.BoardPosition == boardPosition);
                if (!station.Owned)
                {
                    Debug.WriteLine("Error: Property Unowned");
                    Error();
                    return;
                }
                if (station.Owner.Name != player.username)
                {
                    Debug.WriteLine("Error: Property Owned by Another Player");
                    Error();
                    return;
                }
                if (station.Mortgaged)
                {
                    Debug.WriteLine("Error: Property already mortgaged.");
                    Error();
                    return;
                }

                station.Mortgage();
                await Clients.Group(await GettingGroupName()).SendAsync("Mortgage", boardPosition, methodCount);
                return;
            }
            foreach (KeyValuePair<string, SetProp> keyValue in game.setsPropDict)
            {
                foreach (Property property in keyValue.Value.Properties)
                {
                    if (property.BoardPosition == boardPosition)
                    {
                        //Checking
                        if (!property.Owned)
                        {
                            Debug.WriteLine("Error: Property Unowned");
                            Error();
                            return;
                        }
                        if (property.Owner.Name != player.username)
                        {
                            Debug.WriteLine("Error: Property Owned by Another Player");
                            Error();
                            return;
                        }
                        if (property.Mortgaged)
                        {
                            Debug.WriteLine("Error: Property already mortgaged.");
                            Error();
                            return;
                        }
                        if (property.NumOfUpgrades != 0)
                        {
                            Debug.WriteLine("Error: Property has upgrades.");
                            Error();
                            return;
                        }
                        if (keyValue.Value.Properties.Any(o => o.NumOfUpgrades != 0)) //Check
                        {
                            Debug.WriteLine("Error: Set has upgrades.");
                            Error();
                            return;
                        }    
                        property.Mortgage();
                        await Clients.Group(await GettingGroupName()).SendAsync("Mortgage", boardPosition, methodCount);
                        return;
                    }
                }
            }
            Debug.WriteLine("Error: Property not mortgagable.");
            Error();
            return;

        });
    }


    //First Argument must be PLAYERAUTHORISATIONRECORD - The player they wish to move or do stuff as
    [Authorize(Policy = "GameMethodAuthorisation")]
    public async Task Upgrade(PlayerAuthorisationRecord player, int methodCount, int boardPosition)
    {
        var undesiderdPos = new int[] { 7, 22, 36, 2, 17, 33, 0, 10, 20, 30, 4, 38, 5, 15, 25, 35, 12, 28 };
        if (undesiderdPos.Any(o => o == boardPosition))
        {
            Debug.WriteLine($"Error: Trying To Upgrade an Upgradable Property.");
            Error();
            return;
        }
        GameClass game;
        try
        {
            ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
            game = connection.game;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            Error();
            return;
        }
        await Task.Run(async () =>
        {
            foreach (KeyValuePair<string, SetProp> keyValue in game.setsPropDict)
            {
                foreach (Property property in keyValue.Value.Properties)
                {
                    if (property.BoardPosition == boardPosition)
                    {

                        //Checking
                        if (property.Owned && keyValue.Value.SetExclusivelyOwned)
                        {
                            if (property.Owner.Name == player.username)
                            {
                                if (property.NumOfUpgrades < 5)
                                {
                                    if (property.Owner.Cash >= property.UpgradeCost)
                                    {
                                        if (keyValue.Value.Properties.All(o => o.NumOfUpgrades >= property.NumOfUpgrades))
                                        {
                                            property.Upgrade();
                                            await Clients.Group(await GettingGroupName()).SendAsync("Upgrade", boardPosition, methodCount);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        Error();
                        return;
                    }
                }
            }

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
            //Validates if it's their turn
            TurnAuthorisation();
            //Validates if allowed to roll
            if (game.Turn.RollEventDone)
            {
                Error();
            }
            (string Msg, bool Double, bool Diet, int TotalRoll) msgAndDoubleAndJailAndTotal = playerClass.StandardRollDiceEvent(game);
            //game.Turn.turnMsgCount++;
            //await SendMessage()
            await Clients.Group(await GettingGroupName()).SendAsync("RecieveStandardDiceRoll", msgAndDoubleAndJailAndTotal, methodCount);


            //Calls the appropriate landevent-done Independently on server and client side, sse if it works
            await game.CurrentTurnPlayer.LandEventAsync(game);
        });
        
    }
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        try
        {
            ConnectionRecord connection = (ConnectionRecord)Context.Items["connection"];
            //get connection playerclass
            PlayerClass playerClass = connection.game.PlayerList.First<PlayerClass>(playerClass => playerClass.Name == connection.username);
            if (playerClass.CheckIfMortgageFeesHaveToBePaid())
            {
                await Clients.Caller.SendAsync("TradeMortgageFeesDue");
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            Error();
            return;
        }
    }
}
