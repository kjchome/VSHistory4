using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace VSHistory;
/// <summary>
/// Interaction logic for TabFileExclusions.xaml
/// </summary>
public partial class TabFileExclusions : UserControl
{
    /// <summary>
    /// Files to be excluded from ALL solutions.
    /// </summary>
    public static ObservableCollection<ExcludedDirOrFile> ExcludedFiles { get; set; } = new();

    public static TabFileExclusions? tabFileExclusions = null;

    public TabFileExclusions()
    {
        InitializeComponent();

        tabFileExclusions = this;

#if VSHISTORY_PACKAGE
        //
        // The CrispImage (Help icon) only shows up when running in the extension.
        //
        imgHelp.Visibility = System.Windows.Visibility.Visible;
        btnHelp.Visibility = System.Windows.Visibility.Collapsed;
#else
        imgHelp.Visibility = System.Windows.Visibility.Collapsed;
        btnHelp.Visibility = System.Windows.Visibility.Visible;
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
        TabDirectoryExclusions.CellEdit(
            sender,
            e,
            ExcludedFiles,
            ExcludedDirOrFile.ExcludedType.File,
            _BeforeEditValue);
    }

    private string _BeforeEditValue = "";

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
        Help.HelpFileExclusions helpFileExclusions = new();
        helpFileExclusions.FontSize = this.FontSize;
        helpFileExclusions.ShowDialog();
    }
}
