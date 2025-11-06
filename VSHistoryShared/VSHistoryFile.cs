
namespace VSHistoryShared;

/// <summary>
/// Class to manage VS History files
/// </summary>
public class VSHistoryFile
{
    private PhysicalFile? _PhysicalFile = null;
    private string? _ProjectName = null;

    /// <summary>
    /// The compressed, hidden directory into which all VS History files are saved.
    /// For example, "c:\SomeDir\SubDir\file.cpp" will be save in the directory
    /// "c:\SomeDir\SubDir\.vshistory\file.cpp", with filename timestamps like
    /// "2016-09-12-18_04_47_311.cpp" and "2016-09-12-17_36_49_335.cpp".
    /// </summary>
    public static string VSHistoryDirName => ".vshistory";

    /// <summary>
    /// Mask to use when searching for VS History files.
    /// The filename may end with "-" to indicate that it has
    /// been filtered out from display.
    /// </summary>
    public static string VSHistoryFilenameMask => "????-??-??_??_??_??_????.*";

    /// <summary>
    /// Format of the VS history filenames saved.
    /// </summary>
    public static string VSHistoryTimestampFormat => "yyyy-MM-dd_HH_mm_ss_fff";

    private long? _ClusterSize;

    /// <summary>
    /// The cluster size, in bytes, of the volume associated with this VSHistoryFile.
    /// </summary>
    public long ClusterSize => _ClusterSize ??= GetClusterSize(FullPath);

    /// <summary>
    /// Accessor function.
    /// </summary>
    public bool HasHistoryFiles => NumHistoryFiles > 0;

    /// <summary>
    /// Accessor function.
    /// </summary>
    public int NumHistoryFiles => VSHistoryFiles.Count;

    /// <summary>
    /// The number of VSHistory files that are filtered out.
    /// </summary>
    public int NumFiltered => FilteredFilenames?.Count ?? 0;

