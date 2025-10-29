using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

/// <summary>
/// Interaction logic for VersionFilters.xaml
/// </summary>
public partial class VersionFilters : Window
{
    private DirectoryInfo _VersionDir;

    /// <summary>
    /// The full path to the filter settings file.
    /// </summary>
    private string _FilterSettingsPath =>
        Path.Combine(_VersionDir.FullName, FilterVersions.FilterSettingsFilename);

    /// <summary>
    /// The FilterVersions that existed (if any) when the window 
    /// was opened.  This is used to check to see if any changes
    /// have been made to the filter settings.
    /// </summary>
    private FilterVersions _OriginalSettings { get; }

    /// <summary>
    /// The filter settings displayed on the form.
    /// Any changes to the form will be reflected here.
    /// </summary>
    private FilterVersions _FormSettings { get; set; }

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
        _FormSettings = new(_FilterSettingsPath);

        //
        // Save a copy to check for changes at the end.
        //
        _OriginalSettings = (FilterVersions)_FormSettings.Clone();

        //
        // Set the DataContext to enable the Bindings. It's two-way,
        // so any changes will be reflected in _FormSettings.
        //
        DataContext = _FormSettings;

        EnableControls();
    }

    /// <summary>
    /// Clear the search strings, which will trigger the TextChanged event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        _FormSettings.searchString1 = string.Empty;
        _FormSettings.searchString2 = string.Empty;

        txtString2.Text = string.Empty;
        txtString1.Text = string.Empty;
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
        //
        // If there were no changes, we're done.
        //
        if (_OriginalSettings.Equals(_FormSettings))
        {
            Close();
            return;
        }

        //
        // Prepare to save or destroy the filter settings file.
        //
        FileInfo fileInfo = new FileInfo(_FilterSettingsPath);
        Debug.Assert(_FormSettings.searchString1 == txtString1.Text);

        string sSearch = txtString1.Text;

        if (!string.IsNullOrWhiteSpace(sSearch))
        {
            //
            // There are some settings -- save them.
            //
            try
            {
                using (FileStream fsJson = fileInfo.Create())
                {
                    JsonSerializer.Serialize(fsJson, _FormSettings);
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

        //
        // The filter settings changed in some way -- re-filter the versions.
        //
        FilterVersions.Filter(_VersionDir, _FormSettings);

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
        if (string.IsNullOrWhiteSpace(txtString1.Text))
        {
            stackPart2.IsEnabled = false;
            txtString2.IsEnabled = false;
        }
        else
        {
            stackPart2.IsEnabled = true;
            txtString2.IsEnabled = true;
        }
    }

    /// <summary>
    /// The first search string changed.  Update the controls
    /// based on whether or not it is empty.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void txtString1_TextChanged(object sender, TextChangedEventArgs e)
    {
        //
        // If the user hits Enter in searchString1, this handler will fire
        // but it won't change the content of searchString1 in _FormSettings
        // because the btnGo handler will be invoked immediately.  Therefore,
        // change searchString1 in _FormSettings here.
        //
        Debug.Assert(sender == txtString1);

        _FormSettings.searchString1 = txtString1.Text;
        e.Handled = true;

        EnableControls();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        //
        // Start the user here.
        //
        txtString1.Focus();
    }
}
