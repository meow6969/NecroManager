using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;

namespace NecroManager;

public class Mods
{
    private List<Mod> _allMods;

    private static Mods Instance { get; } = new Mods();

    private Mods()
    {
        // TODO: add function to rescan mods folder
        // TODO: add ability to scan for mods without a mod.json (for custom mods and stuff)

        _allMods = ScanForMods();
    }
    
    // we do this because we cannot access Instance in the constructor
    public static void ScanForModsPublic()
    {
        Instance._allMods = ScanForMods();
    }

    private static List<Mod> ScanForMods()
    {
        List<Mod> _allMods = [];
        var modsPath = Path.Combine(Utils.GetInstallPath(), "mods");

        foreach (string gameModsDir in Directory.GetDirectories(modsPath))
        {
            string gameCode = gameModsDir.Split(Path.DirectorySeparatorChar)[^1];
            bool validFolder = true;
            switch (gameCode)
            {
                case "Kr1":
                    break;
                case "Kr2":
                    break;
                case "Kr3":
                    break;
                case "Kr5":
                    break;
                default:
                    validFolder = false;
                    break;
            }
            if (!validFolder) continue;
            
            foreach (string modDir in Directory.GetDirectories(gameModsDir))
            {
                string modJsonPath = Path.Combine(modDir, "mod.json");
                if (!File.Exists(modJsonPath)) continue;
            
                Mod mod = ReadModJson(modJsonPath);

                List<string> files = [];
                foreach (string file in Directory.GetFiles(modDir, "*", SearchOption.AllDirectories))
                {
                    if (File.Exists(file) && file != modJsonPath) files.Add(file[modDir.Length..]);
                }

                mod.Files = files;
                mod.Game = gameCode;
                mod.Path = modDir;
                mod.RelativePath = modDir[gameModsDir.Length..].Replace('\\', '/');
                if (Utils.GetGameConfig(gameCode).EnabledMods.Contains(mod.Path)) mod.Enabled = true;
                else if (Utils.GetGameConfig(gameCode).EnabledMods.Contains(mod.RelativePath)) mod.Enabled = true;
                _allMods.Add(mod);
            }
        }

        return _allMods;
    }

    public static void PreparePatchDirectory()
    {
        string patchPath = Path.Combine(Utils.GetInstallPath(), "patch");
        
        if (Directory.Exists(patchPath)) Directory.Delete(patchPath, true);
        Directory.CreateDirectory(patchPath);
        
        foreach (Mod mod in GetEnabledGameMods())
        {
            Utils.CopyAll(new DirectoryInfo(mod.Path), new DirectoryInfo(patchPath));
        }
    }

    private static List<string> GetUsedFiles()
    {
        List<string> usedFiles = [];
        
        foreach (Mod mod in GetEnabledMods())
        {
            foreach (string file in mod.Files)
            {
                usedFiles.Add(file);
            }
        }

        return usedFiles;
    }

    private static List<Mod> GetEnabledGameMods()
    {
        List<Mod> mods = [];
        
        foreach (Mod mod in Instance._allMods)
        {
            if (mod.Enabled && mod.Game == Utils.GetGame())
            {
                mods.Add(mod);
            }
        }

        return mods;
    }

    public static void SaveGameModsToConfig()
    {
        List<string> mods = [];
        foreach (Mod mod in GetEnabledMods())
        {
            mods.Add(mod.RelativePath);
        }

        Utils.SetGameConfig(enabledMods:mods);
    }

    private static List<Mod> GetEnabledMods()
    {
        List<Mod> enabledMods = [];
        
        foreach (Mod mod in Instance._allMods)
        {
            if (mod.Enabled) enabledMods.Add(mod);
        }

        return enabledMods;
    }

    public static List<Mod> GetAllMods()
    {
        return Instance._allMods;
    }

    public static List<Mod> GetAllGameMods()
    {
        List<Mod> allGameMods = [];
        
        foreach (Mod mod in Instance._allMods)
        {
            if (mod.Game == Utils.GetGame())
            {
                allGameMods.Add(mod);
            }
        }

        return allGameMods;
    }

    public static bool IsConflicting(Mod mod)
    {
        if (mod.Enabled) return false;
        
        List<string> usedFiles = GetUsedFiles();
        
        foreach (string file in mod.Files)
        {
            if (usedFiles.Contains(file)) return true;
        }

        return false;
    }

    public static Mod GetModByPath(string path)
    {
        foreach (Mod mod in Instance._allMods)
        {
            if (mod.Path == path || mod.RelativePath == path)
            {
                return mod;
            }
        }

        throw new Exception($"Could not find mod from path {path}");
    }

    public static void PrintModsToConsole()
    {
        string enabledMods = "";
        
        foreach (Mod mod in Instance._allMods)
        {
            if (mod.Enabled && mod.Game == Utils.GetGame())
            {
                enabledMods += $"{mod.Name}, ";
            }
        }

        if (enabledMods.Length == 0) return;
        Console.WriteLine(enabledMods.Substring(0, enabledMods.Length - 2));
    }

    private static Mod ReadModJson(string modJson)
    {
        string json = File.ReadAllText(modJson);
        Mod source = JsonSerializer.Deserialize<Mod>(json) ?? throw new InvalidOperationException();
        return source;
    }

    public class Mod
    {
        public string Path { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public required string Name { get; init; }
        public required string Version { get; init; }
        public required string Description { get; init; }
        public required string Author { get; init; }
        public List<string> Files { get; set; } = [];
        public bool Enabled { get; set; }
        public string Game { get; set; } = "";
    } 
}