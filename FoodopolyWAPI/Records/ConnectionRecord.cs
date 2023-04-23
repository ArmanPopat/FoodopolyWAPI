using GameClasses;

namespace FoodopolyWAPI.Records;

public record ConnectionRecord(string username, string password, int gameId, GameClass game)
{
}
