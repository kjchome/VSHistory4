using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

/// <summary>
/// Interaction logic for TabLogging.xaml
/// </summary>
public partial class TabLogging : UserControl
{
    public TabLogging()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        LogFileDisplay();
    }

    private void LogFileDisplay()
    {
        FileInfo fi = new(VS_LogFilePath);
        if (fi.Exists)
        {
            txtLogFilePath.Text = fi.FullName;
            string sSize = FormatSize(fi.Length);
            txtLogFileSize.Text = $"Log file size: {fi.Length:N0} ({sSize})";
        }
        else
        {
            txtLogFilePath.Text = "Log file does not exist.";
            txtLogFileSize.Text = "";
        }
    }

    private void btnOpenLog_Click(object sender, RoutedEventArgs e)
    {
        FileInfo fi = new(VS_LogFilePath);
        if (fi.Exists)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fi.FullName,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        else
        {
            MessageBox.Show("Log file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnTruncate_Click(object sender, RoutedEventArgs e)
    {
        const int OneMB = 1024 * 1024;

        FileInfo fi = new(VS_LogFilePath);
        if (!fi.Exists)
        {
            MessageBox.Show("Log file does not exist.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (fi.Length <= OneMB)
        {
            MessageBox.Show("Log file is too small to truncate.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        //
        // Lock the log file to truncate it.
        //
        using Mutex mutex = new(false, VSLogMutexName);
        mutex.WaitOne();

        try
        {
            using FileStream fs =
                fi.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            //
            // Read the file into a byte array and write the last 1 MB back to the file.
            //
            byte[] bOut = new byte[fs.Length];

            fs.Read(bOut, 0, bOut.Length);
            fs.SetLength(0);
            fs.Write(bOut, bOut.Length - OneMB, OneMB);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to truncate the log file: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            mutex.ReleaseMutex();
            LogFileDisplay();
        }
    }
}
