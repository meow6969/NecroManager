using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace NecroManager;

public class Utils
{
    private string _game = "Kr1";
    private Config _config;
    private readonly string _configPath;
    private readonly string _installPath;
    private bool _startGame;
    private static Utils Instance { get; } = new Utils();
    
    public class Config
    {
        public class TheGame
        {
            public string? ExecutablePath { get; set; }
            public List<string> EnabledMods { get; set; } = [];
        }

        public TheGame Kr1 { get; init; } = new TheGame();
        public TheGame Kr2 { get; init; } = new TheGame();
        public TheGame Kr3 { get; init; } = new TheGame();
        public TheGame Kr5 { get; init; } = new TheGame();
    }

    private Utils()
    {
        _installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NecroManager");
        SetupDirectoryStructure();

        _configPath = Path.Combine(_installPath, "config.json");
        
        if (!File.Exists(_configPath))
        {
            Directory.CreateDirectory(_installPath);
            
            _config = new Config();
            string jsonString = JsonSerializer.Serialize(_config);
            File.WriteAllText(_configPath, jsonString);
        }
        else
        {
            string jsonString = File.ReadAllText(_configPath);
            
            _config = JsonSerializer.Deserialize<Config>(jsonString) ?? throw new InvalidOperationException();
        }
    }

    private static void GetLatestLuaDecompiler()
    {
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "nya/69.0");
        
        string releaseInfo = httpClient.GetStringAsync("https://api.github.com/repos/" +
                                                       "marsinator358/luajit-decompiler-v2/releases").Result;
        string? downloadLine = null;
        string? creationTime = null;
        foreach (string line in releaseInfo.Split(","))
        {
            if (line.Contains("browser_download_url") && downloadLine == null)
            {
                downloadLine = $"{line.Split(':')[^2]}:{line.Split(':')[^1]}";
            }

            if (line.Contains("created_at") && creationTime == null)
            {
                creationTime = $"{line.Split(':')[^3]}:{line.Split(':')[^2]}:{line.Split(':')[^1]}";
            }
        }
        if (downloadLine == null || creationTime == null) throw new Exception("Could not get latest Lua decompiler");

        string exeUrl = downloadLine.Replace(" ", null).Replace("\"", null);
        exeUrl = exeUrl.Substring(0, exeUrl.Length - 2);
        creationTime = creationTime.Replace("\"", null);
        string luaDecompilerSaveLocation = Path.Combine(Instance._installPath, "tools", "luajit-decompiler-v2.exe");
        
