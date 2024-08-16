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
        int charCap = 50;
        int chars = 0;
        ErrorMessage.Text = "Error Message:\n";
        foreach (string word in errorMessage.Split(' '))
        {
            chars += word.Length;
            if (word.Contains('\n')) chars = 0;
            if (chars >= charCap)
            {
                chars = 0;
                ErrorMessage.Text += "\n";
            }

            ErrorMessage.Text += $"{word} ";
        }
        // i have to do this in code for some reason it doesnt work setting it in axaml
        ErrorMessage.FontFamily = new FontFamily("avares://NecroManager/fonts#Fira Code");
    }
}