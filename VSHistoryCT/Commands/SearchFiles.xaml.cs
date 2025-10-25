using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

/// <summary>
/// Interaction logic for SearchFiles.xaml
/// </summary>
public partial class SearchFiles : Window
{
    private string SEARCH_APP => "FindStr";

    private string DirPath;

    public SearchFiles(string _directory)
    {
        InitializeComponent();

        DirPath = _directory;
    }

    private void btnGo_Click(object sender, RoutedEventArgs e)
    {
    }

    private void txtCommand_TextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(txtSearch.Text))
        {
            btnGo.IsEnabled = false;
            return;
        }

        btnGo.IsEnabled = true;
        BuildCommand();
    }

    private void BuildCommand()
    {
        if (string.IsNullOrEmpty(txtSearch.Text))
        {
            return;
        }

        string sCommand = SEARCH_APP;
        if (chkIgnoreCase.IsChecked == true)
        {
            sCommand += " /I";
        }

        if (chkRegularExpression.IsChecked == true)
        {
            sCommand += " /E";
        }

        sCommand += $" /C:\"{txtSearch.Text}\"";

        txtCommand.Text = sCommand;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        txtDirectory.Text = DirPath;
        txtOutput.Text = "";
    }

    private void chk_Checked(object sender, RoutedEventArgs e)
    {
        BuildCommand();
    }

    private void btnOpenCmd_Click(object sender, RoutedEventArgs e)
    {
        //
        // The Start command cannot handle a long path like "\\?\C:\...".
        //
        string sPath = DirPath.TrimStart('\\', '?');

        Process procStart = new();
        procStart.StartInfo.FileName = "cmd";
        procStart.StartInfo.Arguments = $"/C Start /D \"{sPath}\" cmd";

        procStart.Start();
    }
}
