using FoodopolyWAPI.Records;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PlayerClasses;
using System.Diagnostics;

namespace FoodopolyWAPI.Authorisation;


//AUthorisation for Methods that affect a player, makes sure only that player will be able to do their own stuff, METHODS THAT USE THIS TAG MUST HAVE THE PLAYERAUTHORISATIONRECORD AS FIRST ARGS
public class GameMethodAuthorisationRequirementHandler : AuthorizationHandler<GameMethodAuthorisationRequirement, HubInvocationContext>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GameMethodAuthorisationRequirement requirement, HubInvocationContext resource)
    {
        PlayerAuthorisationRecord player;
        try
        {
            player = (PlayerAuthorisationRecord)resource.HubMethodArguments[0];
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return;
        }
        if (await IsUserAllowed(resource.Context, player))
        {
            context.Succeed(requirement);
        }         
    }
    private async Task<bool> IsUserAllowed(HubCallerContext context, PlayerAuthorisationRecord player)
    {
        return await Task<bool>.Run(() =>
        {
            ConnectionRecord connection;
            try
            {
                connection = (ConnectionRecord)context.Items["connection"];

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            if (player.username != connection.username || player.password != connection.password)
            {
                return false;
            }
            return true;
        });
        
    }
}
