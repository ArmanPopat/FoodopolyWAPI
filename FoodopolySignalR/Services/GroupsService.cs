using GameClasses;
using System.Diagnostics;

namespace FoodopolySignalR.Services;


//Called through SingalR     This service is here because SignalR does not keep track of groups, but signal R can also communicate to groups through this
public class GroupsService
{
    private Dictionary<string, GameClass> groupDict;   //dictionairy of signalr groups, 
    private RunningGameService _runningGameService;
    public GroupsService(RunningGameService runningGameService) 
    {
        groupDict= new Dictionary<string, GameClass>();
        _runningGameService = runningGameService;
    }

    //method to create data for group and add to dict
    public void CreateGroupData(int gameId, GameClass gameClass)
    {
        try
        {
            groupDict.Add(gameId.ToString(), gameClass);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            throw;  //throw again because the error if group with that id exist should never happen and webapi call should vaildate before
        }
    }

    //Joins a group, creates this group in groupDict if neccessary
    public async Task JoinOrCreateAndJoinGroupData()
    {
        await Task.Run(() =>
        {

        });
    }

}
