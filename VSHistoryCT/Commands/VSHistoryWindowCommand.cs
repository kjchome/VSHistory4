namespace VSHistory;

/// <summary>
/// Command to show the VSHistory tool window.
/// This comes from the Extensions menu.
/// </summary>
[Command(PackageGuids.VSHistoryExtensionsString, PackageIds.VSHistoryExtensionsToolWindowID)]
internal sealed class VSHistoryWindowCommand : BaseCommand<VSHistoryWindowCommand>
{
    protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        return VSHistoryToolWindow.ShowAsync();
    }
}
