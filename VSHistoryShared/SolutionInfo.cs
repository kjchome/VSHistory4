using Microsoft;

namespace VSHistoryShared;

public class SolutionInfo
{
    /// <summary>
    /// When scanning for .vshistory directories, keep track
    /// of the ones we've already processed.
    /// </summary>
    private static HashSet<string> DirsProcessed = new();

    /// <summary>
    /// Base directory if an alternate directory has been selected
    /// or "" if no alternate directory is being used, i.e.,
    /// either the solution or AppData is being used.
    /// </summary>
    public static string AlternateDirectory
    {
        get
        {
            if (VsSettings.FileLocation_Custom)
            {
                //
                // The user-specified location.
                //
                return VsSettings.FileLocation;
            }

            if (VsSettings.FileLocation_AppData)
            {
                //
                // %LOCALAPPDATA%\VSHistory
                //
                return VS_LocalBaseDir;
            }

            return "";
        }
    }

    /// <summary>
    /// List of ALL files in the solution.
    /// </summary>
    public static SortedSet<VSHistoryFile> AllSolutionFiles { get; set; } =
        new(new VSHistoryFileCompare());

    /// <summary>
    /// The name of the solution, e.g., "WpfApp1".
    /// TheSolution.Name is the name of the solution file, e.g., "WpfApp1.sln".
    /// Path.GetFileNameWithoutExtension() returns "WpfApp1" or null if no solution.
    /// </summary>
    public static string? SolutionName
    {
        get => _SolutionName ?? Path.GetFileNameWithoutExtension(SolutionPath);

        set => _SolutionName = value;
    }

    public static string? _SolutionName = null;

    /// <summary>
    /// The full path to the solution file.
    /// </summary>
    public static string? SolutionPath { get; set; } = null;

    /// <summary>
    /// The IVsSolution
    /// </summary>
    public static IVsSolution? TheIVsSolution { get; set; }

