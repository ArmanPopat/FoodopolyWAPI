using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodopolyWAPI.Records;

public record JoinGameRecord(string gameId, string gamePassword, string playerName, string playerPassword)
{
}
