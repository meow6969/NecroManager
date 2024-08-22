namespace NecroManager;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            GUI.UserInterface(args);
        }
        else
        {
            CommandInterface.Commands(args);
        }
        
        // this only starts the game if Utils.Instance._startGame is set to true
        Utils.StartGame();
    }
}