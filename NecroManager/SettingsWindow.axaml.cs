using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace NecroManager;

public partial class SettingsWindow : Window
{
    private int _subWindowsOpened;
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private List<int> _weedTrack;
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private MainWindow _mainWindow;
    
    // the parameter here causes the SettingsWindow.axaml build warning even though this code runs fine
    public SettingsWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        _weedTrack = [0, 0, 0];
        _mainWindow = mainWindow;

        Title = $"{Utils.GetOfficialName()} Settings";
        Foreground = Brushes.White;
        Topmost = true;
        Width = 300;
        Height = 500;
        Closing += (_, e) =>
        {
            if (_subWindowsOpened > 0) e.Cancel = true;
        };
        
        AddContent();
    }

    private void AddContent()
    {
        MainPanel.Children.Clear();
        
        MainPanel.Children.Add(new TextBlock
        {
            Text = "Settings",
            Foreground = Brushes.White,
            FontSize = 30,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        
        MainPanel.Children.Add(new TextBlock
        {
            Text = Utils.GetOfficialName(), 
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        
        Button gameDirectoryButton = new Button
        {
            Content = "Select Game Executable", 
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        gameDirectoryButton.Click += GameDirectoryButton;
        MainPanel.Children.Add(gameDirectoryButton);

        if (Utils.GetGameConfig(Utils.GetGame()).ExecutablePath == null)
        {
            string errorText = Utils.GetGameConfig(Utils.GetGame()).ExecutablePath == null 
                ? "Game path not set!" : "Game executable cannot be found!";
            MainPanel.Children.Add(new TextBlock
            {
                Text = errorText, 
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.Pink,
                Background = Brushes.DarkRed,
            });
            return;
        }
        
        Button decompileButton = new Button
        {
            Content = "Decompile game scripts", 
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        decompileButton.Click += DecompileButton;
        MainPanel.Children.Add(decompileButton);
        
        MainPanel.Children.Add(new Separator
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 5,
            Margin = new Thickness(0, 5)
        });
        
        Button modDirectoryButton = new Button
        {
            Content = "Open mods folder", 
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        modDirectoryButton.Click += ModDirectoryButton;
        MainPanel.Children.Add(modDirectoryButton);
        
        Button scanModsButton = new Button
        {
            Content = "Scan mods folder", 
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        scanModsButton.Click += ScanModsButton;
        MainPanel.Children.Add(scanModsButton);
        
        Button importModButton = new Button
        {
            Content = "Import zip mod", 
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        importModButton.Click += ImportModButton;
        MainPanel.Children.Add(importModButton);

        MainPanel.Children.Add(new TextBlock
        {
            Background = new SolidColorBrush(Color.FromArgb(0xff, 0x20, 0x20, 0x20)),
            Text = "Grayed-out mods are incompatible",
            FontSize = 16,
            Foreground = Brushes.Red,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        MainPanel.Children.Add(new TextBlock
        {
            Background = new SolidColorBrush(Color.FromArgb(0xff, 0x20, 0x20, 0x20)),
            Text = "with selected mods.",
            FontSize = 16,
            Foreground = Brushes.Red,
          
            HorizontalAlignment = HorizontalAlignment.Center
        });
        
        foreach (Mods.Mod mod in Mods.GetAllGameMods())
        {
            if (Mods.IsConflicting(mod))
            {
                MainPanel.Children.Add(new TextBlock()
                {
                    Margin = new Thickness(32, 6.75),
                    Text = $"{mod.Name}",
                    // Foreground = Brushes.LightGray,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 15,
                    Background = Brushes.DimGray,
                    Foreground = Brushes.White
                });
                continue;
            }

            CheckBox checkBox = new CheckBox
            {
                Name = mod.RelativePath,
                Margin = new Thickness(5, 0),
                IsChecked = mod.Enabled,
                Content = mod.Name,
                FontSize = 15
            };
            checkBox.IsCheckedChanged += ModCheckChecked;
            MainPanel.Children.Add(checkBox);
        }
        
        HandleErroredMods();
    }

    private async void DecompileButton(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0)
        {
            _weedTrack[1]++;
            CheckWeedMode();
            return;
        }
        _subWindowsOpened++;
        
        var dirs = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Game Decompile Directory",
            AllowMultiple = false
        });

        if (dirs.Count == 0)
        {
            SpawnErrorWindow("No directory selected.");
            return;
        }
        string decompilePath = dirs[0].Path.LocalPath;
        
        
        if (!Utils.IsDirectoryEmpty(decompilePath))
        {
            SpawnErrorWindow("Directory not empty.");
            return;
        }

        Button decompileButton = (Button)source!;
        decompileButton.Content = "Decompiling...";
        decompileButton.Background = Brushes.Yellow;
        decompileButton.Foreground = Brushes.Black;
        await Utils.DecompileGameAsync(decompilePath);
        _subWindowsOpened--;
        Utils.OpenFileManager(decompilePath);
        AddContent();
    }

    private void CheckWeedMode()
    {
        if (_weedTrack[0] == 4 && _weedTrack[1] == 2 && _weedTrack[2] == 0)
        {
            _mainWindow.WeedMode();
        }
    }

    private void ModDirectoryButton(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0)
        {
            _weedTrack[2]++;
            CheckWeedMode();
            return;
        }

        Utils.OpenFileManager(Path.Combine(Utils.GetInstallPath(), "mods"));
    }
    
    private void ScanModsButton(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0) return;

        Mods.ScanForModsPublic();
        AddContent();
    }

    private void HandleErroredMods()
    {
        List<Mods.Mod> erroredMods = Mods.GetErroredMods();
        string errorMessage = "The mods at the following paths have errors!\n";

        if (erroredMods.Count == 0) return;
        foreach (Mods.Mod mod in erroredMods)
        {
            errorMessage += $"{mod.Path}\n" +
                            $"{mod.Error!.Message}\n" +
                            $"\n";
        }
        
        SpawnErrorWindow(errorMessage);
    }
    
    private async void GameDirectoryButton(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0)
        {
            _weedTrack[0]++;
            CheckWeedMode();
            return;
        }

        _subWindowsOpened++;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"Select {Utils.GetOfficialName()} Executable",
            AllowMultiple = false
        });
        _subWindowsOpened--;
        
        if (files.Count == 0) return;

        string executableName = Utils.GetOfficialName().Replace(":", string.Empty) + ".exe";
        if (files[0].Name != executableName)
        {
            SpawnErrorWindow($"Invalid executable name: {files[0].Name}\n" +
                             $"Make sure to select \"{executableName}\"");
            return;
        }
        
        // Console.WriteLine($"Valid game name: {files[0].Name}");

        var meow = Utils.GetConfig();
        
        Utils.SetGameConfig(executablePath:files[0].Path.ToString()[7..]);
        
        Utils.SetConfig(meow);
        AddContent();
    }

    private async void ImportModButton(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0) return;
        
        _subWindowsOpened++;
        var customZipFileType = new FilePickerFileType("Only zip files")
        {
            Patterns = new[] { "*.zip" },
        };
        
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"Select {Utils.GetOfficialName()} Executable",
            AllowMultiple = false,
            FileTypeFilter = new[] {customZipFileType}
        });
        _subWindowsOpened--;
        
        if (files.Count == 0) return;

        try
        {
            Mods.ImportModFromZip(files[0].Path.LocalPath);
        }
        catch (Exception e)
        {
            SpawnErrorWindow($"Cannot import mod zip\n" +
                             $"{e.Message}");
        }
        
        Mods.ScanForModsPublic();
        AddContent();
    }

    private void SpawnErrorWindow(string errorMessage)
    {
        ErrorWindow errorWindow = new ErrorWindow(errorMessage);
        _subWindowsOpened++;
        errorWindow.Closed += ErrorWindowClosed;
        errorWindow.Show();
    }

    private void ModCheckChecked(object? sender, RoutedEventArgs? args)
    {
        if (sender == null) return;

        CheckBox checkBox = (CheckBox)sender;
        if (checkBox.IsChecked == null) return;
        
        if ((bool)checkBox.IsChecked)
        {
            Debug.Assert(checkBox.Name != null, "checkBox.Name != null");
            Mods.GetModByPath(checkBox.Name).Enabled = true;
            Mods.SaveGameModsToConfig();
            // Mods.PrintModsToConsole();
        }
        else
        {
            Debug.Assert(checkBox.Name != null, "checkBox.Name != null");
            Mods.GetModByPath(checkBox.Name).Enabled = false;
            Mods.SaveGameModsToConfig();
            // Mods.PrintModsToConsole();
        }
        
        AddContent();
    }
    
    private void ErrorWindowClosed(object? sender, EventArgs e)
    {
        // Console.WriteLine("error window closed");
        _subWindowsOpened--;
    }
}
