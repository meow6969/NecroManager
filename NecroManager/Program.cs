using System;
using System.IO;

namespace NecroManager;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            GUI.UserInterface(args);
            Utils.StartGame();
        }
        else
        {
            CommandInterface.Commands(args);
        }
    }
}