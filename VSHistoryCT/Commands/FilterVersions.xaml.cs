using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

/// <summary>
/// Interaction logic for FilterVersions.xaml
/// </summary>
public partial class FilterVersions : Window
{
    private DirectoryInfo VersionDir;

    public static string FilterJson => ".Filter.json";

    private string FilterJsonPath => Path.Combine(VersionDir.FullName, FilterJson);

    /// <summary>
    /// Initialize the form for filtering a VS History directory.
    /// </summary>
    /// <param name="_directory"></param>
    public FilterVersions(DirectoryInfo _directory)
    {
        InitializeComponent();

        VersionDir = _directory;
    }

    /// <summary>
    /// Clear the first search string, which will trigger the TextChanged event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        txt1.Text = string.Empty;
        txt2.Text = string.Empty;
    }

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
    }

    private void EnableControls()
    {
        //
        // If there is no string in txt1, disable the second half of the form.
        //
        if (string.IsNullOrWhiteSpace(txt1.Text))
        {
            stackPart2.IsEnabled = false;
            txt2.IsEnabled = false;
        }
        else
        {
            stackPart2.IsEnabled = true;
            txt2.IsEnabled = true;
        }
    }

    private void txt1_TextChanged(object sender, TextChangedEventArgs e)
    {
        EnableControls();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        EnableControls();
    }
}
