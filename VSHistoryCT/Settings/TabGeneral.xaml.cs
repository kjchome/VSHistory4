using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

/// <summary>
/// Interaction logic for TabGeneral.xaml
/// </summary>
public partial class TabGeneral : UserControl
{
    public static TabGeneral? tabGeneral { get; private set; } = null;

    public TabGeneral()
    {
        InitializeComponent();

        tabGeneral = this;

        txtSettingsLocation.Text = "Settings are saved here: " + VS_SettingsXmlPath;

        //
        // Init the ComboBox items.
        //
        comboKeepLatest.ItemsSource = KeepLatestValues.Select(x => x.ToString("N0"));
        comboKeepForTime.ItemsSource = KeepForTimeValues.Select(x => x.ToString("0 days"));
        comboFrequency.ItemsSource = ItemsInSeconds(FrequencyValues);
        comboMaxStorage.ItemsSource = ItemsInKB(MaxStorageValues);
        comboGZIP.ItemsSource = ItemsInKB(GZIPValues);
    }

    private void btnReset_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show(
            "This will set all settings to the default values\n" +
            "and will remove any solution-specific settings.\n\n" +
            "Are you sure?",
            "Reset Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes || AllVsSettings.Count < 2)
        {
            return;
        }

        //
        // Copy default settings to all the solution-specific settings.
        //
        for (int i = 1; i < AllVsSettings.Count; i++)
        {
            string sName = AllVsSettings[i].Name;
            AllVsSettings[i] = DefaultSettings.FullClone(sName);
        }

        MainWindow.VSSettingsMainWindow!.DataContext = null;

        //VsSettings.ResetToDefault();

        MainWindow.VSSettingsMainWindow.RefreshTabsWithNewSettings();

        MainWindow.VSSettingsMainWindow!.DataContext = VsSettings;
    }

    private void btnResetThis_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show(
            "This will set the settings for this solution to\n" +
            "the default settings for all solutions.\n" +
            "Any custom settings you made will be lost.\n\n" +
            "Are you sure?",
            "Reset Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        MainWindow.VSSettingsMainWindow!.DataContext = null;

        CopyDefaultToSolution();

        MainWindow.VSSettingsMainWindow.RefreshTabsWithNewSettings();

        MainWindow.VSSettingsMainWindow!.DataContext = null;
        MainWindow.VSSettingsMainWindow!.DataContext = VsSettings;
    }

    /// <summary>
    /// Build a ComboBoxItem with the content showing a human-friendly size,
    /// e.g., "64 KB" or "1 MB", and the Tag containing the size in KB.
    /// </summary>
    /// <param name="storageValues"></param>
    /// <returns></returns>
    private IEnumerable ItemsInKB(uint[] storageValues)
    {
        const uint KbPerMb = 1024;

        foreach (uint size in storageValues)
        {
            string sFormatted;

            if (size >= KbPerMb)
            {
                sFormatted = $"{size / KbPerMb} MB";
            }
            else
            {
                sFormatted = $"{size} KB";
            }

            ComboBoxItem item = new()
            {
                Content = sFormatted,
                Tag = size,
            };

            yield return item;
        }
    }

    /// <summary>
    /// Build a ComboBoxItem with the content showing a human-friendly time,
    /// e.g., "30 seconds" or "1 hour", and the Tag containing the time in seconds.
    /// </summary>
    /// <param name="frequencyValues"></param>
    /// <returns></returns>
    private IEnumerable ItemsInSeconds(uint[] frequencyValues)
    {
        const uint SecondsPerMinute = 60;
        const uint SecondsPerHour = 60 * 60;

        foreach (uint frequency in frequencyValues)
        {
            string sFormatted;

            if (frequency >= 2 * SecondsPerHour)
            {
                sFormatted = $"{frequency / SecondsPerHour} hours";
            }
            else if (frequency >= SecondsPerHour)
            {
                sFormatted = $"{frequency / SecondsPerHour} hour";
            }
            else if (frequency >= 2 * SecondsPerMinute)
            {
                sFormatted = $"{frequency / SecondsPerMinute} minutes";
            }
            else if (frequency >= SecondsPerMinute)
            {
                sFormatted = $"{frequency / SecondsPerMinute} minute";
            }
            else
            {
                sFormatted = $"{frequency} seconds";
            }

            ComboBoxItem item = new()
            {
                Content = sFormatted,
                Tag = frequency,
            };

            yield return item;
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        btnResetThis.IsEnabled = !string.IsNullOrEmpty(SolutionName);

        //
        // Sanity checks.
        //
        Debug.Assert(VsSettings.KeepLatestIndex < comboKeepLatest.Items.Count);
        Debug.Assert(VsSettings.KeepForTimeIndex < comboKeepForTime.Items.Count);
        Debug.Assert(VsSettings.FrequencyIndex < comboFrequency.Items.Count);
        Debug.Assert(VsSettings.MaxStorageIndex < comboMaxStorage.Items.Count);
        Debug.Assert(VsSettings.GZIPIndex < comboGZIP.Items.Count);
    }
}
