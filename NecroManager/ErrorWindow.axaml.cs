using Avalonia.Controls;
using Avalonia.Media;

namespace NecroManager;

public partial class ErrorWindow : Window
{
    // the parameter here causes the ErrorWindow.axaml build warning even though this code runs fine
    public ErrorWindow(string errorMessage)
    {
        InitializeComponent();
        Topmost = true;
        Width = 600;
        Height = 200;
        ErrorMessage.Text = $"Error Message:\n" +
                            $"{errorMessage}";
        
        // i have to do this in code for some reason it doesnt work setting it in axaml
        ErrorMessage.FontFamily = new FontFamily("avares://NecroManager/fonts#Fira Code");
    }
}