    /// <summary>
    /// FileInfo of the file in the VS project.
    /// This cannot be null but the file may be deleted.
    /// </summary>
    public FileInfo VSFileInfo { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string? Name => VSFileInfo.Name;

    /// <summary>
    /// Full path to the VS project file.
    /// </summary>
    public string FullPath => VSFileInfo.FullName;

    private DirectoryInfo? _VSHistoryDir;

    /// <summary>
    /// Directory of the VS History files.
    /// </summary>
    public DirectoryInfo VSHistoryDir
    {
        get
        {
            if (_VSHistoryDir == null)
            {
                //
                // Directory of the history files, if any.
                //
                // If an alternate directory was specified, then the VS History files
                // will be in <baseDirectory>\.vshistory\<path to file>
                //
                // Otherwise, the VS History files will be in filedir\.vshistory\filename
                //
                string? baseDirectory = AlternateDirectory;
                string fullPath;

                if (string.IsNullOrEmpty(baseDirectory))
                {
                    //
                    // The VS History files are maintained in the same
                    // folder as the source file.
                    //
                    //  file = C:\Users\SomeUser\Documents\Projects\TheProject\file.cs
                    //
                    //  fullPath = C:\Users\SomeUser\Projects\TheProject\.vshistory\file.cs
                    //
                    fullPath = Path.Combine(
                        VSFileInfo.DirectoryName!,
                        VSHistoryDirName,
                        VSFileInfo.Name);
                }
                else
                {
                    //
                    // Build the full path to where the VS History files
                    // will be stored in the alternate directory.
                    //
                    //  baseDirectory = C:\Users\SomeUser\AppData\Local
                    //  FullPath = \\?\C:\Users\SomeUser\Documents\Projects\TheProject\file.cs
                    //
                    //  fullPath = C:\Users\SomeUser\AppData\Local\.vshistory\C\Users\SomeUser\Documents\Projects\TheProject\file.cs
                    //
                    char[] asTrim = { '\\', '?' };

                    fullPath = Path.Combine(
                        baseDirectory,
                        VSHistoryDirName,
                        FullPath.TrimStart(asTrim).Replace(":", ""));
                }

                //
                // Convert the path to a long path ("\\?\C:\...") and save it.
                //
                _VSHistoryDir = new DirectoryInfo(LongPath(fullPath));
            }

            return _VSHistoryDir;
        }
    }

    /// <summary>
    /// The list of filenames that are filtered out, if any.
    /// </summary>
    public List<string> FilteredFilenames = new();

    private List<FileInfo>? _VSHistoryFiles;

    /// <summary>
    /// List of the FileInfos for the VS History files.
    /// </summary>
    public List<FileInfo> VSHistoryFiles
    {
        get
        {
            if (_VSHistoryFiles == null)
            {
                _VSHistoryFiles = new List<FileInfo>();
                FilteredFilenames = new List<string>();

                if (VSHistoryDir.Exists)
                {
                    //
                    // Get all the files and save them in reverse order (most recent first).
                    //
                    _VSHistoryFiles = [.. VSHistoryDir.GetFiles(VSHistoryFilenameMask)];
                    _VSHistoryFiles.Reverse();

                    //
                    // Get the filter settings, if any, and get the
                    // list of filtered filenames.
                    //
                    string sFilterPath =
                        Path.Combine(VSHistoryDir.FullName, FilterVersions.FilterSettingsName);

                    if (File.Exists(sFilterPath))
                    {
                        FilterVersions filterVersions = new(sFilterPath);
                        FilteredFilenames.AddRange(filterVersions.FilteredFiles);
                    }
                }
            }

            return _VSHistoryFiles;
        }
    }

    /// <summary>
    /// Constructor -- Build information about possible
    /// VS History files for a VS project file.
    /// </summary>
    /// <param name="fileInfo">
    /// FileInfo of a file in the VS project.
    /// </param>
    public VSHistoryFile(FileInfo fileInfo)
    {
        VSFileInfo = fileInfo;
    }

    public override string ToString() => $"{VSFileInfo.Name} {NumHistoryFiles} history files";

    /// <summary>
    /// Constructor -- Build information about possible
    /// VS History files for a VS project file.
    /// </summary>
    /// <param name="filePath">
    /// The path to a file in the VS project.
    /// </param>
    public VSHistoryFile(string filePath) : this(new FileInfo(LongPath(filePath)))
    {
    }

    /// <summary>
    /// Create a directory if necessary and set its attributes to Hidden and Compressed.
    /// </summary>
    /// <param name="dirVsHistory"></param>
    private static void CreateAndHide(DirectoryInfo dirVsHistory)
    {
        //
        // Create the directory as hidden.
        // If it doesn't already exist, create it.
        //
        if (!dirVsHistory.Exists)
        {
            dirVsHistory.Create();
            dirVsHistory.Refresh();
        }

        if ((dirVsHistory.Attributes & FileAttributes.Hidden) == 0)
        {
            File.SetAttributes(dirVsHistory.FullName, FileAttributes.Hidden);
        }

        if ((dirVsHistory.Attributes & FileAttributes.Compressed) != FileAttributes.Compressed)
        {
            //
            // Set the directory to Compressed.
            //
            // CreateFile/DeviceIoControl is used here to accommodate long
            // paths. This will set the Compressed attribute on the directory
            // so that any *new* files will be compressed.  However, it will
            // not compress any existing files/directories.
            //
            // The way to include existing files/directories is to create
            // a ManagementObject and invoke the "Compress" method.
            // However, that doesn't handle long paths and doesn't
            // even handle paths prefixed with "\\?\C:\".
            //
            // Therefore, open the directory and invoke DeviceIoControl.
            //
            using SafeFileHandle handle = CreateFile(
                dirVsHistory.FullName,
                FileAccess.ReadWrite,
                FileShare.ReadWrite,
                IntPtr.Zero,
                FileMode.Open,
                (FileAttributes)FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero
                );

            if (!handle.IsInvalid)
            {
                int lpBytesReturned = 0;
                short lpInBuffer = 1;   // COMPRESSION_FORMAT_DEFAULT

                DeviceIoControl(
                    handle,
                    FSCTL_SET_COMPRESSION,
                    ref lpInBuffer,
                    sizeof(short),
                    IntPtr.Zero,
                    0,
                    ref lpBytesReturned,
                    IntPtr.Zero);
            }

            int errorLevel = Marshal.GetLastWin32Error();
            if (errorLevel != NO_ERROR)
            {
                VSLogMsg($"DeviceIoControl returned {errorLevel} on " +
                    $"{dirVsHistory.FullName}", Severity.Error);
            }
        }

        dirVsHistory.Refresh();
    }

    /// <summary>
    /// Build the FileInfo for the history file with filename timestamps, e.g.,
    /// "2016-09-12-18_04_47_311.cs".
    ///
    /// If the file is larger than the volume's cluster size, it should be compressed.
    /// In this case, the file extension will be the original file extension with the
    /// uncompressed file size and ".gz", "2016-09-12-18_04_47_311.cs.14321.gz".
    /// </summary>
    /// <returns></returns>
    public FileInfo BuildHistoryFile()
    {
        string timeStamp = VSFileInfo.LastWriteTime.ToString(VSHistoryTimestampFormat);
        string historyPath = Path.Combine(VSHistoryDir.FullName, timeStamp);

        //
        // Minimum size to compress a file.
        //
        uint uiMinGzipSize = VsSettings.GZIP ? GZIPValues[VsSettings.GZIPIndex] * 1024 : 0;

        if (uiMinGzipSize > 0 && VSFileInfo.Length > uiMinGzipSize)
        {
            //
            // Use the original file extension + size + ".gz"
            //
            historyPath = Path.ChangeExtension(historyPath,
                VSFileInfo.Extension + "." + VSFileInfo.Length + ".gz");
        }
        else
        {
            //
            // No compression necessary -- use the original file extension.
            //
            historyPath = Path.ChangeExtension(historyPath, VSFileInfo.Extension);
        }

        return new FileInfo(historyPath);
    }

    /// <summary>
    /// Create the directory for the history files
    /// </summary>
    /// <returns>DirectoryInfo of the path on success, or null on failure</returns>
    public DirectoryInfo CreatePath()
    {
        //
        // Get the base path to the .vshistory directory.
        //
        DirectoryInfo? dirVsHistory;
        string? baseDirectory = AlternateDirectory;

        if (string.IsNullOrEmpty(baseDirectory))
        {
            //
            // The VS History files are maintained in the same folder as the source file.
            //
            dirVsHistory = VSHistoryDir.Parent;
            dirVsHistory?.Refresh();
        }
        else
        {
            //
            // The VS History files are maintained in the alternate directory.
            //
            dirVsHistory = new DirectoryInfo(Path.Combine(baseDirectory, VSHistoryDirName));
        }

        //
        // Create the .vshistory directory.
        //
        CreateAndHide(dirVsHistory!);

        //
        // Now create the directory for the history file.
        //
        CreateAndHide(VSHistoryDir);

        return VSHistoryDir;
    }

    /// <summary>
    /// The PhysicalFile associated with this solution file.
    /// </summary>
    public PhysicalFile? ThePhysicalFile => _PhysicalFile ??=
        ThreadHelper.JoinableTaskFactory.Run(() => PhysicalFile.FromFileAsync(FullPath));

    /// <summary>
    /// The Project name associated with this solution file.
    /// </summary>
    public string? ProjectName => _ProjectName ??= ThePhysicalFile?.ContainingProject?.Name;

    /// <summary>
    /// This VSHistory file has been renamed.
    /// </summary>
    /// <param name="sNewFilename"></param>
    public void Rename(string sNewFilename)
    {
#if VSHISTORY_PACKAGE
        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // Is this the currently displayed document?
        //
        bool bCurrentWindowIsThis =
            g_VSControl?.LatestHistoryFile?.FullPath == VSFileInfo.FullName;

        FileInfo oldFileInfo = VSFileInfo;
        VSFileInfo = new(sNewFilename);

        //
        // We only handle renaming the filename (think that's the only possibility?).
        //
        if (VSFileInfo.DirectoryName == oldFileInfo.DirectoryName && VSHistoryDir.Exists)
        {
            try
            {
                string sNewVSHistoryDir =
                    Path.Combine(VSHistoryDir.Parent.FullName, VSFileInfo.Name);

                //
                // Move it.
                //
                VSHistoryDir.MoveTo(sNewVSHistoryDir);
                VSHistoryDir.Refresh();

                VSLogMsg($"Moved '{oldFileInfo.Name}' to '{VSFileInfo.Name}'");

                if (bCurrentWindowIsThis)
                {
                    RefreshVSHistoryWindow(VSFileInfo.FullName);
                }
            }
            catch (Exception ex)
            {
                VSLogMsg($"Failed to move '{oldFileInfo.Name}' to '{VSFileInfo.Name}': {ex}");
            }
        }
#endif
    }

    /// <summary>
    /// Save a file in its VS History directory if not done already.
    /// </summary>
    public void Save()
    {
        //
        // Quick exit for an empty file.
        //
        if (!VSFileInfo.Exists || VSFileInfo.Length == 0)
        {
            VSLogMsg("Skipping empty file " + VSFileInfo.Name);
            return;
        }
        ThreadHelper.ThrowIfNotOnUIThread();

        uint uiFrequency = VsSettings.Frequency ?
            FrequencyValues[VsSettings.FrequencyIndex] : 0;

        if (uiFrequency > 0 && HasHistoryFiles)
        {
            //
            // Check to see if it is time to save another version.
            //
            DateTime dtLatest = DateTimeFromFilename(VSHistoryFiles![0].Name);

            TimeSpan tsSecondsSinceLatest = VSFileInfo.LastWriteTime - dtLatest;

            if (tsSecondsSinceLatest.TotalSeconds < uiFrequency)
            {
                VSLogMsg(
                    $"Not time to save. Last version {dtLatest:HH:mm:ss}," +
                    $" next save {dtLatest.AddSeconds(uiFrequency):HH:mm:ss}", Severity.Detail);

                return;
            }
        }

        CreatePath();

        FileInfo fileHistoryFile = BuildHistoryFile();

        if (!fileHistoryFile.Exists)
        {
            SaveAndPurge(fileHistoryFile);
        }
        else if (fileHistoryFile.Length != VSFileInfo.Length)
        {
            Debug.Assert(fileHistoryFile.Length == VSFileInfo.Length,
                string.Format("Existing {0} {1} bytes, expected {2} bytes",
                fileHistoryFile.FullName, fileHistoryFile.Length, VSFileInfo.Length));
        }
        else
        {
            VSLogMsg("VSHistory file exists: " + fileHistoryFile.FullName);
        }
    }

    /// <summary>
    /// Save the current file state to the specified history file
    /// and purge old history files if necessary.
    /// </summary>
    /// <remarks>
    /// This method saves the current file state by creating a
    /// history file and ensures that the history directory does not
    /// exceed the allowed limits by purging older history files.
    /// The files are processed in reverse chronological order (most recent first).
    /// </remarks>
    /// <param name="fileHistoryFile">
    /// The <see cref="FileInfo"/> object representing the target
    /// history file where the current file state will be saved.
    /// </param>
    private void SaveAndPurge(FileInfo fileHistoryFile)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        
        VSLogMsg("Save " + fileHistoryFile.FullName);

        //
        // Copy this as a history file, GZIPing if appropriate.
        //
        ZipIt(VSFileInfo, fileHistoryFile);

        //
        // Get all the files and save them in reverse order (most recent first).
        //
        _VSHistoryFiles = [.. VSHistoryDir.GetFiles(VSHistoryFilenameMask)];
        _VSHistoryFiles.Reverse();

        //
        // Purge history files in case we've exceeded some limit.
        //
        bool bAnyPurged = PurgeHistoryFile(this);

        //
        // Get the filter settings, if any, and get the
        // list of filtered filenames.
        //
        string sFilterPath =
            Path.Combine(VSHistoryDir.FullName, FilterVersions.FilterSettingsName);

        if (File.Exists(sFilterPath))
        {
            FilterVersions filterVersions = new(sFilterPath);

            //
            // If any files were purged, we must force the entire list
            // of files to be filtered again. This is done by setting
            // the highest version timestamp to DateTime.MinValue.
            //
            if (bAnyPurged)
            {
                filterVersions.highestVersion = DateTime.MinValue;
            }

            //
            // Update the list of filtered files. Only new files will
            // be checked (unless bAnyPurged is true).
            //
            FilterVersions.Filter(VSHistoryDir, filterVersions);

            FilteredFilenames.Clear();
            FilteredFilenames.AddRange(filterVersions.FilteredFiles);
        }
    }

    /// <summary>
    /// Save the current file as a VSHistory file.
    /// </summary>
    public void SaveCurrentFile()
    {
        //
        // Quick exit for an empty file.
        //
        if (!VSFileInfo.Exists || VSFileInfo.Length == 0)
        {
            VSLogMsg("Skipping empty file " + VSFileInfo.Name);
            return;
        }

        ThreadHelper.ThrowIfNotOnUIThread();
        
        try
        {
            CreatePath();

            FileInfo fileHistoryFile = BuildHistoryFile();

            //
            // Include this in the list of VSHistory files.
            //
            SaveAndPurge(fileHistoryFile);
        }
        catch (Exception ex)
        {
            VSLogMsg($"Failed to copy to {VSFileInfo.FullName} -- {ex.Message}",
                Severity.Error);
        }
    }
}
