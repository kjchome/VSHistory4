namespace VSHistory;

/// <summary>
/// A row displayed in the VSHistory tool window.
/// </summary>
public class VSHistoryRow
{
    /// <summary>
    /// If true, the Size and Date fields for this row in the tool window will
    /// be bold. Currently, this is only used when reverting a file from a
    /// previous version, in which case the previous version is set Bold to show
    /// where the revert was done.
    /// </summary>
    public bool BoldText { get; set; } = false;

    /// <summary>
    /// State of the checked box.  If 2 boxes are checked, then the
    /// difference between those 2 VS History files will be displayed.
    /// </summary>
    public bool Checked { get; set; } = false;

    /// <summary>
    /// Get the size of the history file.
    /// If the file is compressed, it will have a ".gz" extension and will contain
    /// the size preceding that, e.g., ".cs.82828.gz" or ".cs.82828T.gz".
    /// </summary>
    public long? FileSize => field ??= SizeOfVSHistoryFile(VSHistoryFileInfo);

    /// <summary>
    /// Show the "pretty" date/time for this history file.
    /// </summary>
    public string PrettyWhenSaved => field ??= PrettyDateTime(VSHistoryFileInfo);

    /// <summary>
    /// Actual size on disk.  The history file is compressed, so if it's
    /// greater than the cluster size (typically 4K), this will be some
    /// smaller size reflecting the compression.
    /// </summary>
    public string SizeOnDisk
    {
        get
        {
            if (field == null)
            {
                //
                // Size on disk in KB, where 1 KB = 1024 bytes.
                // Since the size is in clusters, it will always be a multiple of 1024.
                //
                long lSize = GetSizeOnDisk(VSHistoryFileInfo, g_VSControl!.ClusterSize) / 1024;

                field = $"Size on disk: {lSize:N0} KB";
                if (VSHistoryFileInfo.Extension == ".gz")
                {
                    field += " (GZIP)";
                }
            }
            return field;
        }
    } = null;

    /// <summary>
    /// The FileInfo of the VS project file associated with
    /// this VS History file.  Note that it's possible that
    /// the VS project file has been deleted.  That's OK.
    /// </summary>
    public FileInfo VSFileInfo { get; private set; }

    /// <summary>
    /// FileInfo of the VS History file.
    /// </summary>
    public FileInfo VSHistoryFileInfo { get; private set; }

    /// <summary>
    /// The date/time derived from the history filename,
    /// e.g., "2025-04-06_12_05_40_770.cs".  This is used
    /// as the sort field to sort the "When Saved" column.
    /// </summary>
    public DateTime? WhenSaved => field ??= DateTimeFromFilename(VSHistoryFileInfo.FullName);

    public VSHistoryRow(FileInfo fileInfo, FileInfo vsHistoryFile)
    {
        VSHistoryFileInfo = fileInfo;
        VSFileInfo = vsHistoryFile;
    }
}
