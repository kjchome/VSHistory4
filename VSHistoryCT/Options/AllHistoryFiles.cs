

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using FontStyle = System.Drawing.FontStyle;

namespace VSHistory;

/// <summary>
/// UserControl to display *all* the VS History files for all project items.
/// </summary>
public partial class AllHistoryFiles : UserControl
{
    /// <summary>
    /// The default string for labInfo.Text as set when designed.
    /// </summary>
    private readonly string m_DefaultInfoString;

    /// <summary>
    /// The list of all solution files with or without VS history files.
    /// Reset this to null when the solution is closed so that a new
    /// list of files will be generated when the solution is opened.
    /// </summary>
    public static SortedSet<VSHistoryFile>? _AllSolutionFiles;

    public bool m_Initializing;

    /// <summary>
    /// Log solutions files, if any, if verbose logging is enabled.
    /// </summary>
    public static void LogAllSolutionFiles()
    {
        if (!VsSettings.LoggingIsVerbose)
        {
            return;
        }

        ThreadHelper.ThrowIfNotOnUIThread();

        if (AllSolutionFiles.Count == 0)
        {
            VSLogMsg("No solution files found.");
            return;
        }

        StringBuilder sb = new();

        //
        // Start with the solution file itself.
        //
        sb.AppendLine($"Solution {SolutionName} {SolutionPath}");

        //
        // List each solution file and the number of history files.
        //
        foreach (VSHistoryFile historyFile in AllSolutionFiles)
        {
            sb.AppendLine($"  {historyFile.Name,-32} {historyFile.NumHistoryFiles}");
        }
        VSLogMsg(sb.ToString(), Severity.Verbose);
    }

