using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

/// <summary>
/// The settings to filter VS History files.
/// </summary>
public class FilterSettings : INotifyPropertyChanged
{
    public bool radExclude1 { get; set; } = false;

    public bool radExclude2 { get; set; } = false;

    public bool radInclude1 { get; set; } = true;

    public bool radInclude2 { get; set; } = false;

    public bool radOrInclude { get; set; } = true;

    public string searchString1 { get; set; } = "";

    public string searchString2 { get; set; } = "";

    /// <summary>
    /// Load the settings from the specified path, if it exists.
    /// </summary>
    /// <param name="FilterJsonPath"></param>
    public FilterSettings(string FilterJsonPath)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(FilterJsonPath);

            if (fileInfo.Exists)
            {
                FilterSettings? filterSettings =
                    JsonSerializer.Deserialize<FilterSettings>(fileInfo.OpenRead());

                if (filterSettings != null)
                {
                    radExclude1 = filterSettings.radExclude1;
                    radExclude2 = filterSettings.radExclude2;
                    radInclude1 = filterSettings.radInclude1;
                    radInclude2 = filterSettings.radInclude2;
                    searchString1 = filterSettings.searchString1;
                    searchString2 = filterSettings.searchString2;
                }
            }
        }
        catch
        {
        }
    }

    public FilterSettings()
    {
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Interaction logic for VersionFilters.xaml
/// </summary>
public partial class VersionFilters : Window
{
    private DirectoryInfo _VersionDir;

    public static string FilterJson => ".Filter.json";

    private string _FilterJsonPath => Path.Combine(_VersionDir.FullName, FilterJson);

    private FilterSettings _FilterSettings { get; set; }

    /// <summary>
    /// Initialize the form for filtering a VS History directory.
    /// </summary>
    /// <param name="_directory"></param>
    public VersionFilters(DirectoryInfo _directory)
    {
        InitializeComponent();

        _VersionDir = _directory;

        //
        // If there is a settings file, load it.
        //
        _FilterSettings = new(_FilterJsonPath);

        //
        // Set the DataContext to enable the Bindings.
        //
        DataContext = _FilterSettings;

        EnableControls();
    }

    /// <summary>
    /// Clear the search strings, which will trigger the TextChanged event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        _FilterSettings.searchString1 = string.Empty;
        _FilterSettings.searchString2 = string.Empty;

        searchString2.Text = string.Empty;
        searchString1.Text = string.Empty;
    }

    /// <summary>
    /// If there are any filter settings (i.e. searchString1 isn't empty),
    /// save the settings in the filter file.  If there aren't, delete
    /// the filter file if it exists.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        FileInfo fileInfo = new FileInfo(_FilterJsonPath);
        string sSearch = searchString1.Text;

        if (!string.IsNullOrWhiteSpace(sSearch))
        {
            //
            // There are some settings -- save them.
            //
            try
            {
                using (FileStream fsJson = fileInfo.OpenWrite())
                {
                    JsonSerializer.Serialize(fsJson, _FilterSettings);
                }
            }
            catch
            {
                //
                // Something went wrong?
                //
                sSearch = string.Empty;
            }
        }

        if (string.IsNullOrWhiteSpace(sSearch))
        {
            try
            {
                //
                // There are no filter settings -- delete the file.
                //
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }
            }
            catch
            {
            }
        }

        Close();
    }

    /// <summary>
    /// Enables or disables the second half of the form based on
    /// the presence of text in the first search string input.
    /// </summary>
    /// <remarks>
    /// If the <see cref="searchString1"/> input is empty or contains only whitespace,
    /// the controls in the second part of the form are disabled.
    /// Otherwise, they are enabled. This method ensures that the second
    /// set of controls is only accessible when the first input is valid.
    /// </remarks>
    private void EnableControls()
    {
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

    /// <summary>
    /// The first search string changed.  Update the controls
    /// based on whether or not it is empty.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void searchString1_TextChanged(object sender, TextChangedEventArgs e)
    {
        EnableControls();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
    }
}
