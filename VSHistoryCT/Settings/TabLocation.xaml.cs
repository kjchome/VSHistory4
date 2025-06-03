using System.Windows;
using System.Windows.Forms;

using UserControl = System.Windows.Controls.UserControl;

namespace VSHistory;

/// <summary>
/// Interaction logic for TabLocation.xaml
/// </summary>
public partial class TabLocation : UserControl
{
    public static TabLocation? tabLocation { get; private set; } = null;

    public TabLocation()
    {
        InitializeComponent();

        tabLocation = this;

        txtAppDataPath.Text = VS_LocalBaseDir;

        DataContext = VsSettings;

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

    private void btnBrowse_Click(object sender, RoutedEventArgs e)
    {
        using FolderBrowserDialog dialog = new();

        DialogResult result = dialog.ShowDialog();
        if (result != DialogResult.OK)
        {
            return;
        }

        //
        // Convert the path to a long path ("\\?\C:\...") and save it.
        //
        txtCustomPath.Text = LongPath(dialog.SelectedPath);
    }

    /// <summary>
    /// Display Help.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CrispImage_PreviewMouseLeftButtonDown
        (object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Help.HelpChooseLocation helpChooseLocation = new();
        helpChooseLocation.FontSize = this.FontSize;
        helpChooseLocation.ShowDialog();
    }
}
