namespace VSHistory.Events;

public class VSHistorySolutionEvents
{
    /// <summary>
    /// A folder was opened.
    /// </summary>
    /// <param name="obj"></param>
    public static void SolutionEvents_OnAfterOpenFolder(string obj)
    {
        if (obj == null)
        {
            VSLogMsg("No folder open?", Severity.Warning);
            return;
        }

        ThreadHelper.ThrowIfNotOnUIThread();

        VSLogMsg("Folder OPENED! " + obj, Severity.Detail);

        //
        // Init the new folder info.
        //
        InitFolderInfo(obj);

        LogAllSolutionFiles();
    }

    public static void SolutionEvents_OnAfterCloseSolution()
    {
        VSLogMsg("Solution CLOSED!", Severity.Detail);

        //
        // Reset the list of all solution files.
        //
        ResetSolutionInfo();
    }

    public static void SolutionEvents_OnAfterOpenSolution(Solution? obj)
    {
        if (obj == null)
        {
            VSLogMsg("No solution open?", Severity.Warning);
            return;
        }

        VSLogMsg("Solution OPENED! " + obj.FullPath, Severity.Detail);

        //
        // Init the new solution.
        //
        ThreadHelper.JoinableTaskFactory.Run(InitSolutionInfoAsync);

        ThreadHelper.ThrowIfNotOnUIThread();
        LogAllSolutionFiles();
    }
}
