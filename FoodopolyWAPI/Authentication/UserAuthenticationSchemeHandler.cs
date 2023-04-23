using FoodopolyWAPI.Records;
using FoodopolyWAPI.Services;
using GameClasses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using PlayerClasses;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Encodings.Web;




//Will use this later
namespace FoodopolyWAPI.Authentication;

public class UserAuthenticationSchemeHandler:AuthenticationHandler<UserAuthenticationSchemeOptions>
{
    private RunningGameService _runningGameService;
    public UserAuthenticationSchemeHandler(
        IOptionsMonitor<UserAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        RunningGameService runningGameService) : base(options, logger, encoder, clock)
        
    {
        _runningGameService = runningGameService;
    }
    private async Task<(bool Success, ClaimsPrincipal? Principal)> IsUserAllowedAndAddData( HttpContext context)
    {
        //assigned values to satisfy error stuff
        string username = "1000";
        string password = "password";
        string gameId = "1000";
        int gameIdInt = 1000;

        //making sure no empty or null or incorrect character
        bool worked = await Task<bool>.Run(() =>
        {

            var httpContext = context;
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
            return (false,null);
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
            return (false, null);
            //disconnect with msg
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return (false, null);
            //disconnect with msg e
        }
        ClaimsPrincipal principal;
        try
        {
            PlayerClass player = game.PlayerList.First(playerIn => playerIn.Name == username && playerIn.Password == password);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return (false, null);
            //need to send msg
        }
        try
        {
            //ConnectionRecord connection = new ConnectionRecord(username, password, gameIdInt, game);
            //context.Items.Add("connection", connection);
            var claims = new List<Claim>
            {
            new Claim("username", username, ClaimValueTypes.String),
            new Claim("password", password, ClaimValueTypes.String),
            new Claim("gameId", gameId, ClaimValueTypes.Integer)
            };
            var userGameidentity = new ClaimsIdentity(claims, "Connection");
            //var claim = new Claim(ClaimTypes.UserData, connection, ClaimValueTypes.);
            principal = new ClaimsPrincipal(userGameidentity);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return (false, null);
        }
        //context.Items.Add("methodSentCount", 0);
        return (true, principal);

    }
    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Read the token from request headers/cookies
        // Check that it's a valid session, depending on your implementation
        var successAndPrincipal = await IsUserAllowedAndAddData(Context);
        if (successAndPrincipal.Success)
        {
            // If the session is valid, return success:
            return await Task<AuthenticateResult>.Run(() =>
            {

                AuthenticationTicket ticket;
                AuthenticateResult result;
                try
                {
                    //var user = new ClaimsIdentity("User");
                    ////user.AddClaim();
                    //var principal = new ClaimsPrincipal(new ClaimsIdentity("User"));

                    ticket = new AuthenticationTicket(successAndPrincipal.Principal, Scheme.Name); // no possible null refrence here
                    result = AuthenticateResult.Success(ticket);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + "\n Error In Authentication");
                    result = AuthenticateResult.Fail("Authentication failed");
                }
                return result;
            });
        }
        else
        {
            return AuthenticateResult.Fail("Authentication failed");
        }
        
        

        // If the token is missing or the session is invalid, return failure:
        // return AuthenticateResult.Fail("Authentication failed");
    }
}
