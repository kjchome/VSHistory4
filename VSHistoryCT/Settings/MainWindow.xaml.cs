global using VSHistoryShared;

global using static VSHistoryShared.VS_Settings;
global using static VSHistoryShared.VSHistoryUtilities;
global using static VSHistoryShared.Win32Imports;

global using MessageBox = System.Windows.MessageBox;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VSHistory;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static bool gbUsingDefault { get; set; } = true;

    public static MainWindow? VSSettingsMainWindow { get; private set; }

    /// <summary>
    /// Constructor to specify the solution name and the selected tab.
    /// </summary>
    /// <param name="fileInfos"></param>
    /// <param name="solutionName"></param>
    /// <param name="selectedTab"></param>
    public MainWindow(string selectedTab)
    {
        InitializeComponent();

        VSSettingsMainWindow = this;

        Title = $"{AssemblyName} " +
            $"v{VSHistoryAssembly.GetName().Version!.ToString(3)}" +
#if DEBUG
            " Debug" +
#else
            " Release" +
#endif
            $", Built {AssemblyBuildTime}";

        //
        // Set the font in all tabs and build the TabIndices dictionary.
        //
        FontSize = VsSettings.NormalFontSize;
        TabIndices = new();

        for (int i = 0; i < tabControl.Items.Count; i++)
        {
            TabItem tabItem = (TabItem)tabControl.Items[i];
            TabIndices.Add(tabItem.Name, i);
            tabItem.FontSize = this.FontSize;
        }

        //
        // Save the solution radio button content without the solution name.
        //
        radThisSolutionContent = radThisSolution.Content.ToString();

        //
        // Mark the specified tab as Selected.
        //
        int selectedTabIndex = 0;
        if (!string.IsNullOrWhiteSpace(selectedTab) && TabIndices.ContainsKey(selectedTab))
        {
            selectedTabIndex = TabIndices[selectedTab];
        }

        tabControl.SelectedIndex = selectedTabIndex;

        bInitializing = false;
    }

    public void RefreshTabsWithNewSettings()
    {
        Debug.Assert(TabDirectoryExclusions.tabDirectoryExclusions != null);
        Debug.Assert(TabFileExclusions.tabFileExclusions != null);
        Debug.Assert(TabLocation.tabLocation != null);
        Debug.Assert(TabGeneral.tabGeneral != null);

        VS_Settings settings = VsSettings;

        //
        // Refresh the tabs that may have changed.
        //
        TabDirectoryExclusions.ExcludedDirs.Clear();
        TabDirectoryExclusions.tabDirectoryExclusions!.gridNames.ItemsSource = null;

        TabFileExclusions.ExcludedFiles.Clear();
        TabFileExclusions.tabFileExclusions!.gridNames.ItemsSource = null;

        //
        // If this is a solution-specific settings display,
        // start with the default values as readonly.
        // Include the default values, but in red and read-only.
        //
        Brush bgBrush = new SolidColorBrush(Colors.AntiqueWhite);
        Brush fgBrush = new SolidColorBrush(Colors.Red);
        Brush fgBlack = new SolidColorBrush(Colors.Black);

        if (!gbUsingDefault)
        {
            VS_Settings defaultSettings = DefaultSettings;
            foreach (string sDir in defaultSettings.ExcludedDirs)
            {
                ExcludedDir excludedDir = new(sDir);
                excludedDir.BGColor = bgBrush;
                excludedDir.FGColor = fgBrush;
                excludedDir.IsEnabled = false;

                TabDirectoryExclusions.ExcludedDirs.Add(excludedDir);
            }

            foreach (string sFile in defaultSettings.ExcludedFiles)
            {
                ExcludedFile excludedFile = new(sFile);
                excludedFile.BGColor = bgBrush;
                excludedFile.FGColor = fgBrush;
                excludedFile.IsEnabled = false;

                TabFileExclusions.ExcludedFiles.Add(excludedFile);
            }
        }

        //
        // If we're displaying the default values, put them in red.
        //
        foreach (string s in settings.ExcludedDirs)
        {
            ExcludedDir excludedDir = new(s);
            excludedDir.FGColor = gbUsingDefault ? fgBrush : fgBlack;
            TabDirectoryExclusions.ExcludedDirs.Add(excludedDir);
        }
        TabDirectoryExclusions.tabDirectoryExclusions!.gridNames.ItemsSource =
            TabDirectoryExclusions.ExcludedDirs;

        foreach (string s in settings.ExcludedFiles)
        {
            ExcludedFile excludedFile = new(s);
            excludedFile.FGColor = gbUsingDefault ? fgBrush : fgBlack;
            TabFileExclusions.ExcludedFiles.Add(excludedFile);
        }
        TabFileExclusions.tabFileExclusions!.gridNames.ItemsSource =
            TabFileExclusions.ExcludedFiles;

        //
        // Force a refresh of the Location and General information.
        //
        TabLocation.tabLocation!.DataContext = null;
        TabLocation.tabLocation!.DataContext = settings;

        TabGeneral.tabGeneral!.DataContext = null;
        TabGeneral.tabGeneral!.DataContext = settings;

        //
        // If there is no solution-specific custom path, show the default.
        // It is still read-only until the user selects the
        // "Store VS History files here" radio button.
        //
        if (!gbUsingDefault &&
            !settings.FileLocation_Custom &&
            string.IsNullOrEmpty(settings.FileLocation))
        {
            TabLocation.tabLocation!.txtCustomPath.Text = DefaultSettings.FileLocation;
        }
    }

    /// <summary>
    /// Save current state of the settings to be restored
    /// when we close the window.
    /// </summary>
    private SettingsState _SettingsState = EditSettingsState;

    private bool bInitializing = true;

    /// <summary>
    /// radThisSolution.Content without the solution name,
    /// which will be added in the Loaded event.
    /// </summary>
    private string radThisSolutionContent;

    /// <summary>
    /// A dictionary that correlates the name of a TabItem
    /// ("tabGeneral", "tabAllFiles", etc.) to their index
    /// in the top-level TabControl.
    /// </summary>
    private Dictionary<string, int> TabIndices;

    /// <summary>
    /// Bye...
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnCloseMain_Click(object? sender, RoutedEventArgs? e)
    {
        //
        // Force the settings to be read from the saved settings.
        // This handles the case where settings were changed and
        // "Cancel" was clicked rather than "OK".
        //
        // If "OK" was clicked, then the changes have already been saved.
        //
        ResetAllSettings();

        //
        // Restore the settings state.
        // We should *not* have been in an editing state.
        //
        if (_SettingsState == SettingsState.EditSettingsDefault ||
            _SettingsState == SettingsState.EditSettingsSolution)
        {
            Debug.Assert(_SettingsState == SettingsState.EditSettingsDefault ||
                _SettingsState == SettingsState.EditSettingsSolution);

            VSLogMsg(
                "The previous settings state was " +
                $"{_SettingsState} when closing the window.",
                Severity.Error);
        }

        EditSettingsState = _SettingsState;

        Close();
    }

    /// <summary>
    /// Change the font size up or down.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnFontUpDown(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        if (button.Name.EndsWith("Up"))
        {
            this.FontSize++;
        }
        else
        {
            this.FontSize--;
        }

        VsSettings.NormalFontSize = this.FontSize;

        foreach (TabItem tabItem in tabControl.Items)
        {
            tabItem.FontSize = this.FontSize;
        }
    }

    /// <summary>
    /// Save the settings and close the window.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        Debug.Assert(DataContext is VS_Settings);
        VS_Settings settings = (VS_Settings)DataContext!;

        //
        // Get the excluded directories and files but don't
        // include the read-only ones -- they're from the default.
        //
        settings.ExcludedDirs = TabDirectoryExclusions.ExcludedDirs
            .Where(r => r.IsEnabled)
            .Select(n => n.Name).ToList();

        settings.ExcludedFiles = TabFileExclusions.ExcludedFiles
            .Where(r => r.IsEnabled)
            .Select(n => n.Name).ToList();

        //
        // Make sure the settings are valid before we exit.
        //
        if (!ValidateSettings())
        {
            e.Handled = true;
            return;
        }

        try
        {
            //
            // Save the settings to the XML file.
            // Make sure the settings directory exists.
            //
            Directory.CreateDirectory(VS_LocalBaseDir);

            //
            // Overwrite the settings file, creating it if necessary.
            // PruneSettings will remove keys that are the same as the default.
            //
            DialogResult = SaveXml(PruneSettings());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            //
            // Close the window.
            //
            btnCloseMain_Click(sender, e);
        }
    }

    /// <summary>
    /// This is called when the user has checked the "Default solution"
    /// or the "This Solution" radio button.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void radAllSolutions_Checked(object sender, RoutedEventArgs e)
    {
        if (bInitializing)
        {
            return;
        }

        gbUsingDefault = radAllSolutions.IsChecked == true;

        //
        // Sanity check.
        //
        if (!gbUsingDefault && string.IsNullOrWhiteSpace(SolutionName))
        {
            throw new InvalidOperationException(
                "Cannot set the solution radio button to 'This Solution' " +
                "when there is no solution name.");
        }

        //
        // Determine what editing we're doing.
        //
        EditSettingsState = gbUsingDefault ? SettingsState.EditSettingsDefault : SettingsState.EditSettingsSolution;

        //
        // Get the settings for this solution or default as appropriate.
        //
        SaveCurrentTabData();

        DataContext = null;
        DataContext = VsSettings;

        RefreshTabsWithNewSettings();
    }

    /// <summary>
    /// One of the "Left", "Right", "Top" radio buttons was checked.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RadioButton_Checked(object? sender, RoutedEventArgs? e)
    {
        if (radTabsLeft.IsChecked == true)
        {
            tabControl.TabStripPlacement = Dock.Left;
        }
        else if (radTabsTop.IsChecked == true)
        {
            tabControl.TabStripPlacement = Dock.Top;
        }
        else if (radTabsRight.IsChecked == true)
        {
            tabControl.TabStripPlacement = Dock.Right;
        }
    }

    private void SaveCurrentTabData()
    {
        Debug.Assert(TabDirectoryExclusions.tabDirectoryExclusions != null);
        Debug.Assert(TabFileExclusions.tabFileExclusions != null);

        //
        // Get the current settings -- either the default or solution-specific.
        //
        VS_Settings settings = (VS_Settings)DataContext!;

        if (settings == null)
        {
            return;
        }

        //
        // Save the excluded files and directories in the settings as List<string>.
        //
        if (TabDirectoryExclusions.tabDirectoryExclusions != null &&
            TabDirectoryExclusions.ExcludedDirs != null)
        {
            settings.ExcludedDirs = TabDirectoryExclusions.ExcludedDirs
                .Where(r => r.IsEnabled)
                .Select(n => n.Name).ToList();
        }

        if (TabFileExclusions.tabFileExclusions != null &&
            TabFileExclusions.ExcludedFiles != null)
        {
            settings.ExcludedFiles = TabFileExclusions.ExcludedFiles
                .Where(r => r.IsEnabled)
                .Select(n => n.Name).ToList();
        }
    }

    /// <summary>
    /// Validate the settings for a specific solution or the default settings.
    /// </summary>
    /// <param name="sKey"></param>
    /// <param name="sName"></param>
    /// <returns></returns>
    private bool ValidateSettings(VS_Settings settings)
    {
        string sName = settings.Name;
        if (string.IsNullOrEmpty(sName))
        {
            sName = "Default";
        }

        //
        // If a custom location is selected, make sure the directory is specified.
        //
        if (settings.FileLocation_Custom)
        {
            if (string.IsNullOrWhiteSpace(settings.FileLocation))
            {
                MessageBox.Show($"In the {sName} settings, you have selected to store\n" +
                    $"VSHistory files in a directory but you have not specified\n" +
                    $"where they should be saved.\n\n" +
                    $"You must correct this before the settings can be saved.\n" +
                    $"Go to the \"Location of Files\" tab to fix this.",
                    "No Directory for Saving VSHistory Files",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }

            //
            // Validate the directory string.
            //
            ExcludedDir dir = new(settings.FileLocation);
            if (!dir.IsValid)
            {
                MessageBox.Show($"In the {sName} settings, you have specified an " +
                    $"invalid directory \"{dir.Name}\".\n\n" +
                    $"You must correct this before the settings can be saved.\n" +
                    $"Go to the \"Location of Files\" tab to fix this.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
        }

        //
        // Validate the excluded directories.
        // NOTE: This should have already been handled!
        //
        foreach (string sDir in settings.ExcludedDirs)
        {
            ExcludedDir dir = new(sDir);
            if (!dir.IsValid)
            {
                VSLogMsg($"Invalid directory {dir.Name} for {sName}.");
                MessageBox.Show($"In the {sName} solution, there is an " +
                    $"invalid directory {dir.Name}.\n\n" +
                    $"You must correct this before the settings can be saved.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
        }

        //
        // Validate the excluded files.
        // NOTE: This should have already been handled!
        //
        foreach (string sFile in settings.ExcludedFiles)
        {
            ExcludedFile file = new(sFile);
            if (!file.IsValid)
            {
                VSLogMsg($"Invalid file {file.Name} for {sName}.");
                MessageBox.Show($"In the {sName} solution, there is an " +
                    $"invalid file {file.Name}.\n\n" +
                    $"You must correct this before the settings can be saved.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validate the settings for the current solution and the default settings.
    /// </summary>
    /// <returns></returns>
    private bool ValidateSettings()
    {
        //
        // Prune the settings to remove any that are the same as the default.
        //
        List<VS_Settings> allSettings = PruneSettings();

        foreach (VS_Settings sSettings in allSettings)
        {
            if (!ValidateSettings(sSettings))
            {
                return false;
            }
        }

        return true;
    }

    private void winMainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        //
        // We are editing the settings.  What can be done depends on
        // whether we have a solution name.
        //
        if (string.IsNullOrWhiteSpace(SolutionName))
        {
            //
            // There is no solution.  Disable the "This Solution" radio button.
            //
            radThisSolution.IsEnabled = false;
        }
        else
        {
            //
            // Set the content of the solution radio button to include the solution name,
            // e.g., "Directories in _THIS solution (SolutionName)".
            //
            radThisSolution.Content = radThisSolutionContent + $" ({SolutionName})";
            radThisSolution.IsEnabled = true;
        }

        //
        // Start with the "All Solutions" radio button checked.
        //
        Debug.Assert(radAllSolutions.IsChecked == true);

        EditSettingsState = SettingsState.EditSettingsDefault;

        //
        // Get the settings for this solution or default as appropriate.
        //
        DataContext = VsSettings;

        RefreshTabsWithNewSettings();
    }
}

/// <summary>
/// A directory to be excluded from VS History processing.
/// </summary>
public class ExcludedDir : ExcludedDirOrFile
{
    public ExcludedDir() : base(ExcludedType.Directory)
    {
    }

    public ExcludedDir(string _name) : base(_name, ExcludedType.Directory)
    {
    }
}

/// <summary>
/// A file to be excluded from VS History processing.
/// </summary>
public class ExcludedFile : ExcludedDirOrFile
{
    public ExcludedFile() : base(ExcludedType.File)
    {
    }

    public ExcludedFile(string _name) : base(_name, ExcludedType.File)
    {
    }
}

/// <summary>
/// A directory or file to be excluded from VS History processing.
/// </summary>
public class ExcludedDirOrFile
{
    /// <summary>
    /// The background color of the cell in the DataGrid.
    /// This is changed when this is a all-solutions exclusion
    /// in a solution-specific settings display.
    /// </summary>
    public Brush BGColor { get; set; } = Brushes.White;

    /// <summary>
    /// The foreground color of the cell in the DataGrid.
    /// This is changed when this is a all-solutions exclusion
    /// in a solution-specific settings display.
    /// </summary>
    public Brush FGColor { get; set; } = Brushes.Black;

    /// <summary>
    /// Set True if this entry can be modified, False if not,
    /// i.e., a default setting displayed in a solution-specific page.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public bool IsValid
    {
        get
        {
            //
            // Check for invalid characters in the name.
            //
            if (string.IsNullOrWhiteSpace(Name) || WhichType == ExcludedType.Undefined)
            {
                VSLogMsg("Name or type cannot be null or empty.");
                return false;
            }

            if (WhichType == ExcludedType.Directory)
            {
                //
                // Skip over "\\?\" prefix if it exists.
                //
                string sTrim = Name;
                if (sTrim.StartsWith(@"\\?\"))
                {
                    sTrim = sTrim.Substring(4);
                }

                if (sTrim.IndexOfAny(InvalidDirChars) >= 0)
                {
                    VSLogMsg("Directory contains invalid characters.");
                    return false;
                }
                return true;
            }

            if (Name.IndexOfAny(InvalidFileChars) >= 0)
            {
                VSLogMsg("Filename contains invalid characters.");
                return false;
            }
            return true;
        }
    }

    public string Name
    {
        get => _Name;

        set
        {
            if (!string.IsNullOrWhiteSpace(value) && value != _Name)
            {
                VSLogMsg($"Setting type {WhichType} to {value}", Severity.Detail);
                _Name = value;
            }
        }
    }

    /// <summary>
    /// The type of exclusion, directory or file.
    /// </summary>
    public ExcludedType WhichType { get; set; }

    public enum ExcludedType
    {
        Undefined,
        Directory,
        File
    }

    public ExcludedDirOrFile()
    {
    }

    public ExcludedDirOrFile(ExcludedType _Type)
    {
        WhichType = _Type;
        VSLogMsg($"New excluded {WhichType}, no name");
    }

    public ExcludedDirOrFile(string _name, ExcludedType _Type)
    {
        _Name = _name;
        WhichType = _Type;
        VSLogMsg($"New excluded {WhichType} '{Name}'");
    }

    public void ResetName()
    {
        _Name = "";
    }

    public override string ToString()
    {
        return $"Exclude {WhichType} {Name}";
    }

    private string _Name = "";

    /// <summary>
    /// Characters that cannot be in a directory string.
    /// We use this instead of Path.GetInvalidPathChars()
    /// because we want to allow '"', '\\' and ':' in the string.
    /// </summary>
    private char[] InvalidDirChars =>
    [
        '*', '?', '<', '>', '|', '\0', '/',
        (char)1, (char)2, (char)3, (char)4, (char)5,
        (char)6, (char)7, (char)8, (char)9, (char)10,
        (char)11, (char)12, (char)13, (char)14, (char)15,
        (char)16, (char)17, (char)18, (char)19, (char)20,
        (char)21, (char)22, (char)23, (char)24, (char)25,
        (char)26, (char)27, (char)28, (char)29, (char)30,
        (char)31
    ];

    /// <summary>
    /// Characters that cannot be in a filename string.
    /// We use this instead of Path.GetInvalidFilenameChars()
    /// because we want to allow wildcard characters in the
    /// string but not '\\' or ':'.
    /// </summary>
    private char[] InvalidFileChars =>
    [
        ':', '\\', '<', '>', '|', '\0', '/',
        (char)1, (char)2, (char)3, (char)4, (char)5,
        (char)6, (char)7, (char)8, (char)9, (char)10,
        (char)11, (char)12, (char)13, (char)14, (char)15,
        (char)16, (char)17, (char)18, (char)19, (char)20,
        (char)21, (char)22, (char)23, (char)24, (char)25,
        (char)26, (char)27, (char)28, (char)29, (char)30,
        (char)31
    ];
}
