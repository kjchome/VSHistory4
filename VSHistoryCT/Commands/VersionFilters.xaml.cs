using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

public class FilterSettings : INotifyPropertyChanged
{
    public bool radExclude1 { get; set; } = false;

    public bool radExclude2 { get; set; } = false;

    public bool radInclude1 { get; set; } = true;

    public bool radInclude2 { get; set; } = false;

    public bool radOrInclude { get; set; } = true;

    public string searchString1 { get; set; } = "Hello";

    public string searchString2 { get; set; } = "There";

    public event PropertyChangedEventHandler? PropertyChanged;

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

    private FilterSettings? filterSettings { get; set; } = null;

    /// <summary>
    /// Initialize the form for filtering a VS History directory.
    /// </summary>
    /// <param name="_directory"></param>
    public VersionFilters(DirectoryInfo _directory)
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
        filterSettings.searchString1= string.Empty;
        filterSettings.searchString2= string.Empty;
    
        searchString2.Text = string.Empty;
        searchString1.Text = string.Empty;
    }


    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        FileInfo fileInfo = new FileInfo(FilterJsonPath);
        string sSearch = searchString1.Text;

        if (!string.IsNullOrWhiteSpace(sSearch))
        {
            try
            {
                //
                // Save the disk info in a JSON file.
                //
                JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                };

                using (FileStream fsJson = fileInfo.OpenWrite())
                {
                    JsonSerializer.Serialize(fsJson, filterSettings, jsonOptions);
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
        filterSettings = null;

        try
        {
            FileInfo fileInfo = new FileInfo(FilterJsonPath);

            if (fileInfo.Exists)
            {
                filterSettings = JsonSerializer.Deserialize<FilterSettings>(fileInfo.OpenRead());
            }
        }
        catch
        {
        }

        //
        // If we didn't get the settings from the file, fall back to defaults.
        //
        filterSettings ??= new();

        DataContext = filterSettings;

        EnableControls();
    }
}
