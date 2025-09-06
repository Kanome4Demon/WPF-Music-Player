using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public static class WindowHelper
{
    public static void ToggleMaxRestore(Window window, Border mainBorder, Button maxRestoreButton)
    {
        if (window.WindowState == WindowState.Normal)
        {
            mainBorder.Margin = new Thickness(0);
            window.WindowState = WindowState.Maximized;
            maxRestoreButton.Content = "\xE923";
            maxRestoreButton.ToolTip = "还原";
        }
        else
        {
            window.WindowState = WindowState.Normal;
            mainBorder.Margin = new Thickness(20);
            maxRestoreButton.Content = "\xE922";
            maxRestoreButton.ToolTip = "最大化";
        }
    }

    public static void Minimize(Window window)
    {
        window.WindowState = WindowState.Minimized;
    }

    public static void Close(Window window)
    {
        window.Close();
    }

    public static void Drag(Window window, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            window.DragMove();
        }
    }
}
