using GameClasses;
//using FoodopolySignalR.Services;
using FoodopolyClasses.PlayerClasses;
using System.Diagnostics;
using StandardInformation;
using FoodopolyClasses.StandardInformation;

namespace FoodopolyWAPI.Services;

//Does the calls of the WebApi Stuff
public class GameService
{
    public GameService(RunningGameService runningGameService) 
    {
        _runningGameService = runningGameService;
    }
    private RunningGameService _runningGameService;

    
    //Creates Game Object and joins the game, does not interact with signalR groups
    public async Task<(bool success, string message, int gameIdOrStatusCode)> CreateGameAndJoinPrep(string password, string username, string userPassword)
    {
        
        PlayerClass player;
        int gameIdOrStatusCode;
        try
        {
            player = new PlayerClass(username, userPassword, DefaultVariables.startingCash, null);
            gameIdOrStatusCode = await _runningGameService.CreateGameAsync(password);
        }
        catch(Exception ex)
        {
            return (false, ex.Message, 400);
        }


        //Join code here

        //Note DefualtValue used for starting class here, can make adjustments later to make variable.
        //Also note player to create is number 1 playingpos
        var successAndMessage = await JoinGameForNewAsync(gameIdOrStatusCode, player);

        return (successAndMessage.success, successAndMessage.message, gameIdOrStatusCode);
    }

    


    //try catch will be here
    private async Task PartialJoinGameAsync(GameClass gameClass, PlayerClass player)
    {


        await Task.Run(() =>
        {
            gameClass.AddPlayer(player);
        });
    }

    public async Task<(bool success, string message)> JoinGameForNewAsync(int id, PlayerClass player)
    {
        //GameClass gameClass;
        try
        {
            GameClass gameClass = await _runningGameService.GameIdentifierAsync(id);
            await PartialJoinGameAsync(gameClass, player);
            return (true,"");
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine(ex.Message);
            return (false,ex.Message);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return (false, e.Message);
        }

    }

    private static bool GamePassValidation(GameClass gameClass, string gamePassword)
    {
        gamePassword = gamePassword.Trim();
        if (gameClass.Password == gamePassword)
        {
            return true;
        }
        return false;
    }

    private static bool PlayerPassValidation(PlayerClass playerClass, string playerPassword)
    {
        playerPassword= playerPassword.Trim();
        if(playerClass.Password == playerPassword)
        {
            return true;
        }
        return false;
    }

    private static async Task<bool> DoesPlayerExistAlreadyAsync(GameClass gameClass, string username)
    {
        return await Task<bool>.Run(() => {
            //PlayerClass playerTemp = gameClass.PlayerList.First<PlayerClass>(player => player.Name == username);
            if (gameClass.PlayerList.Any(player => player.Name == username))
            {
                return true;
            }
            return false; });
    }

    //Call this method if only wants to join
    public async Task<(bool success, string errorMessage, int statusCode)> OnlyJoinGameAsync(int id, string gamePassword, string username,string userPassword)
    {
        //Makes sure game exists with that Id
        int statusCode = 500;
        GameClass gameClass;
        try
        {
            gameClass = await _runningGameService.GameIdentifierAsync(id);
        }
        catch (InvalidOperationException ex)
        { 
            Debug.WriteLine(ex.Message);
            
            if (ex.Message == "No Game With That Id")
            {
                statusCode = 404;
            }
            return (false,ex.Message, statusCode);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return (false, e.Message, statusCode);
        }


        PlayerClass player;

        //Validates Game Password
        if (!GamePassValidation(gameClass, gamePassword))
        {
            statusCode = 401;
            return (false, "Wrong Game Password", statusCode);
        }



        //checks if player exists and validates password
        if (await DoesPlayerExistAlreadyAsync(gameClass, username))     //Problem: if another user tries to join with same name, they will ask for pass
        {
            statusCode = 401;
            player = gameClass.PlayerList.First<PlayerClass>(player => player.Name == username);   ///will need testing
            if (!PlayerPassValidation(player, userPassword))
            {
                return (false, "Wrong User Password", statusCode);     //Maybe add someone already chose that name???
            }

            ///just give them the ok to rejoin i.e 200 status code
            return (true, "", 200);

        }

        try
        {
            player = new PlayerClass(username, userPassword, DefaultVariables.startingCash, null);
            await PartialJoinGameAsync(gameClass, player);
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine(ex.Message);
            if (ex.Message == "The Game is Full")
            {
                statusCode = 406;
            }
            return (false, ex.Message, statusCode);
        }
        catch(Exception e)
        {
            Debug.WriteLine(e.Message);
            return (false, e.Message, statusCode);
        }
        return (true, "", 200);
    }

    //more stuff here
}
