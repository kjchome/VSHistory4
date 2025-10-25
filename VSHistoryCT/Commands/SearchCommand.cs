namespace VSHistory;

[Command(PackageIds.VSHistorySearch)]
internal sealed class SearchCommand : BaseCommand<SearchCommand>
{
    /// <summary>
    /// The "Search" command is used to search the files in the current VS History directory.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (g_VSControl?.LatestHistoryFile?.VSHistoryDir == null ||
            !g_VSControl.LatestHistoryFile.HasHistoryFiles)
        {
            return;
        }

        //
        // Open the Search window.
        //
        SearchFiles searchFiles = new();

        searchFiles.ShowDialog();
    }
}
