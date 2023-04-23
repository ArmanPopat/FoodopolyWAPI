using FoodopolyWAPI.Records;
using FoodopolyWAPI.Services;
using GameClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PlayerClasses;
using System.Diagnostics;

namespace FoodopolyWAPI.Authorisation;

public class GameGroupsTestAuthorizationRequirement: AuthorizationHandler<GameGroupsTestAuthorizationRequirement, HubInvocationContext>,
    IAuthorizationRequirement
{
    private RunningGameService _runningGameService;
    public GameGroupsTestAuthorizationRequirement(RunningGameService runningGameService)
    {
        _runningGameService = runningGameService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GameGroupsTestAuthorizationRequirement requirement, HubInvocationContext resource)
    {
        if (await IsUserAllowedAndAddData(resource.HubMethodName, resource.Context))
        {
            context.Succeed(requirement);
        }
    }

    private async Task<bool> IsUserAllowedAndAddData(string hubMethodName, HubCallerContext context)
    {
        //assigned values to satisfy error stuff
        string username = "1000";
        string password = "password";
        string gameId = "1000";
        int gameIdInt = 1000;

        //making sure no empty or null or incorrect character
        bool worked = await Task<bool>.Run(() =>
        {

            var httpContext = context.GetHttpContext();
            if (httpContext == null)
            {
                return false;
                //return and deny connection
            }
            try
            {
                username = httpContext.Request.Query["username"].ToString().Trim();
                password = httpContext.Request.Query["password"].ToString().Trim();
                gameId = httpContext.Request.Query["gameId"].ToString().Trim();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }


            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(gameId))
            {
                return false;
            }

            bool gameIdIntSuccess = int.TryParse(gameId, out gameIdInt);
            if (!gameIdIntSuccess)
            {
                return false;
                //deny connection, disconnect
            }
            return true;
        });

        if (!worked)
        {
            return false;
        }
        //validation to see if player should join here, will disconnect if not
        GameClass game;
        try
        {
            game = await _runningGameService.GameIdentifierAsync(gameIdInt);
        }
        catch (InvalidOperationException e)
        {
            Debug.WriteLine(e.Message);
            return false;
            //disconnect with msg
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return false;
            //disconnect with msg e
        }

        try
        {
            PlayerClass player = game.PlayerList.First(playerIn => playerIn.Name == username && playerIn.Password == password);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return false;
            //need to send msg
        }
        try
        {
            ConnectionRecord connection = new ConnectionRecord(username, password, gameIdInt, game);
            context.Items.Add("connection", connection);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return false;
        }
        //context.Items.Add("methodSentCount", 0);
        return true;

    }
}
