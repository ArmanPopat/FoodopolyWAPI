using FoodopolyWAPI.Records;

namespace FoodopolyWAPI.Services;

public class APIProcessor
{
    private GameService _gameService;
    public APIProcessor(GameService gameService)
    {
        _gameService = gameService;
    }

    public async Task<(bool success, string message, int gameIdOrStatusCode)> POSTCreatGameProcessor(CreateGameRecord gameRecord)
    {
        var successAndMessageAndGameIdOrStatusCode = await _gameService.CreateGameAndJoinPrep(gameRecord.gamePassword, gameRecord.playerName, gameRecord.playerPassword);
        return successAndMessageAndGameIdOrStatusCode;
    }

    public async Task<(bool success, string errorMessage, int statusCode)> PATCHJoinGameProcessor(JoinGameRecord gameRecord)
    {
        bool isIdInt = int.TryParse(gameRecord.gameId, out var gameId);
        if (!isIdInt)
        {
            return (false, "Game ID is not in integer format", 400);
        }
        var successAndMessageAndCode = await _gameService.OnlyJoinGameAsync(gameId, gameRecord.gamePassword, gameRecord.playerName, gameRecord.playerPassword);
        return successAndMessageAndCode;
    }
}
