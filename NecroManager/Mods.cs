using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.IO.Compression;
using System.Linq;

namespace NecroManager;

public class Mods
{
    private List<Mod> _allMods;
    private List<Mod> _erroredMods;

    private static Mods Instance { get; } = new Mods();

    private Mods()
    {
        // TODO: add function to rescan mods folder
        // TODO: add ability to scan for mods without a mod.json (for custom mods and stuff)
        List<List<Mod>> meow = ScanForMods();
        _allMods = meow[0];
        _erroredMods = meow[1];
    }

    public static List<Mod> GetErroredMods()
    {
        return Instance._erroredMods;
    }
    
    // we do this because we cannot access Instance in the constructor
    public static void ScanForModsPublic()
    {
        List<List<Mod>> meow = ScanForMods();
        Instance._allMods = meow[0];
        Instance._erroredMods = meow[1];
    }

    private static List<List<Mod>> ScanForMods()
    {
        List<Mod> allMods = [];
        List<Mod> erroredMods = [];
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
                try
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
                    mod.RelativePath = modDir[Utils.GetInstallPath().Length..].Replace('\\', '/');
                    if (Utils.GetGameConfig(gameCode).EnabledMods.Contains(mod.Path)) mod.Enabled = true;
                    else if (Utils.GetGameConfig(gameCode).EnabledMods.Contains(mod.RelativePath)) mod.Enabled = true;
                    allMods.Add(mod);
                }
                catch (Exception e)
                {
                    erroredMods.Add(new Mod
                    {
                        Path = modDir,
                        Name = "",
                        Version = "",
                        Author = "",
                        Description = "",
                        Error = e
                    });
                }
            }
        }

        return [allMods, erroredMods];
    }

    public static void PreparePatchDirectory()
    {
        string patchPath = Path.Combine(Utils.GetInstallPath(), "patch");
        
        if (Directory.Exists(patchPath)) Directory.Delete(patchPath, true);
        Directory.CreateDirectory(patchPath);
        PatchModDisplay(patchPath);
        
        foreach (Mod mod in GetEnabledGameMods())
        {
            Utils.CopyAll(new DirectoryInfo(mod.Path), new DirectoryInfo(patchPath));
        }
    }

    private static void PatchModDisplay(string patchPath)
    {
        if (GetTwiceUsedFiles().Contains("/all-desktop/screen_settings.lua")) return;
        if (GetOnceUsedFiles().Contains("/all-desktop/screen_settings.lua"))
        {
            
        }
        
        Directory.CreateDirectory(Path.Combine(patchPath, "all-desktop"));
        string modsLua = "MODS = \"";
        foreach (Mod mod in GetEnabledGameMods())
        {
            modsLua += $"{mod.Name}\\n";
        }

        modsLua += "Mod Display\"";
        File.Copy(
            Path.Combine(
                "./ModDisplay", $"{Utils.GetGame()}_screen_settings.lua"), 
            Path.Combine(
                patchPath, "all-desktop", "screen_settings.lua")
            );
        File.WriteAllText(Path.Combine(patchPath, "all-desktop", "mods.lua"), modsLua);
    }

    private static void MergeModdedFiles(string relativeFilePath)
    {
        
    }
    
    private static List<string> GetOnceUsedFiles()
    {
        List<string> onceUsedFiles = [];
        
        foreach (Mod mod in GetEnabledGameMods())
        {
            foreach (string file in mod.Files)
            {
                onceUsedFiles.Add(file);
            }
        }

        return onceUsedFiles;
    }

    private static List<string> GetTwiceUsedFiles()
    {
        List<string> onceUsedFiles = [];
        List<string> twiceUsedFiles = [];
        
        foreach (Mod mod in GetEnabledGameMods())
        {
            foreach (string file in mod.Files)
            {
                if (onceUsedFiles.Contains(file)) twiceUsedFiles.Add(file);
                else onceUsedFiles.Add(file);
            }
        }

        return twiceUsedFiles;
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
        
        List<string> usedFiles = GetTwiceUsedFiles();
        
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
                enabledMods += $"{mod.RelativePath}, ";
            }
        }

        if (enabledMods.Length == 0) return;
        Console.WriteLine(enabledMods.Substring(0, enabledMods.Length - 2));
    }
    
    public static void ImportModFromZip(string zipPath)
    {
        if (!Utils.IsUnix())
        {
            // for some reason on windows it puts a / before the drive letter and it messes everything up
            if (zipPath[0] == '/' || zipPath[0] == '\\') zipPath = zipPath[1..];
        }

        string extractPath = Path.Combine(Path.GetTempPath(), "kingdomrush_zipmod");
        if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
        ZipFile.ExtractToDirectory(zipPath, extractPath);
        string modRootPath;
        if (File.Exists(Path.Combine(extractPath, "mod.json")))
        {
            modRootPath = extractPath;
        }
        else
        {
            if (Directory.GetDirectories(extractPath).Length == 0) throw new Exception("Could not find mod.json");
            string subPath = Directory.GetDirectories(extractPath)[0];
            if (File.Exists(Path.Combine(subPath, "mod.json"))) modRootPath = subPath;
            else throw new Exception("Could not find mod.json");
        }

        Mod mod = ReadModJson(Path.Combine(modRootPath, "mod.json"));
        if (mod.Game == "") throw new Exception("mod.json does not have Game attribute");
        string allModsPath = Path.Combine(Utils.GetInstallPath(), "mods", mod.Game);
        string modRootName = Path.GetFileNameWithoutExtension(zipPath);
        string installedModPath = Path.Combine(allModsPath, modRootName);
        
        if (!Directory.Exists(allModsPath)) throw new Exception("Invalid Game attribute in mod.json");
        if (Directory.Exists(installedModPath))
        {
            Directory.Delete(installedModPath, true);
            Directory.CreateDirectory(installedModPath);
        }
        // Console.WriteLine(allModsPath);
        // Console.WriteLine(modRootPath);
        // Console.WriteLine(installedModPath);
        Utils.CopyAll(new DirectoryInfo(modRootPath), new DirectoryInfo(installedModPath));
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
        public Exception? Error { get; set; }
    } 
}