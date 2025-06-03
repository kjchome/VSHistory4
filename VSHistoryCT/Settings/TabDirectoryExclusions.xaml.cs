using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

/// <summary>
/// Interaction logic for TabDirectoryExclusions.xaml
/// </summary>
public partial class TabDirectoryExclusions : UserControl
{
    /// <summary>
    /// Directories to be excluded from ALL solutions.
    /// </summary>
    public static ObservableCollection<ExcludedDirOrFile> ExcludedDirs { get; set; } = new();

    public static TabDirectoryExclusions? tabDirectoryExclusions = null;

    public TabDirectoryExclusions()
    {
        InitializeComponent();

        tabDirectoryExclusions = this;

#if VSHISTORY_PACKAGE
        //
        // The CrispImage (Help icon) only shows up when running in the extension.
        //
        imgHelp.Visibility = Visibility.Visible;
        btnHelp.Visibility = Visibility.Collapsed;
#else
        imgHelp.Visibility = Visibility.Collapsed;
        btnHelp.Visibility = Visibility.Visible;
#endif
    }

    /// <summary>
    /// This is called when the user has edited a cell in the data grid.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void gridNames_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        //
        // Check if the edit is valid.  If not, show an
        // error message and restore the original value.
        //
        CellEdit(
            sender,
            e,
            ExcludedDirs,
            ExcludedDirOrFile.ExcludedType.Directory,
            _BeforeEditValue);
    }

    /// <summary>
    /// Process the cell edit ending event.
    /// </summary>
    /// <param name="sender">
    /// This is the data grid that is being edited.
    /// </param>
    /// <param name="e">
    /// This is the event args for the cell edit ending event.
    /// </param>
    /// <param name="dirOrFiles">
    /// List of directory or file entries in the data grid.
    /// </param>
    /// <param name="whichType">
    /// Indicates whether this is a directory or file entry.
    /// </param>
    /// <param name="beforeEditValue">
    /// The value of the cell before the edit.
    /// </param>
    public static void CellEdit(
        object sender,
        DataGridCellEditEndingEventArgs e,
        ObservableCollection<ExcludedDirOrFile> dirOrFiles,
        ExcludedDirOrFile.ExcludedType whichType,
        string beforeEditValue)
    {
        //
        // Get the excluded directory or file entry.
        //
        ExcludedDirOrFile? excluded = e.Row.DataContext as ExcludedDirOrFile;
        if (excluded == null)
        {
            //
            // This should never happen.  Throw an exception?
            //
            return;
        }

        //
        // If the entry type is undefined, set it to the type of the entry.
        // This happens when a new row is added to the data grid..
        //
        if (excluded.WhichType == ExcludedDirOrFile.ExcludedType.Undefined)
        {
            excluded.WhichType = whichType;
        }

        //
        // Check if the entry is valid.  If so, we're done.
        //
        if (excluded.IsValid)
        {
            return;
        }

        //
        // Oops.  The entry is invalid.  Show a message box and cancel the edit.
        //
        MessageBox.Show($"The entry '{excluded.Name}' is invalid.  Try again.",
                "Invalid Directory Name", MessageBoxButton.OK, MessageBoxImage.Error);

        //
        // Reset the data grid's items source to null.  This is
        // required to force the data grid to refresh the display.
        //
        Debug.Assert(sender is not null && sender is DataGrid);
        DataGrid dataGrid = (DataGrid)sender!;

        int iRowNumber = e.Row.GetIndex();
        dataGrid.ItemsSource = null;

        //
        // If this is an existing row, set the name back to the original value.
        //
        if (iRowNumber >= 0 && iRowNumber < dirOrFiles.Count)
        {
            dirOrFiles[iRowNumber].Name = beforeEditValue;
        }

        //
        // Set the items source back to the list of directories or files.
        // This will refresh the display and show the original value.
        //
        dataGrid.ItemsSource = dirOrFiles;

        //
        // Cancel the edit.  This will prevent the data grid from
        // committing the edit and updating the display.
        //
        e.Cancel = true;
    }

    private static string _BeforeEditValue = "";

    /// <summary>
    /// This is called when the user begins editing a cell in the data grid.
    /// Save the original value of the cell so we can restore it if the edit is cancelled.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void gridNames_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        ExcludedDirOrFile? excluded = e.Row.DataContext as ExcludedDirOrFile;
        _BeforeEditValue = excluded?.Name ?? "";
    }

    /// <summary>
    /// Display Help.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CrispImage_PreviewMouseLeftButtonDown
        (object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Help.HelpDirectoryExclusions exclusionsHelp = new();
        exclusionsHelp.FontSize = this.FontSize;
        exclusionsHelp.ShowDialog();
    }
}
