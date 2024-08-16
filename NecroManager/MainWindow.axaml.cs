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
        SettingsWindow settingsWindow = new SettingsWindow();
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
        playButton.Background = Brushes.Yellow;
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
        
        ImageBrush bgImage = new ImageBrush(new Bitmap($"./img/{Utils.GetGame().ToLower()}bg.jpg"))
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
            Background = Brushes.Gray
        };

        Button playButton = new Button
        {
            Content = "Play",
            FontSize = 72,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(20),
            Background = Brushes.Lime
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
            FontSize = 72,
            Background = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(220, 20)
        };
        settingsButton.SetValue(Grid.RowProperty, 3);
        settingsButton.Click += SettingsButtonClicked;

        MeowGrid.Children.Clear();
        MeowGrid.Children.Add(meowButton);
        MeowGrid.Children.Add(playButton);
        MeowGrid.Children.Add(settingsButton);
    }
}