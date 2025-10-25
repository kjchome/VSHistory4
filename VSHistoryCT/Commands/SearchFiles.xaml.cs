using System.Windows;

namespace VSHistory;

/// <summary>
/// Interaction logic for SearchFiles.xaml
/// </summary>
public partial class SearchFiles : Window
{
    public SearchFiles()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        string sLine = "This is a fairly long line that may or may not wrap around the window but should kick in the scroll bar.";

        List<string> lines = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            lines.Add(sLine);
        }

        txtOutput.Text=string.Join("\n", lines.ToArray());
    }
}
