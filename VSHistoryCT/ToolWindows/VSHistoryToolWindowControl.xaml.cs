using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VSHistory;

public partial class VSHistoryToolWindowControl : UserControl
{
    /// <summary>
    /// The cluster size on the volume where the history file is located.
    /// </summary>
    public uint ClusterSize => (uint)(LatestHistoryFile?.ClusterSize ?? 4096);

    /// <summary>
    /// The latest history file that is currently being
    /// displayed in the tool window.
    /// </summary>
    public VSHistoryFile? LatestHistoryFile { get; set; } = null;

    /// <summary>
    /// The list of VSHistoryFiles that are displayed in the tool window.
    /// </summary>
    public VSHistoryRows VSHistoryRows { get; set; } = new();

    /// <summary>
    /// Constructor for the VSHistoryToolWindowControl.
    /// </summary>
    public VSHistoryToolWindowControl()
    {
        InitializeComponent();

        //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE"); // Testing only

        //
        // Set headers to localized strings.
        // A space is around the strings because using Padding
        // causes ugly white spaces when Dark theme is used.
        //
        colOpen.Header = $" {LocalizedString("Open")} ";
        colDiff.Header = $" {LocalizedString("Diff")} ";
        colSize.Header = $" {LocalizedString("Size")} ";
        colDate.Header = $" {LocalizedString("Date")} ";
    }

    /// <summary>
    /// A checkbox in the tool window was checked.
    /// If there are 2 checkboxes checked then show
    /// the difference between the versions checked.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        List<VSHistoryRow> version_files = VSHistoryRows.Where(x => x.Checked).ToList();

        if (version_files.Count != 2)
        {
            return;
        }

