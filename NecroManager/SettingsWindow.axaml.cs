using System;
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
    
    public SettingsWindow()
    {
        InitializeComponent();

        Title = $"{Utils.GetOfficialName()} Settings";
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
            FontSize = 30,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        
        MainPanel.Children.Add(new TextBlock
        {
            Text = Utils.GetOfficialName(), 
            HorizontalAlignment = HorizontalAlignment.Center
        });
        
        Button gameDirectoryButton = new Button
        {
            Content = "Select Game Executable", 
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
                Foreground = Brushes.Red
            });
            return;
        }
        
        Button decompileButton = new Button
        {
            Content = "Decompile game scripts", 
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
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        modDirectoryButton.Click += ModDirectoryButton;
        MainPanel.Children.Add(modDirectoryButton);
        
        Button scanModsButton = new Button
        {
            Content = "Scan mods folder", 
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        scanModsButton.Click += ScanModsButton;
        MainPanel.Children.Add(scanModsButton);

        MainPanel.Children.Add(new TextBlock
        {
            FontStyle = FontStyle.Italic,
            Text = "Grayed-out mods are incompatible with",
            FontSize = 12,
            Foreground = Brushes.Red,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        MainPanel.Children.Add(new TextBlock
        {
            FontStyle = FontStyle.Italic,
            Text = "selected mods.",
            FontSize = 12,
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
                    Background = Brushes.DimGray
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
    }

    private async void DecompileButton(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0) return;
        _subWindowsOpened++;
        
        var dirs = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Game Decompile Directory",
            AllowMultiple = false
        });

        if (dirs.Count == 0)
        {
            ErrorWindow errorWindow = new ErrorWindow("No directory selected.");
            errorWindow.Closed += ErrorWindowClosed;
            errorWindow.Show();
            return;
        }
        // substring(7) to remove file:// 
        string decompilePath = dirs[0].Path.ToString()[7..];
        
        if (!Utils.IsDirectoryEmpty(decompilePath))
        {
            ErrorWindow errorWindow = new ErrorWindow("Directory not empty.");
            errorWindow.Closed += ErrorWindowClosed;
            errorWindow.Show();
            return;
        }

        Button decompileButton = (Button)source!;
        decompileButton.Content = "Decompiling...";
        decompileButton.Background = Brushes.Yellow;
        await Utils.DecompileGameAsync(decompilePath);
        _subWindowsOpened--;
        Utils.OpenFileManager(decompilePath);
        AddContent();
    }

    private void ModDirectoryButton(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0) return;

        Utils.OpenFileManager(Path.Combine(Utils.GetInstallPath(), "mods"));
    }
    
    private void ScanModsButton(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0) return;

        Mods.ScanForModsPublic();
        AddContent();
    }
    
    private async void GameDirectoryButton(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0) return;

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
            ErrorWindow errorWindow = new ErrorWindow($"Invalid executable name: {files[0].Name}\n" +
                                                      $"Make sure to select \"{executableName}\"");
            _subWindowsOpened++;
            errorWindow.Closed += ErrorWindowClosed;
            errorWindow.Show();
            return;
        }
        
        // Console.WriteLine($"Valid game name: {files[0].Name}");

        var meow = Utils.GetConfig();
        
        Utils.SetGameConfig(executablePath:files[0].Path.ToString()[7..]);
        
        Utils.SetConfig(meow);
        AddContent();
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
