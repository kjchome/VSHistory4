using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;

using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace VSHistory;

public class VSHistoryRows : ObservableCollection<VSHistoryRow>
{
}

public class VSHistoryToolWindow : BaseToolWindow<VSHistoryToolWindow>
{
    public override Type PaneType => typeof(Pane);

    /// <summary>
    /// Refresh the contents of the VS History tool window with the specified project file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="bForce">
    /// If true, force refresh regardless of whether
    /// or not we think the file has changed.
    /// </param>
    public static void RefreshVSHistoryWindow(string? filePath = null, bool bForce = false)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (null == g_VSControl)
        {
            return;
        }

        //
        // Update the window colors to match the current theme.
        //
        UpdateWindowColors(g_VSControl!);

        if (string.IsNullOrEmpty(SolutionName))
        {
            VSLogMsg("No solution name", Severity.Verbose);
        }

        //
        // If a path wasn't specified, get the currently active
        // document, if any.
        //
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = GetActiveDocument();
        }

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            VSLogMsg("No active document", Severity.Detail);
            return;
        }

        //
        // If there's a current file, has it changed?
        //
        FileInfo fiNewFile = new FileInfo(LongPath(filePath!));

        if (!bForce && g_VSControl.LatestHistoryFile != null)
        {
            FileInfo fiLatest = g_VSControl.LatestHistoryFile.VSFileInfo;

            if (fiNewFile.FullName == fiLatest.FullName &&
                fiNewFile.LastWriteTimeUtc == fiLatest.LastWriteTimeUtc)
            {
                //
                // This gets annoying...
                //
                //VSLogMsg($"Don't refresh the same file {fiNewFile.Name}", Severity.Verbose);
                return;
            }
        }

        VSHistoryFile vsHistoryFile = new VSHistoryFile(fiNewFile);

        g_VSControl.LatestHistoryFile = vsHistoryFile;

        VSLogMsg($"Refresh {vsHistoryFile.VSFileInfo.Name}", Severity.Detail);

        //
        // Update the tool window.
        //
        SetToolWindowCaption(vsHistoryFile);

        //
        // Remove all previous history files and build a new list.
        //
        g_VSControl.VSHistoryRows.Clear();

        if (vsHistoryFile.HasHistoryFiles)
        {
            foreach (FileInfo fileInfo in vsHistoryFile.VSHistoryFiles)
            {
                //
                // Exclude any files that have been filtered out.
                // They will have the filter suffix ("-") on the filename.
                //
                string sNameOnly = Path.GetFileNameWithoutExtension(fileInfo.Name);
                if (!sNameOnly.EndsWith(FilterVersions.FilterSuffixStr))
                {
                    VSHistoryRow newRow = new VSHistoryRow(fileInfo, vsHistoryFile.VSFileInfo);
                    g_VSControl.VSHistoryRows.Add(newRow);
                }
            }
        }

        DataGrid dg = g_VSControl.gridFiles;
        dg.ItemsSource = null;
        dg.ItemsSource = g_VSControl.VSHistoryRows;
    }

    /// <summary>
    /// Update the colors on the VSHistory control and the embedded grid.
    /// </summary>
    /// <param name="control"></param>
    public static void UpdateWindowColors(VSHistoryToolWindowControl control)
    {
        //
        // Update the window colors to match the current theme.
        //
        Color clrBackground =
            VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);

        Color clrForeground =
            VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);

        Color clrButtonForeground =
            VSColorTheme.GetThemedColor(EnvironmentColors.PanelHyperlinkColorKey);

        System.Windows.Media.Color background = System.Windows.Media.Color.FromArgb
            (clrBackground.A, clrBackground.R, clrBackground.G, clrBackground.B);

        System.Windows.Media.Color foreground = System.Windows.Media.Color.FromArgb
            (clrForeground.A, clrForeground.R, clrForeground.G, clrForeground.B);

        System.Windows.Media.Color buttonForeground = System.Windows.Media.Color.FromArgb
            (clrButtonForeground.A, clrButtonForeground.R, clrButtonForeground.G, clrButtonForeground.B);

        control.gridFiles.Background = new SolidColorBrush(background);
        control.gridFiles.Foreground = new SolidColorBrush(foreground);

        control.Resources["cellBackground"] = new SolidColorBrush(background);
        control.Resources["cellForeground"] = new SolidColorBrush(foreground);
        control.Resources["buttonForeground"] = new SolidColorBrush(buttonForeground);
        control.Resources["buttonBackground"] = new SolidColorBrush(background);
    }

    /// <summary>
    /// Set the caption of the Tool Window to indicate
    /// the number of VSHistory files are available.
    /// </summary>
    /// <param name="vsFile"></param>
    public static void SetToolWindowCaption(VSHistoryFile vsFile)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // Nothing to do if we don't have a window pane.
        //
        if (g_VSToolWindowPane == null)
        {
            VSLogMsg("No tool window pane", Severity.Warning);
            return;
        }

        string sCaption;

        //
        // If there are some VS History files filtered out,
        // show them in the caption, "7/10 VS History Files for ...".
        //
        if (vsFile.NumFiltered > 0)
        {
            int iUnfiltered = vsFile.NumHistoryFiles - vsFile.NumFiltered;

            sCaption = $"{iUnfiltered:N0}/{vsFile.NumHistoryFiles:N0} " +
                $"VS History Files for {vsFile.Name}";
        }
        else
        {
            sCaption = $"{vsFile.NumHistoryFiles:N0} VS History Files for {vsFile.Name}";
        }

        g_VSToolWindowPane.Caption = sCaption;

        VSLogMsg($"Set caption to '{sCaption}'", Severity.Detail);
    }

    public override async Task<FrameworkElement> CreateAsync
        (int toolWindowId, CancellationToken cancellationToken)
    {
        VSLogMsg("Create Tool Window", Severity.Detail);
        return await Task.FromResult(new VSHistoryToolWindowControl());
    }

    public override string GetTitle(int toolWindowId) => "";

    /// <summary>
    /// The pane has been set for the tool window.  Save it.
    /// </summary>
    /// <param name="pane"></param>
    /// <param name="toolWindowId"></param>
    public override void SetPane(ToolWindowPane pane, int toolWindowId)
    {
        base.SetPane(pane, toolWindowId);

        ThreadHelper.ThrowIfNotOnUIThread();

        g_VSToolWindowPane = pane;
        g_VSToolWindowPane.Caption = "VSHistory";
        g_VSControl = (VSHistoryToolWindowControl)pane.Content;

        VSLogMsg("Got VSHistory ToolWindowPane", Severity.Detail);
    }

    [Guid(PackageGuids.VSHistoryWindowPaneString)]
    internal class Pane : ToolkitToolWindowPane
    {
        public Pane()
        {
            BitmapImageMoniker = KnownMonikers.History;
            ToolBar = new CommandID(PackageGuids.VSHistory, PackageIds.VSHistoryToolbar);
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();

            VSLogMsg("Tool Window Created", Severity.Detail);

            RefreshVSHistoryWindow();

            //
            // If a solution is open (and verbose logging is enabled),
            // display the solution files.
            //
            ThreadHelper.ThrowIfNotOnUIThread();
            LogAllSolutionFiles();
        }
    }
}
