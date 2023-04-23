using Microsoft.AspNetCore.Authorization;

namespace FoodopolyWAPI.Authorisation;

public class GameMethodAuthorisationRequirement : IAuthorizationRequirement
{
    public GameMethodAuthorisationRequirement()
    { }
}
