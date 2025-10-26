
namespace VSHistory;

[Command(PackageIds.VSHistoryFilter)]
internal sealed class FilterCommand : BaseCommand<FilterCommand>
{
    /// <summary>
    /// The "Open" command is used to open the current VS History
    /// directory in the file explorer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void Execute(object sender, EventArgs e)
    {
        if (g_VSControl?.LatestHistoryFile?.VSHistoryDir == null ||
            !g_VSControl.LatestHistoryFile.HasHistoryFiles)
        {
            return;
        }

        
    }

    private static Dictionary<string, int> dicDir = new();

    protected override void BeforeQueryStatus(EventArgs e)
    {
        DirectoryInfo? dir = g_VSControl?.LatestHistoryFile?.VSHistoryDir;

        if (dir == null || !g_VSControl.LatestHistoryFile.HasHistoryFiles)
        {
            return;
        }

        int iter;
        string sPath = dir.FullName;
        if (dicDir.TryGetValue(sPath, out iter))
        {
            dicDir[sPath] = ++iter;
        }
        else
        {
            iter = 1;
            dicDir.Add(sPath, iter);
        }

        Command.Text = $"Iter {iter}";

        base.BeforeQueryStatus(e);
    }
}