    /// <summary>
    /// The list of all solution files with or without VS history files.
    /// </summary>
    public static SortedSet<VSHistoryFile> AllSolutionFiles
    {
        get
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_AllSolutionFiles == null)
            {
                _AllSolutionFiles =
                    ThreadHelper.JoinableTaskFactory.Run(GetAllSolutionFilesAsync);
            }
            return _AllSolutionFiles;
        }

        set
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _AllSolutionFiles?.Clear();
            _AllSolutionFiles = value;
        }
    }

    /// <summary>
    /// The list of all solution files with VS history files.
    /// </summary>
    public static List<VSHistoryFile> AllVSHistoryFiles
    {
        get
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return [.. AllSolutionFiles.Where(static x => x.VSHistoryFiles.Count > 0)];
        }
    }

    /// <summary>
    /// The list of the FileInfos for all solution files with VS history files.
    /// </summary>
    public static List<FileInfo> AllVSHistoryFileInfos
    {
        get
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return [.. AllSolutionFiles
                .Where(static x => x.VSHistoryFiles.Count > 0)
                .Select(f => new FileInfo(f.FullPath))];
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public AllHistoryFiles()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        m_Initializing = true;

        InitializeComponent();

        //
        // Save the default string for labInfo.
        //
        m_DefaultInfoString = labInfo.Text;

        m_Initializing = false;
    }

    /// <summary>
    /// Refresh the display of VS history files.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AllHistoryFiles_Paint(object sender, PaintEventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // The TreeView should only be enabled if a project is loaded.
        //
        treeAllHistoryFiles.Visible = AllVSHistoryFiles.Count > 0;
        if (!treeAllHistoryFiles.Visible)
        {
            labInfo.Text = "There are no project files with VS history files.";
            return;
        }

        treeAllHistoryFiles.SuspendLayout();
        treeAllHistoryFiles.Nodes.Clear();

        labInfo.Text = m_DefaultInfoString;

        if (radOrderByFile.Checked)
        {
            DisplayTreeByFilename();
        }
        else
        {
            DisplayTreeByDate();
        }

        treeAllHistoryFiles.ResumeLayout();
    }

    /// <summary>
    /// Display the TreeView with files ordered by WhenSaved.
    /// </summary>
    private void DisplayTreeByDate()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        List<FileInfo> listAllFiles = new List<FileInfo>();

        //
        // Get all the history files, keyed by date string.
        //
        foreach (VSHistoryFile vsHistoryFile in AllVSHistoryFiles)
        {
            listAllFiles.AddRange(from FileInfo child in vsHistoryFile.VSHistoryFiles
                                  select child);
        }

        //
        // We now have all the FileInfos of the history files.
        // Sort them by date (filename), most recent first.
        //
        listAllFiles.Sort(new VSHistoryFileDateCompare());
        listAllFiles.Reverse();

        string sDate = "";
        TreeNode? node = null;
        foreach (FileInfo fileInfo in listAllFiles)
        {
            //
            // Extract the timestamp from the filename.  Convert "2016-09-17_12_10_26_725"
            // to "2016-09-17 12:10:26.725" to be parsed as a DateTime string.
            //
            DateTime dtLastWrite = DateTimeFromFilename(fileInfo.Name);

            Debug.Assert(dtLastWrite != DateTime.MinValue);

            //
            // Get the date string for this file.
            //
            string sThisDate = PrettyDate(dtLastWrite);
            string sThisTime = PrettyTime(dtLastWrite);

            if (sThisDate != sDate)
            {
                //
                // A new date, a new top-level node.
                //
                sDate = sThisDate;
                node = new TreeNode(sDate)
                {
                    ToolTipText = "Files saved " + sDate
                };
                treeAllHistoryFiles.Nodes.Add(node);
            }

            string sBaseFilename = Path.GetFileName(fileInfo.DirectoryName);
            long iSize = SizeOfVSHistoryFile(fileInfo);
            string sTitle = string.Format("{0} {1}, {2:N0} bytes", sBaseFilename,
                sThisTime, iSize);

            TreeNode nodeChild = new TreeNode(sTitle)
            {
                Tag = fileInfo
            };

            //
            // Get the tag for this file, if any.
            //
            string? sTag = Win32Utilities.ReadVSHistoryTag(fileInfo);
            if (!string.IsNullOrWhiteSpace(sTag))
            {
                //
                // Underline the entry.
                //
                nodeChild.NodeFont = new Font(treeAllHistoryFiles.Font, FontStyle.Underline);
                nodeChild.ToolTipText = sTag;
            }

            node!.Nodes.Add(nodeChild);
        }
    }

    /// <summary>
    /// Display the TreeView with files ordered by filename.
    /// </summary>
    private void DisplayTreeByFilename()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // Load up the TreeView elements.
        //
        foreach (VSHistoryFile vsHistoryFile in AllVSHistoryFiles)
        {
            FileInfo thisFile = vsHistoryFile.VSFileInfo;
            string sTitle;
            if (thisFile.Exists)
            {
                sTitle = string.Format("{0}, {1:N0} bytes, {2:N0} VS History files",
                        thisFile.Name, thisFile.Length, vsHistoryFile.NumHistoryFiles);
            }
            else
            {
                sTitle = string.Format("{0}, (deleted), {1:N0} VS History files",
                        thisFile.Name, vsHistoryFile.NumHistoryFiles);
            }

            TreeNode node = new TreeNode(sTitle)
            {
                Tag = vsHistoryFile,
                ToolTipText = thisFile.FullName
            };

            foreach (FileInfo child in vsHistoryFile.VSHistoryFiles)
            {
                long iSize = VSHistoryUtilities.SizeOfVSHistoryFile(child);
                sTitle = string.Format("{0}, {1:N0} bytes", VSHistoryUtilities.PrettyDateTime(child), iSize);

                TreeNode nodeChild = new TreeNode(sTitle)
                {
                    Tag = child
                };

                //
                // Get the tag for this file, if any.
                //
                string? sTag = Win32Utilities.ReadVSHistoryTag(child);
                if (!string.IsNullOrWhiteSpace(sTag))
                {
                    //
                    // Underline the entry.
                    //
                    nodeChild.NodeFont = new Font(treeAllHistoryFiles.Font, FontStyle.Underline);
                    nodeChild.ToolTipText = sTag;
                }

                node.Nodes.Add(nodeChild);
            }

            treeAllHistoryFiles.Nodes.Add(node);
        }
    }

    private void radOrderByFile_CheckedChanged(object sender, EventArgs e)
    {
        if (m_Initializing || !((RadioButton)sender).Checked)
        {
            return;
        }

        this.Refresh();
    }

    /// <summary>
    /// The user double-clicked a file in the tree.  Display a diff.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TreeAllHistoryFiles_DoubleClick(object sender, EventArgs e)
    {
        //
        // Get the full path to this history file and the current file.
        //
        TreeNode node = treeAllHistoryFiles.SelectedNode;
        TreeNode parent = node.Parent;

        ThreadHelper.ThrowIfNotOnUIThread();

        if (radOrderByFile.Checked &&
            node.Tag is FileInfo info &&
            parent != null &&
            parent.Tag is VSHistoryFile file)
        {
            // TBD VSHistoryUtilities.FileDifference(info, file.VSFileInfo);
        }
        else if (radOrderByDate.Checked && node.Tag is FileInfo fileInfo)
        {
            //
            // The history file path is real_dir\.vshistory\filename\mmm_dd_yy...
            //
            string sBaseFilename = Path.GetFileName(fileInfo.DirectoryName);
            DirectoryInfo dirReal = fileInfo.Directory.Parent.Parent;
            string sCurrentFilePath = Path.Combine(dirReal.FullName, sBaseFilename);

            // TBD VSHistoryUtilities.FileDifference(fileInfo, new FileInfo(sCurrentFilePath));
        }
    }
}
