namespace VSHistory;

/// <summary>
/// Command to open the settings page from the Extensions menu.
/// </summary>
[Command(PackageGuids.VSHistoryExtensionsString, PackageIds.VSHistoryExtensionsSettingsID)]
internal sealed class SettingsCommandFromMenu : BaseCommand<SettingsCommandFromMenu>
{
    protected override void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        OpenSettingsWindow();
    }

    /// <summary>
    /// Open the Settings window to the "General" tab
    /// and handle any changes made to settings.
    /// </summary>
    public static void OpenSettingsWindow()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // Open the Settings window at the default (General) tab.
        //
        MainWindow mainWindow = new MainWindow("tabGeneral");

        bool? bOK = mainWindow.ShowDialog();

        //
        // If something changed, update the tool window.
        //
        if (bOK == true)
        {
            RefreshVSHistoryWindow(bForce: true);
        }
    }

    private static bool Localized;

    protected override void BeforeQueryStatus(EventArgs e)
    {
        if (!Localized)
        {
            Localized = true;
            Command.Text = LocalizedString("Settings");
        }

        base.BeforeQueryStatus(e);
    }
}

/// <summary>
/// Command to open the settings page from the "Settings" button in the Tool Window.
/// </summary>
[Command(PackageIds.VSHistory)]
internal sealed class SettingsCommandFromToolWindow : BaseCommand<SettingsCommandFromToolWindow>
{
    protected override void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        SettingsCommandFromMenu.OpenSettingsWindow();
    }
}