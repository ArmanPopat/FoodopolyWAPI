//using FoodopolySignalR.Services;
using FoodopolyWAPI.Authentication;
using FoodopolyWAPI.Authorisation;
using FoodopolyWAPI.Hubs;
using FoodopolyWAPI.Records;
using FoodopolyWAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

//Custom services here

//Based of StackOverflow Answer
//var runningGameService = new RunningGameService();
//var rgService = new RunningGameService();
//builder.Services.AddSingleton<RunningGameService>(rgService);
builder.Services.AddSingleton<RunningGameService>();

builder.Services.AddSingleton<IAuthorizationHandler, GameGroupsAuthorisationConnectRequirementHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, GameMethodAuthorisationRequirementHandler>();
//builder.Services.AddSingleton<IAuthorizationHandler, BypassRequirementHandler>();

builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<APIProcessor>();
builder.Services.AddSingleton<GroupsService>();

//builder.Services.AddAuthentication();


//Currently all done in authentication by adding the items, uses context.items, should instead put to claims in authetication which is easy to do by editing this scheme handler, need to figure out 
builder.Services.AddAuthentication()
    .AddScheme<UserAuthenticationSchemeOptions, UserAuthenticationSchemeHandler>(
        "User",
        opts => { }
    );
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GameGroupsAuthorisationConnect", policy =>
    {
        policy.Requirements.Add(new GameGroupsAuthorisationConnectRequirement());
        //policy.AddAuthenticationSchemes("User");
    });
    options.AddPolicy("GameMethodAuthorisation", policy =>
    {
        policy.Requirements.Add(new GameMethodAuthorisationRequirement());
        //policy.AddAuthenticationSchemes("User");
    });
    //options.AddPolicy("GameGroupsTestAuthorizationRequirement", policy =>
    //{
    //    policy.Requirements.Add(new GameGroupsTestAuthorizationRequirement(rgService));
    //});
});


builder.Services.AddSignalR()
    .AddNewtonsoftJsonProtocol();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseResponseCompression(); //Was in the msoft tutorial

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//app.MapBlazorHub(); ///signar, might be needed for, unsure, likely not
app.MapHub<GameHub>("/connected/game");//.RequireAuthorization();

app.UseHttpsRedirection();

//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/api/weatherforecast", () =>
//{
//    var forecast = Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateTime.Now.AddDays(index),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast");

//refrenced service in part of variables of lambda
app.MapPost("/api/game", async (APIProcessor aPIProcessor, CreateGameRecord gameRecord) =>
{
    
    var successAndMessageAndGameIdOrStatusCode = await aPIProcessor.POSTCreatGameProcessor(gameRecord);
    if (successAndMessageAndGameIdOrStatusCode.success == true)
    {
        return Results.Created($"/api/game/{successAndMessageAndGameIdOrStatusCode.gameIdOrStatusCode}", null);//decide if I want to pass the object here, maybe after first signal r connect???
    }
    return Results.Problem(successAndMessageAndGameIdOrStatusCode.message, null, successAndMessageAndGameIdOrStatusCode.gameIdOrStatusCode, "Problem");
    
})
.WithName("PostCreateGame");


//Patch to rejoin or join game for first time
app.MapPatch("/api/game/{gameId}", async (int gameId, APIProcessor aPIProcessor, JoinGameRecord record) =>
{
    var successAndMessageAndCode = await aPIProcessor.PATCHJoinGameProcessor(record);
    if (successAndMessageAndCode.success == true)
    {
        return Results.Ok();
    }
    return Results.Problem(successAndMessageAndCode.errorMessage, null, successAndMessageAndCode.statusCode, "Problem!");
})
.WithName("PatchJoinGame");

//app.UseAuthentication();

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}