using System.Globalization;
using System.Windows;
using System.Windows.Controls;

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
}
