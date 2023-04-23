using GameClasses;
using System.Diagnostics;
using FoodopolyWAPI.Records;
using System.Reflection.Metadata.Ecma335;

namespace FoodopolyWAPI.Services;


//Called through SingalR     This service is here because SignalR does not keep track of groups, but signal R can also communicate to groups through this
public class GroupsService
{
    private Dictionary<string, List<ConnectionRecord>> groupDict;   //dictionairy of signalr groups, 
    private RunningGameService _runningGameService;
    public GroupsService(RunningGameService runningGameService) 
    {
        groupDict= new Dictionary<string, List<ConnectionRecord>>();
        _runningGameService = runningGameService;
    }

    //method to create data for group and add to dict
    //should only be called after game is created!
    private void CreateGroupAndJoinDataAsync(string gameId, ConnectionRecord connection)
    {
        try
        {
            //Can use for validation if we want later - to check game has someone with that username
            //GameClass gameClass = await _runningGameService.GameIdentifierAsync(gameId); //will throw an error if no game with this id exists

            //ConnectionModel connectionModel = new ConnectionModel();
            List<ConnectionRecord> connectionList = new(){connection};
            groupDict.Add(gameId.ToString(), connectionList);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            throw;  //throw again because the error if group with that id exist should never happen and webapi call should vaildate before
        }
    }

    //Joins a group, creates this group in groupDict if neccessary, only this method should be called when a connection made ?
    public async Task<string?> JoinOrCreateAndJoinGroupDataAsync(string gameId, ConnectionRecord connection)
    {
        return await Task<string?>.Run(() => {
            string? msg = null;
            if (groupDict.ContainsKey(gameId.ToString()))
            {
                if (groupDict[gameId.ToString()].Any(connectionIn => connectionIn == connection))
                {
                    Debug.WriteLine("Same connection already exists, should not happen");
                    msg = "Same connection already exists, should not happen";
                }
                if (groupDict[gameId.ToString()].Any(connectionIn => connectionIn.username == connection.username))
                {
                    Debug.WriteLine("Connection with that username already existed, adding it, could be another system with same user");
                    msg = "Connection with that username already existed, adding it, could be another system with same user";
                    groupDict[gameId.ToString()].Add(connection);
                }
            }
            else
            {
                CreateGroupAndJoinDataAsync(gameId, connection);
            }
            return msg;
        });
    }

    //no validation in this method, not called outside
    private bool RemoveFromAndDeleteGroupDataAync(string gameId, ConnectionRecord connection)
    {
        return groupDict.Remove(gameId.ToString());
    }
    public async Task<string?> RemoveOrRemoveFromAndDeleteGroupDataAsync(string gameId, ConnectionRecord connection)
    {
        return await Task<string?>.Run(() =>
        {
            string gameIdString = gameId.ToString();
            string? msg = null;
            if (!groupDict.ContainsKey(gameIdString))
            {
                msg = "No group with that GameId Key";
                Debug.WriteLine(msg);
                return msg;
            }
            if (!groupDict[gameIdString].Contains(connection))
            {
                msg = "This connection does not exist in that group";
                Debug.WriteLine(msg);
                return msg;
            }
            if (groupDict[gameIdString].Count()>1)
            {
                groupDict[gameIdString].Remove(connection);
            }
            else
            {
                RemoveFromAndDeleteGroupDataAync(gameId,connection);
            }
            return msg;
        });
    }

}
