using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NecroManager;

public static class CommandInterface
{
    public static void Commands(string[] args)
    {
        if (args[0] == "--help" || args[0] == "-h" || args[0] == "h")
        {
            HelpCommand();
        }
        
        if (args.Length < 2)
        {
            Console.WriteLine("Not enough arguments passed!\n\n");
            HelpCommand();
            return;
        }

        string? temp = Utils.GetValidGameCode(args[0]);
        
        if (temp == null)
        {
            Console.WriteLine("Invalid game code\n\n");
            HelpCommand();
        }

        args[0] = temp!;
        
        switch (args[1])
        {
            case "d":
                DecompileCommand(args);
                break;
            case "p":
                PlayCommand(args);
                break;
            case "s":
                SetDirectoryCommand(args);
                break;
            default:
                Console.WriteLine("Invalid command passed\n\n");
                HelpCommand();
                break;
        }
    }

    private static void HelpCommand()
    {
        Console.WriteLine(
            $"NecroManager\n" +
            $"\n" +
            $"usage        : NecroManager <game code> <command> <arguments>\n" +
            $"               pass no arguments to enter the GUI\n" +
            $"\n" +
            $"game codes:\n" +
            $"  kr1        : Kingdom Rush\n" +
            $"  kr2        : Kingdom Rush: Frontiers\n" +
            $"  kr3        : Kingdom Rush: Origins\n" +
            $"  kr5        : Kingdom Rush: Alliance\n" +
            $"\n" +
            $"commands:\n" +
            $"  d <path>   : decompile the game, path should be output folder for decompilation\n" +
            $"  p          : start the modded game, by default uses mods defined in config.json\n" +
            $"  s <path>   : set the path to the game folder\n" +
            $"  h          : show this dialogue\n" +
            $"\n" +
            $"arguments:\n" +
            $"  -m <mods>  : comma separated list of mods to start the game\n" +
            $"               NOTE: must be used with command p\n" +
            $"               NOTE: each mod is named by its parent folder name, so mod1/mod.json would be named mod1.\n" +
            $"  -n         : prevents the mods selected by -m from being saved in the config file\n" +
            $"               NOTE: must be used with command p and argument -m\n" +
            $"  -f         : forces decompile to non-empty folders\n" +
            $"               NOTE: must be used with command d\n" +
            $"               NOTE: overwrites existing files\n" +
            $"  -h, --help : show this dialogue\n" +
            $"\n" +
            $"examples:\n" +
            $"  NecroManager kr1 p -m mod1,mod2,mod3 -n\n" +
            $"  NecroManager kr5 d ~/Desktop/kingdom_rush_alliance_decompile" 
        );
        Environment.Exit(0);
    }

    private static void DecompileCommand(string[] args)
    {
        if (args.Length == 2)
        {
            Console.WriteLine("Not enough arguments passed to decompile!\n\n");
            HelpCommand();
        }
        
        CheckForGameInitialization(args[0]);
        string path = GetPathFromArgs(args, flag:"-f");

        bool force = args.Contains("-f");
        try
        {
            Utils.DecompileGame(path, force);
        }
        catch (Exception e)
        {
            if (e.Message == "Decompile location is not empty!")
            {
                Console.WriteLine("Decompile directory is not empty. use -f to force decompile to this folder\n" +
                                  "WARNING: will overwrite existing files!");
                Environment.Exit(0);
            }

            throw;
        }
    }

