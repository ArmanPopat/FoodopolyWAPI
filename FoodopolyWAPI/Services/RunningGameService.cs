using FoodopolyClasses;
using GameClasses;
using PlayerClasses;
using System.Diagnostics;

namespace FoodopolyWAPI.Services;


//Called though api service
public class RunningGameService
{
    public Dictionary<int, GameClass> GameDict { get; set; } //Change String to GameClass Here!!! Needs to be created
    private int _gameInitialId = 1000;

    public RunningGameService()
    {
        GameDict= new Dictionary<int, GameClass>();
    }
    public async Task<GameClass> GameIdentifierAsync(int id)
    {
        return await Task<GameClass>.Run(() =>
        {
            if (!GameDict.ContainsKey(id))
            {
                Debug.WriteLine("No Game with that id");
                //Code here
                throw new InvalidOperationException("No Game with that ID");
            }

            //return above should prevent this out of index error thingy 
            GameClass gameClass = GameDict[id];
            return gameClass;
        });
    }

    public async Task PartialDeleteGameAsync(int id)
    {
        await Task.Run(() => {
            if (!GameDict.ContainsKey(id))
            {
                throw new InvalidOperationException("No Game With That Id");
            }
            GameDict.Remove(id);
        });
    }
    public async Task DeleteGameAsync(int id)
    {
        ///try catch stuff here?
        await PartialDeleteGameAsync(id);
    }


    //creates game with different keys
    public async Task<int> CreateGameAsync(string password)
    {
        int gameIdValue = await Task<int>.Run(async () =>
        {
            int gameID = GameIdGenerator();
            //GameClass gameClass = new GameClass(gameID, password);
            GameBuilderClass gameBuilderClass = new GameBuilderClass();
            GameClass gameClass =await gameBuilderClass.BuildGameClass(gameID, password);
            GameDict.Add(gameID, gameClass);
            return gameID;
        });
        return gameIdValue;
    }


    //ensures different keys
    private int GameIdGenerator()  //decided against async stuff, because of overhead
    {
        
        _gameInitialId++;
        return _gameInitialId;
    }


    //beleive is unneccessary here,put in api service
    ////error not dealt with here, in api service instead
    //public void JoinGame(GameClass gameClass, PlayerClass player)
    //{
    //    gameClass.AddPlayer(player);
    //}
}