        // we have an outdated decompiler
        if (File.GetCreationTime(luaDecompilerSaveLocation) < DateTime.Parse(creationTime))
        {
            File.WriteAllBytes(luaDecompilerSaveLocation, httpClient.GetByteArrayAsync(exeUrl).Result);
        }
    }
    
    public static string GetOfficialName()
    {
        switch (Instance._game)
        {
            case "Kr1":
                return "Kingdom Rush";
            case "Kr2":
                return "Kingdom Rush: Frontiers";
            case "Kr3":
                return "Kingdom Rush: Origins";
            case "Kr5":
                return "Kingdom Rush: Alliance";
            default:
                throw new Exception($"Cannot find official name for {Instance._game}");
        }
    }

    public static string? GetValidGameCode(string game)
    {
        return game.ToLower() switch
        {
            "kr1" => "Kr1",
            "kr2" => "Kr2",
            "kr3" => "Kr3",
            "kr5" => "Kr5",
            _ => null
        };
    }

    public static void SetGame(string game)
    {
        if (GetValidGameCode(game) == null) throw new Exception($"Invalid game code {game}");
        Instance._game = game;
    }

    public static string GetGame()
    {
        return Instance._game;
    }

    public static Config GetConfig()
    {
        return Instance._config;
    }

    public static void SetConfig(Config config)
    {
        Instance._config = config;
        SaveConfig();
    }
    
    public static string ExpandTildePath(string path)
    {
        if (!path.StartsWith('~')) return path;
        string? nya = Environment.GetEnvironmentVariable("HOME");
        if (nya == null) throw new Exception("HOME variable is null, exiting");
        return $"{nya}{path.Substring(1)}";
    }

    public static void SaveConfig()
    {
        string jsonString = JsonSerializer.Serialize(Instance._config);
        File.WriteAllText(Instance._configPath, jsonString);
    }

    public static string FindProgramExecutable(string exeName)
    {
        if (IsUnix())
        {
            string? nya = Environment.GetEnvironmentVariable("PATH");
            if (nya == null) throw new Exception("PATH variable is null, exiting");
            foreach (string path in nya.Split(':'))
            {
                if (!Directory.Exists(path)) continue;  // the directory in path doesnt exist
                foreach (string path2 in Directory.GetFiles(path))
                {
                    if (!File.Exists(path2)) continue; // the thing is a path not a file
                    if (path2.EndsWith($"/{exeName}"))
                    {
                        return path2;
                    }
                }
            }
            throw new Exception("executable cannot be found in PATH");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            switch (exeName)
            {
                case "7z":
                    string path = (string) Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\7-Zip", 
                        "Path", "CANNOT_FIND_7ZIP"
                    )!;
                    if (path == "CANNOT_FIND_7ZIP") throw new Exception("Cannot find executable");
                    return $@"{path}\7z.exe";
                default:
                    throw new Exception("Cannot find executable");
            }
        }

        throw new Exception("Cannot find executable");
    }

    public static Config.TheGame GetGameConfig(string game = "null")
    {
        if (game == "null") game = Instance._game;
        
        switch (game)
        {
            case "Kr1":
                return Instance._config.Kr1;
            case "Kr2":
                return Instance._config.Kr2;
            case "Kr3":
                return Instance._config.Kr3;
            case "Kr5":
                return Instance._config.Kr5;
            default:
                throw new Exception("Utils.GetGameConfig() passed invalid game argument");
        }
    }

    public static void SetGameConfig(string? executablePath = null, List<string>? enabledMods = null)
    {
        if (!IsUnix() && executablePath != null)
        {
            // for some reason on windows it puts a / before the drive letter and it messes everything up
            if (executablePath[0] == '/' || executablePath[0] == '\\') executablePath = executablePath[1..];
        }
        
        switch (Instance._game)
        {
            case "Kr1":
                if (executablePath != null)
                {
                    Instance._config.Kr1.ExecutablePath = executablePath;
                }

                if (enabledMods != null)
                {
                    Instance._config.Kr1.EnabledMods = enabledMods;
                }

                break;
            case "Kr2":
                if (executablePath != null)
                {
                    Instance._config.Kr2.ExecutablePath = executablePath;
                }

                if (enabledMods != null)
                {
                    Instance._config.Kr2.EnabledMods = enabledMods;
                }

                break;
            case "Kr3":
                if (executablePath != null)
                {
                    Instance._config.Kr3.ExecutablePath = executablePath;
                }

                if (enabledMods != null)
                {
                    Instance._config.Kr3.EnabledMods = enabledMods;
                }

                break;
            case "Kr5":
                if (executablePath != null)
                {
                    Instance._config.Kr5.ExecutablePath = executablePath;
                }

                if (enabledMods != null)
                {
                    Instance._config.Kr5.EnabledMods = enabledMods;
                }

                break;
            default:
                throw new Exception("Utils.SetGameConfig() passed invalid game argument");
        }
        
        SaveConfig();
    }

    public static string GetInstallPath()
    {
        return Instance._installPath;
    }

    private void SetupDirectoryStructure()
    {
        Directory.CreateDirectory(Path.Combine(_installPath, "mods", "Kr1"));
        Directory.CreateDirectory(Path.Combine(_installPath, "mods", "Kr2"));
        Directory.CreateDirectory(Path.Combine(_installPath, "mods", "Kr3"));
        Directory.CreateDirectory(Path.Combine(_installPath, "mods", "Kr5"));
        Directory.CreateDirectory(Path.Combine(_installPath, "patch"));
        Directory.CreateDirectory(Path.Combine(_installPath, "tools"));
    }

    public static void OpenFileManager(string path)
    {
        if (IsUnix())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = FindProgramExecutable("xdg-open"),
                Arguments = $"\"{path}\""
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\""
            });
        }
    }
    
    public static Task DecompileGameAsync(string decompileLocation)
    {
        return Task.Run(() => DecompileGame(decompileLocation));
    }

    public static bool IsDirectoryEmpty(string dir)
    {
        if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
        {
            return true;
        }

        return false;
    }

    public static void DecompileGame(string decompileLocation, bool force=false)
    {
        GetLatestLuaDecompiler();
        
        decompileLocation = ExpandTildePath(decompileLocation);
        string luaDecompiler = Path.Combine(Instance._installPath, "tools", "luajit-decompiler-v2.exe");
        string extractLocation = Path.Combine(Path.GetTempPath(), "kingdom");

        if (File.Exists(decompileLocation)) throw new Exception("Decompile location is a file!");
    
        Directory.CreateDirectory(decompileLocation);
    
        // if decompile location isn't empty
        if (!IsDirectoryEmpty(decompileLocation) && !force) throw new Exception("Decompile location is not empty!");
        
        if (Directory.Exists(extractLocation)) Directory.Delete(extractLocation, true);
        Directory.CreateDirectory(extractLocation);
        
        ProcessStartInfo pro = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = FindProgramExecutable("7z"),
            Arguments = $"-y x \"{GetGameConfig().ExecutablePath}\" -o\"{extractLocation}\""
        };
        Process x = Process.Start(pro) ?? throw new InvalidOperationException();
        x.WaitForExit();

        string forceOverwrite = "";
        if (force)
        {
            forceOverwrite = "-f";
        }
        
        ProcessStartInfo pro2;
        if (IsUnix())
        {
            pro2 = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = FindProgramExecutable("wine"),
                Arguments = $"{luaDecompiler} \"{extractLocation}\" -o \"{decompileLocation}\" -e lua -s {forceOverwrite}"
            };
        }
        else
        {
            pro2 = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = luaDecompiler,
                Arguments = $"\"{extractLocation}\" -o \"{decompileLocation}\" -e lua -s {forceOverwrite}"
            };
        }
        
        Process x2 = Process.Start(pro2) ?? throw new InvalidOperationException();
        x2.WaitForExit();
    }

    public static void SetReadyToStart()
    {
        Instance._startGame = true;
    }
    
    public static void StartGame()
    {
        if (!Instance._startGame) return;
        
        if (IsUnix())
        {
            ProcessStartInfo pro = new ProcessStartInfo
            {
                FileName = FindProgramExecutable("wine"),
                Arguments = $"\"{GetModdedExePath()}\""
            };
            Process x = Process.Start(pro) ?? throw new InvalidOperationException();
            x.WaitForExit();
        }
        else
        {
            ProcessStartInfo pro = new ProcessStartInfo
            {
                FileName = GetModdedExePath()
            };
            Process x = Process.Start(pro) ?? throw new InvalidOperationException();
            x.WaitForExit();
        }
    }

    private static string GetModdedExePath()
    {
        string executablePath = GetGameConfig().ExecutablePath ?? throw new InvalidOperationException();

        return $"{executablePath.Substring(0, executablePath.Length - 4)}_mod.exe";
    }

    public static Task PatchExecutableAsync()
    {
        return Task.Run(PatchExecutable);
    }
    
    public static void PatchExecutable()
    {
        Mods.PreparePatchDirectory();
        
        string executablePath = GetGameConfig().ExecutablePath ?? throw new InvalidOperationException();
        string patchPath = Path.Combine(Instance._installPath, "patch");
        
        string moddedExe = GetModdedExePath();
        if (File.Exists(moddedExe))
        {
            File.Delete(moddedExe);
            // Console.WriteLine("deleted moddedExe");
        }
        File.Copy(executablePath, moddedExe);
        
        // Console.WriteLine(patchPath);
        // Console.WriteLine(executablePath);
        // Console.WriteLine(moddedExe);
        // Console.WriteLine($"CWD=\"{patchPath}\" 7z u \"{moddedExe}\" .");

        string oldWorkingDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(patchPath);
        
        ProcessStartInfo pro = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = FindProgramExecutable("7z"),
            Arguments = $"-y -bd u \"{moddedExe}\" ."
        };
        Process x = Process.Start(pro) ?? throw new InvalidOperationException();
        x.WaitForExit();
        
        Directory.SetCurrentDirectory(oldWorkingDirectory);
    }

    public static bool IsUnix()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    }
    
    public static void CopyAll(DirectoryInfo source, DirectoryInfo target) // https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo
    {
        if (source.FullName.ToLower() == target.FullName.ToLower())
        {
            return;
        }

        // Check if the target directory exists, if not, create it.
        if (Directory.Exists(target.FullName) == false)
        {
            Directory.CreateDirectory(target.FullName);
        }

        // Copy each file into it's new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            // Console.WriteLine(@$"Copying {target.FullName}{Path.DirectorySeparatorChar}{fi.Name}");
            fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }
}