    /// <summary>
    /// The Solution.
    /// </summary>
    public static Solution? TheSolution { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public SolutionInfo()
    {
        //
        // Process the current solution, if any.
        // This won't return until after the initialization
        // of the solution has completed.
        //
        ThreadHelper.JoinableTaskFactory.Run(InitSolutionInfoAsync);
    }

    /// <summary>
    /// Process all the children of a SolutionItem.
    /// If a file, save in AllSolutionFiles.
    /// </summary>
    /// <param name="obj"></param>
    private static void AddChildren(SolutionItem obj)
    {
        if (obj == null || obj.Children == null || obj.Children.Count() <= 0)
        {
            return;
        }

        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // Save the file of the SolutionItem itself (.sln, .vcsproj, etc.)
        //
        if (!string.IsNullOrEmpty(obj.FullPath) && obj.Type is SolutionItemType.PhysicalFile)
        {
            VSHistoryFile historyFile = new(obj.FullPath!);
            if (!AllSolutionFiles.Add(historyFile))
            {
                VSLogMsg($"Skipping duplicate {historyFile.FullPath}", Severity.Verbose);
            }
        }

        //
        // Process all the children recursively.
        //
        foreach (SolutionItem item in obj.Children!)
        {
            if (item == null)
            {
                continue;
            }

            switch (item.Type)
            {
                case SolutionItemType.PhysicalFile:
                    PhysicalFile physicalFile = (PhysicalFile)item;
                    AllSolutionFiles.Add(new VSHistoryFile(physicalFile.FullPath!));
                    break;

                case SolutionItemType.Project:
                    VSLogMsg($"Project '{item.Name}'", Severity.Detail);
                    ProcessDirs(Path.GetDirectoryName(LongPath(item.FullPath!)));
                    break;

                case SolutionItemType.MiscProject:
                    Debug.Assert(false, "MiscProject??");
                    break;

                case SolutionItemType.Solution:
                    Debug.Assert(false, "Unexpected solution??");
                    break;

                default:
                    break;
            }

            AddChildren(item);
        }
    }

    /// <summary>
    /// Init the files belonging to a folder.
    /// We include all files except those that clearly
    /// don't need VSHistory processing.
    /// </summary>
    /// <param name="sFolder"></param>
    /// <returns></returns>
    public static void InitFolderInfo(string sFolder)
    {
        VSLogMsg($"Initializing Folder Info for {sFolder}", Severity.Detail);

        ResetSolutionInfo();

        //
        // Save this as the "solution" path.
        //
        SolutionPath = sFolder;

        //
        // Iterate through the directories recursively.
        //
        ProcessDirs(sFolder);
    }

    /// <summary>
    /// Initialize everything about a solution, including
    /// the projects and all files in the solution.
    /// </summary>
    /// <returns></returns>
    public static async Task InitSolutionInfoAsync()
    {
        VSLogMsg("Initializing Solution (waiting for main thread)", Severity.Detail);

        ResetSolutionInfo();

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        TheIVsSolution = await VS.Services.GetSolutionAsync();
        if (TheIVsSolution == null)
        {
            VSLogMsg("IVsSolution is null.", Severity.Detail);
            return;
        }

        if (!TheIVsSolution.IsOpen())
        {
            VSLogMsg("Solution is not open.", Severity.Detail);
            return;
        }

        //
        // Get the Solution.
        //
        TheSolution = (Solution?)await IVsSolutionExtensions.ToSolutionItemAsync(TheIVsSolution);

        if (TheSolution == null)
        {
            VSLogMsg("TheSolution is null.", Severity.Detail);
            return;
        }

        if (string.IsNullOrEmpty(TheSolution.FullPath))
        {
            VSLogMsg("The Solution Path is null or empty.", Severity.Detail);
            return;
        }

        SolutionPath = LongPath(TheSolution.FullPath!);

        VSLogMsg($"Solution: {SolutionPath}", Severity.Detail);

        //
        // Use the settings from this solution.
        // We are using these settings, not editing them.
        //
        EditSettingsState = SettingsState.NotEditing;

        //
        // Process all the .vshistory directories under the solution directory.
        // This will find most of the files with VSHistory versions, including
        // files that have been deleted and are no longer part of the solution.
        // This is useful when displaying "All Files".
        //
        ProcessDirs(Path.GetDirectoryName(SolutionPath));

        //
        // Add the files in the solution directory and all child directories.
        //
        AddChildren(TheSolution);
    }

    /// <summary>
    /// Log solutions files, if any, if verbose logging is enabled.
    /// </summary>
    public static void LogAllSolutionFiles()
    {
        if (!VsSettings.LoggingIsVerbose)
        {
            return;
        }

        ThreadHelper.ThrowIfNotOnUIThread();

        if (AllSolutionFiles.Count == 0)
        {
            VSLogMsg("No solution files found.");
            return;
        }

        StringBuilder sb = new();

        //
        // Start with the solution file itself.
        //
        sb.AppendLine($"Solution '{SolutionName}' {AllSolutionFiles.Count:N0} solution files");
        sb.AppendLine($"  Path: {SolutionPath}");
        sb.AppendLine("  " + new string('-', SolutionPath!.Length + 6));

        //
        // List each solution file and the number of history files.
        //
        foreach (VSHistoryFile file in AllSolutionFiles.OrderBy(n => n.Name))
        {
            string? sProjName = file.ProjectName;

            sb.Append($"  {file.Name,-32} {file.NumHistoryFiles,5:N0} history files ");

            if (!file.VSFileInfo.Exists)
            {
                sb.Append("(deleted) ");
            }

            if (!string.IsNullOrEmpty(sProjName))
            {
                sb.Append($"in project {sProjName}");
            }
            else
            {
                sb.Append(file.FullPath);
            }

            sb.AppendLine();
        }

        VSLogMsg(sb.ToString(), Severity.Verbose);
    }

    /// <summary>
    /// Process the .vshistory directories found under the sParentDirectory.
    /// </summary>
    /// <param name="sParentDirectory"></param>
    public static void ProcessDirs(string sParentDirectory)
    {
        //
        // Look for all .vshistory directories under the parent directory.
        // This is fast and often gets everything.
        //
        VSLogMsg("Parent directory: " + sParentDirectory, Severity.Verbose);

        if (!Directory.Exists(sParentDirectory))
        {
            VSLogMsg($"Parent directory doesn't exist: {sParentDirectory}",
                Severity.Warning);
            return;
        }

        //
        // Each .vshistory directory can contain subdirectories with the filenames.
        // The filenames should exist in the parent directory of the .vshistory directory,
        // although it's possible that they are from deleted files.
        //
        //     dir1\filename.ext
        //     dir1\.vshistory
        //     dir1\.vshistory\filename.ext
        //     dir1\.vshistory\filename.ext\2016-11-12-16_14_27_123.ext
        //     dir1\.vshistory\filename.ext\2016-09-12-18_04_47_311.ext
        //
        foreach (string sHistoryDir in Directory.EnumerateDirectories(
                                        sParentDirectory,
                                        VSHistoryFile.VSHistoryDirName,
                                        SearchOption.AllDirectories))
        {
            //
            // If already done, skip it.  This often happens
            // when the solution directory is processed and
            // then project directories are processed and they
            // are under the solution directory.
            //
            if (!DirsProcessed.Add(sHistoryDir))
            {
                continue;
            }

            //
            // sHistoryDir is the .vshistory directory.
            // It will contain the directories named after
            // the filenames under it.
            //
            VSLogMsg("  VSHistory: " + sHistoryDir, Severity.Verbose);

            DirectoryInfo vsHistoryDir = new(sHistoryDir);

            foreach (DirectoryInfo vsHistoryFilename in vsHistoryDir.EnumerateDirectories())
            {
                VSLogMsg("       File: " + vsHistoryFilename.Name, Severity.Verbose);

                //
                // Add this file to the list.
                //
                string sFullFilename = Path.Combine
                    (vsHistoryDir.Parent.FullName, vsHistoryFilename.Name);

                VSHistoryFile vsHistoryFile = new VSHistoryFile(sFullFilename);

                if (vsHistoryFile.HasHistoryFiles)
                {
                    AllSolutionFiles.Add(vsHistoryFile);
                }
            }
        }
    }

    /// <summary>
    /// Clear all the information about the solution.
    /// </summary>
    public static void ResetSolutionInfo()
    {
        AllSolutionFiles = new(new VSHistoryFileCompare());
        DirsProcessed = new();
        TheIVsSolution = null;
        TheSolution = null;
    }
}
