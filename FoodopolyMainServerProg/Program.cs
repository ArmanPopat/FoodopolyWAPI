using GameClasses;
namespace FoodopolyMainServerProg;

public class Program
{
    public Dictionary<int, GameClass> GameDict { get; set; }
    public Program() 
    {
        GameDict = new Dictionary<int, GameClass>();
    }
    static async Task Main(string[] args)
    {
        GameDict= new Dictionary<int, GameClass>();

        Console.WriteLine("Hello, World!");
    }
}