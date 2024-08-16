using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace NecroManager;

public partial class MainWindow : Window
{
    // private Window _settingsWindow;
    private int _subWindowsOpened;
    private bool _weedMode;
    
    public MainWindow()
    {
        // _settingsWindow = new SettingsWindow(_game);
        InitializeComponent();
        Closing += (_, e) =>
        {
            if (_subWindowsOpened > 0) e.Cancel = true;
        };
        // MeowView.Content = new Grid();
        CreateContent();
        Resized += OnResize;
        Initialized += WaitForInitialization;
    }

    public void WeedMode()
    {
        _weedMode = true;
        CreateContent();
    }

    private void WaitForInitialization(object? sender, EventArgs e)
    {
        CreateContent();
    }
    
    private void OnResize(object? sender, EventArgs e)
    {
        CreateContent();
    }

    public void ButtonClicked(object source, RoutedEventArgs args)
    {
        if (_subWindowsOpened > 0) return;
        
        Utils.SetGame(((Button)source).Name ?? throw new InvalidOperationException());
        
        CreateContent();
    }

    private void SettingsButtonClicked(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0) return;
        
        Mods.ScanForModsPublic();
        OpenSettingsWindow();
    }

    private void OpenSettingsWindow()
    {
        SettingsWindow settingsWindow = new SettingsWindow(this);
        _subWindowsOpened++;
        settingsWindow.Closed += SettingsWindowClosed;
        settingsWindow.Show();
    }

    private void SettingsWindowClosed(object? sender, EventArgs e)
    {
        _subWindowsOpened--;
        Focusable = true;
        Utils.SaveConfig();
    }

    private async void PlayButtonClicked(object? source, RoutedEventArgs? args)
    {
        if (_subWindowsOpened > 0) return;
        if (!File.Exists(Utils.GetGameConfig(Utils.GetGame()).ExecutablePath))
        {
            OpenSettingsWindow();
            return;
        }

        Button playButton = (Button)source!;
        playButton.Content = "Patching...";
        playButton.Background = Brushes.Gray;
        playButton.Foreground = Brushes.White;
        _subWindowsOpened++;
        await Utils.PatchExecutableAsync();
        Utils.SetReadyToStart();
        Close();
    }

    private void CreateContent()
    {
        // side panel is 200 width
        
        double width = 1240;
        double height = 727; // wysi
        if (!Double.IsNaN(Width))
        {
            width = Width;
            height = Height;
        }

        string bg = $"./img/{Utils.GetGame().ToLower()}bg.jpg";
        if (_weedMode)
        {
            bg = "./img/vuekosmokingbluntweeddrugs.jpg";
        }
        
        ImageBrush bgImage = new ImageBrush(new Bitmap(bg))
        {
            Stretch = Stretch.UniformToFill
        };
        KittyPanel.Background = bgImage;
        
        Button meowButton = new Button
        {
            Content = Utils.GetOfficialName(),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16),
            Background = new SolidColorBrush(Color.FromArgb(0xff, 0x30, 0x30, 0x30)),
            Foreground = Brushes.White
        };

        Button playButton = new Button
        {
            Content = "Play",
            FontSize = 80,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(0xff, 0x30, 0x70, 0x30)),
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(35, 20),
            
        };
        playButton.SetValue(Grid.RowProperty, 3);
        playButton.Click += PlayButtonClicked;
        
        // here i tried to fix the button text and background color not being viewable on hover but 
        // then i stopped caring
        // playButton.BorderBrush = Brushes.White;
        // playButton.BorderThickness = new Thickness(1, 1);
        // playButton.Styles.Add(new Style(x => x.OfType<Button>())
        // {
        //     Selector = PointerOverElementProperty,
        //     
        // });

        MeowGrid.RowDefinitions =
        [
            new(GridLength.Parse("75")), //i have no idea what i am doing someone help me
            new(new GridLength(height - 100))
        ];
        
        MeowGrid.ColumnDefinitions = [new(new GridLength(width))];

        Button settingsButton = new Button
        {
            Content = "Settings",
            FontSize = 40,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(0xff, 0x30, 0x30, 0x30)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(235, 20)
        };
        settingsButton.SetValue(Grid.RowProperty, 3);
        settingsButton.Click += SettingsButtonClicked;

        MeowGrid.Children.Clear();
        MeowGrid.Children.Add(meowButton);
        MeowGrid.Children.Add(playButton);
        MeowGrid.Children.Add(settingsButton);
    }
}