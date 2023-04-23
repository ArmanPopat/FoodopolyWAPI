namespace FoodopolyWAPI.Models;

//not used right now, using record instead
public class ConnectionModel
{
    public string Username { get; set; }
    //public string Password { get; set; } //unsure if needed here, maybe if connected after lack of internet on that page
    public string ConnectionId { get; set; }

    public ConnectionModel(string username, string connectionId) 
    {
        Username = username;
        //Password = password;
        ConnectionId = connectionId;
    }
}
