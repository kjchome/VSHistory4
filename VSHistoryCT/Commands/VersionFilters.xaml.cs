using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

public class FilterSettings : INotifyPropertyChanged
{
    public FilterSettings(string FilterJsonPath)
    {

    }

    public bool radExclude1 { get; set; } = false;

    public bool radExclude2 { get; set; } = false;

    public bool radInclude1 { get; set; } = true;

    public bool radInclude2 { get; set; } = false;

    public bool radOrInclude { get; set; } = true;

    public string searchString1 { get; set; } = "Hello";

    public string searchString2 { get; set; } = "There";

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Interaction logic for VersionFilters.xaml
/// </summary>
public partial class VersionFilters : Window
{
    private DirectoryInfo VersionDir;

    public static string FilterJson => ".Filter.json";

    private string FilterJsonPath => Path.Combine(VersionDir.FullName, FilterJson);

    private FilterSettings filterSettings;

    /// <summary>
    /// Initialize the form for filtering a VS History directory.
    /// </summary>
    /// <param name="_directory"></param>
    public VersionFilters(DirectoryInfo _directory)
    {
        InitializeComponent();

        VersionDir = _directory;
        filterSettings = new FilterSettings(FilterJsonPath);
        this.DataContext = filterSettings;
    }

    /// <summary>
    /// Clear the first search string, which will trigger the TextChanged event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        searchString1.Text = string.Empty;
        searchString2.Text = string.Empty;
    }

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
    }

    private void EnableControls()
    {
        //
        // If there is no string in searchString1, disable the second half of the form.
        //
        if (string.IsNullOrWhiteSpace(searchString1.Text))
        {
            stackPart2.IsEnabled = false;
            searchString2.IsEnabled = false;
        }
        else
        {
            stackPart2.IsEnabled = true;
            searchString2.IsEnabled = true;
        }
    }

    private void searchString1_TextChanged(object sender, TextChangedEventArgs e)
    {
        EnableControls();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        EnableControls();
    }
}
