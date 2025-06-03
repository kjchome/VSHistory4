
namespace VSHistory;

[Command(PackageIds.VSHistoryAllFiles)]
internal sealed class AllFilesCommand : BaseCommand<AllFilesCommand>
{
    protected override void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // Open the Settings window at the AllFiles tab.
        //
        MainWindow mainWindow = new MainWindow("tabAllFiles");

        bool? bOK = mainWindow.ShowDialog();

        //
        // If something changed, update the tool window.
        //
        if (bOK == true)
        {
            RefreshVSHistoryWindow(bForce: true);
        }
    }
}