        //
        // Show the difference between the two versions.
        // The Tool Window will be refreshed when the
        // user returns to the original file, so the
        // checkboxes will be cleared.
        //
        // Set the option to make the right file read-only.
        // This is the VSHistory file, so it should be read-only.
        //
        ThreadHelper.ThrowIfNotOnUIThread();
        FileDifferenceClass.FileDifference(
            version_files[0].VSHistoryFileInfo,
            version_files[1].VSHistoryFileInfo,
            true);
    }

    /// <summary>
    /// We got a right-click somewhere in the tool window.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DataGridCell_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // Testing shows that the device is a Mouse regardless of
        // the actual input device, like mouse, touch pad, touch screen, etc.
        // (although I have not tested a Stylus).
        //
        if (LatestHistoryFile != null && e.Device is MouseDevice mouse)
        {
            //
            // If the mouse is over a TextBlock (i.e., Size or Date), get the VSHistoryRow.
            //
            if (mouse.DirectlyOver is TextBlock text && text.DataContext is VSHistoryRow row)
            {
                RevertVersion(row);
            }
        }
    }

    /// <summary>
    /// The user clicked "Diff" on one of the rows in the Tool Window.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Diff_Click(object sender, RoutedEventArgs e)
    {
        VSHistoryRow? row = GetSelectedRow();
        if (row == null)
        {
            return;
        }

        ThreadHelper.ThrowIfNotOnUIThread();
        FileDifferenceClass.FileDifference(
            row.VSHistoryFileInfo,
            row.VSFileInfo);
    }

    /// <summary>
    /// Get the VSHistoryRow of the selected cell.
    /// </summary>
    /// <returns></returns>
    private VSHistoryRow? GetSelectedRow()
    {
        //
        // There should be exactly one selected cell.
        //
        if (gridFiles.SelectedCells.Count != 1)
        {
            //
            // Should never happen.
            //
            VSLogMsg($"Huh? {gridFiles.SelectedCells.Count}", Severity.Error);
            return null;
        }

        //
        // Get the VSHistoryRow of the row that was clicked.
        //
        VSHistoryRow? row = gridFiles.SelectedCells[0].Item as VSHistoryRow;
        Debug.Assert(row != null);

        return row;
    }

    /// <summary>
    /// The user clicked "Open" on one of the rows in the Tool Window.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Open_Click(object sender, RoutedEventArgs e)
    {
        VSHistoryRow? row = GetSelectedRow();
        if (row == null)
        {
            return;
        }

        //
        // Open the VSHistory version in the "preview" tab.
        //
        Documents docs = new();

        ThreadHelper.JoinableTaskFactory.Run(() =>
            docs.OpenInPreviewTabAsync(row!.VSHistoryFileInfo.FullName));
    }

    /// <summary>
    /// Revert the current document to a particular version.
    /// 
    /// 1. The current document must not be modified (dirty).
    /// 2. The selected version cannot be the same as the current document (what's the point?).
    /// 3. The operator is asked to confirm that the current document
    ///    should be reverted to the selected version.
    ///    
    /// </summary>
    /// <param name="row"></param>
    private void RevertVersion(VSHistoryRow row)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // For project files, the Moniker is the path to the file.
        // Visual Studio really doesn't like long file paths like
        // (\\?\C:\...).  Sad.
        //
        string sMoniker = ShortPath(LatestHistoryFile!.FullPath);

        RunningDocumentTable _rdt = new RunningDocumentTable();
        RunningDocumentInfo rdi = _rdt.GetDocumentInfo(sMoniker);

        //foreach (RunningDocumentInfo doc in _rdt)
        //{
        //    Debug.WriteLine($"Cookie {doc.DocCookie,2} {doc.IsDocumentInitialized} Moniker {doc.Moniker} " +
        //        $"Flags {doc.Flags} {(_VSRDTFLAGS)doc.Flags} {(_VSRDTFLAGS3)doc.Flags} {(_VSRDTFLAGS4)doc.Flags}");
        //}

        if (!rdi.IsDocumentInitialized)
        {
            //
            // Not sure we should have gotten here?
            //
            Debug.Assert(rdi.IsDocumentInitialized);
            return;
        }

        if (rdi.IsDirty)
        {
            MessageBox.Show("The currently open file must be saved first.",
                "Cannot Revert", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }

        //
        // Get the version file and the "live" file.
        //
        FileInfo fiVersionFile = row.VSHistoryFileInfo;
        FileInfo fiLiveFile = LatestHistoryFile.VSFileInfo;
        Debug.Assert(fiVersionFile.Exists && fiLiveFile.Exists);

        if (fiVersionFile.Length == fiLiveFile.Length &&
            fiVersionFile.LastWriteTime == fiLiveFile.LastWriteTime)
        {
            MessageBox.Show("This is the same file as the currently open file.  Nothing to do.",
                "Cannot Revert", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }

        string sMsg = $"This will revert {LatestHistoryFile.Name} to the version from\n\n" +
                $"{row.PrettyWhenSaved} ({row.FileSize:N0} bytes)\n\n" +
                "Do you want to revert to this version?";

        MessageBoxResult result = MessageBox.Show(sMsg, "Revert from Version",
            MessageBoxButton.OKCancel, MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
        {
            return;
        }

        //
        // We "revert" to the version file by writing it over the live file.
        //
        try
        {
            using (FileStream fsLive = File.Create(fiLiveFile.FullName))
            {
                using (FileStream fsVersion = fiVersionFile.OpenRead())
                {
                    fsVersion.CopyTo(fsLive);
                }
            }
            
            //
            // Refresh the FileInfo and save the current file as a version.
            //
            fiLiveFile.Refresh();
            LatestHistoryFile.Save();

            //
            // Refresh the versions shown in the Tool Window.
            //
            RefreshVSHistoryWindow(filePath: fiLiveFile.FullName, bForce: true);

            //
            // Highlight the row we just converted from.
            //
            foreach (VSHistoryRow item in VSHistoryRows)
            {
                if (item.VSHistoryFileInfo.FullName == fiVersionFile.FullName)
                {
                    item.BoldText = true;
                    break;
                }
            }

            gridFiles.ItemsSource = null;
            gridFiles.ItemsSource = VSHistoryRows;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to revert from the version file: {ex}",
                "Revert Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }


    }
}