    private static void PlayCommand(string[] args)
    {
        CheckForGameInitialization(args[0]);
        bool saveToConfig = true;
        bool collectingMods = false;
        bool setMods = false;
        string? modsListString = null;
        List<string> modsList = [];
        foreach (string word in args)
        {
            if (word == "-n")
            {
                saveToConfig = false;
                collectingMods = false;
                continue;
            }

            if (word == "-m")
            {
                collectingMods = true;
                continue;
            }

            if (collectingMods)
            {
                modsListString += $"{word} ";
                setMods = true;
            }
        }

        if (setMods)
        {
            if (modsListString != null)
            {
                modsListString = modsListString[..^1];
                foreach (string mod in modsListString.Split(','))
                {
                    modsList.Add(mod.Replace('\\', '/'));
                }
            }

            List<Mods.Mod> realModsList = [];
        
            foreach (string mod in modsList)
            {
                try
                {
                    realModsList.Add(mod[0] != '/' ? Mods.GetModByPath($"/{mod}") : Mods.GetModByPath(mod));
                }
                catch (Exception e)
                {
                    if (e.Message.StartsWith("Could not find mod from path"))
                    {
                        Console.WriteLine($"Invalid mod path {mod}\n" +
                                          $"Are you sure you entered the mods parent folder name?");
                        Environment.Exit(0);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            foreach (Mods.Mod mod in Mods.GetAllGameMods())
            {
                mod.Enabled = false;
            }

            foreach (Mods.Mod mod in realModsList)
            {
                if (Mods.IsConflicting(mod))
                {
                    Console.WriteLine($"One or more mods are conflicting!\n" +
                                      $"Conflicting mod: {mod.Name}");
                    Environment.Exit(0);
                }
                mod.Enabled = true;
            }

            if (saveToConfig && realModsList.Count > 0)
            {
                Mods.SaveGameModsToConfig();
            }
        }
        
        Console.WriteLine("Patching game...");
        Utils.PatchExecutable();
        Console.WriteLine("Starting!");
        Utils.SetReadyToStart();
        Utils.StartGame();
    }

    private static void SetDirectoryCommand(string[] args)
    {
        string game = args[0];
        string path = GetPathFromArgs(args);
        
        Utils.SetGame(game);

        if (IsGamePathValid(path))
        {
            string validExeName = Utils.GetOfficialName().Replace(":", null) + ".exe";
            Utils.SetGameConfig(executablePath:Path.Combine(path, validExeName));
            Console.WriteLine("Game path set successfully.");
        }
        else
        {
            Console.WriteLine("Invalid path\n\n");
            HelpCommand();
        }
    }

    private static string GetPathFromArgs(string[] args, int skip = 2, string? flag = null)
    {
        string path = "";
        foreach (string word in args)
        {
            if (skip > 0)
            {
                skip--;
                continue;
            }

            if (word == flag)
            {
                continue;
            }

            path += $"{word} ";
        }

        path = path[..^1]; // remove leading space
        if (path[0] == '\"') path = path[1..];
        if (path[^1] == '\"') path = path[..^1];

        return path;
    }

    private static void CheckForGameInitialization(string game)
    {
        Utils.SetGame(game);
        
        if (File.Exists(Utils.GetGameConfig(Utils.GetGame()).ExecutablePath)) return;
        // game is not initialized yet
        Console.WriteLine($"Enter directory path to {Utils.GetOfficialName()}");
        string? path = Console.ReadLine();
        if (path == null)
        {
            Console.WriteLine("Invalid path\n\n");
            HelpCommand();
        }

        if (IsGamePathValid(path!))
        {
            string validExeName = Utils.GetOfficialName().Replace(":", null) + ".exe";
            Utils.SetGameConfig(executablePath:Path.Combine(path!, validExeName));
            Console.WriteLine("Game path set successfully.");
        }
        else
        {
            Console.WriteLine("Invalid path\n\n");
            HelpCommand();
        }
    }

    private static bool IsGamePathValid(string path)
    {
        // remove quotes around path if user put them
        if (path[0] == '\"') path = path[1..];
        if (path[^1] == '\"') path = path[..^1];
            
        path = Utils.ExpandTildePath(path);
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            return false;
        }

        string validExeName = Utils.GetOfficialName().Replace(":", null) + ".exe";
        // if the user gave us path to the game exe
        if (path.Split(Path.DirectorySeparatorChar)[^1] == validExeName)
        {
            return true;
        }
        
        // if user gave us path to the game folder
        string validExePath = Path.Combine(path, validExeName);
            
        return File.Exists(validExePath);
        // the directory path the user gave us is invalid
    }
}