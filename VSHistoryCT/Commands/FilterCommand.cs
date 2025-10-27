
namespace VSHistory;

[Command(PackageIds.VSHistoryFilter)]
internal sealed class FilterCommand : BaseCommand<FilterCommand>
{
    /// <summary>
    /// The label for the Filter button -- "Filter" (localized).
    /// </summary>
    private static string? _CommandText = null;

    /// <summary>
    /// Set the Filter label appropriately depending on
    /// whether any filters are defined for this file.
    /// </summary>
    /// <param name="e"></param>
    protected override void BeforeQueryStatus(EventArgs e)
    {
        VSHistoryFile? historyFile = g_VSControl?.LatestHistoryFile;
        if (historyFile == null)
        {
            return;
        }

        DirectoryInfo? dir = historyFile.VSHistoryDir;
        if (dir == null || !historyFile.HasHistoryFiles)
        {
            return;
        }

        //
        // If the filter file exists, add a check to the button.
        //
        _CommandText ??= LocalizedString("Filter");

        if (File.Exists(Path.Combine(LongPath(dir.FullName), VersionFilters.FilterJson)))
        {
            const string checkmark = "\u2713";
            Command.Text = $"{checkmark} {_CommandText}";
        }
        else
        {
            Command.Text = $"{_CommandText}";
        }

        //
        // This doesn't do anything, but...
        //
        base.BeforeQueryStatus(e);
    }

    /// <summary>
    /// The "Filter" command opens the VersionFilters window
    /// to let the use edit the version filters.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        VSHistoryFile? historyFile = g_VSControl?.LatestHistoryFile;
        if (historyFile == null)
        {
            return;
        }

        DirectoryInfo? dir = historyFile.VSHistoryDir;
        if (dir == null || !historyFile.HasHistoryFiles)
        {
            return;
        }

        //
        // Open the Filter window.
        //
        VersionFilters filterVersions = new(dir);
        filterVersions.FontSize = VsSettings.NormalFontSize;

        filterVersions.ShowDialog();
    }
}
