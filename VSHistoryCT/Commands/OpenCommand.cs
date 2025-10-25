namespace VSHistory;

[Command(PackageIds.VSHistoryOpen)]
internal sealed class OpenCommand : BaseCommand<OpenCommand>
{
    /// <summary>
    /// The "Open" command is used to open the current VS History
    /// directory in the file explorer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void Execute(object sender, EventArgs e)
    {
        if (g_VSControl?.LatestHistoryFile?.VSHistoryDir != null &&
            g_VSControl.LatestHistoryFile.HasHistoryFiles)
        {
            Process.Start(g_VSControl.LatestHistoryFile.VSHistoryDir.FullName);
        }
    }
}
