using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;

namespace VSHistory;

/// <summary>
/// Interaction logic for TabAllFiles.xaml
/// </summary>
public partial class TabAllFiles : UserControl
{
    /// <summary>
    /// The default string for labInfo.Text as set when designed.
    /// </summary>
    private readonly string? m_DefaultInfoString;

    private bool m_Initializing = true;

    private BitmapSources m_BitmapSources = new();

    public TabAllFiles()
    {
        InitializeComponent();

        //
        // Only one of these should be true.
        //
        Debug.Assert(VsSettings.radOrderByDate ^ VsSettings.radOrderByFile);

        radOrderByDate.IsChecked = VsSettings.radOrderByDate;
        radOrderByFile.IsChecked = VsSettings.radOrderByFile;

        m_DefaultInfoString = labInfo.Content.ToString();

        m_Initializing = false;
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (!m_Initializing)
        {
            VsSettings.radOrderByDate = radOrderByDate.IsChecked == true;
            VsSettings.radOrderByFile = radOrderByFile.IsChecked == true;
        }

        RefreshTreeView();
    }

    private void RefreshTreeView()
    {
        //
        // This is to avoid problems when in design mode.
        //
        if (m_Initializing || AllSolutionFiles == null)
        {
            return;
        }

        //
        // The TreeView should only be enabled if a project is loaded.
        //
        if (AllSolutionFiles.Count == 0)
        {
            labInfo.Content = "There are no project files with VSHistory version files.";
            return;
        }

        labInfo.Content = m_DefaultInfoString;

        if (radOrderByFile.IsChecked == true)
        {
            DisplayTreeByFilename();
        }
        else
        {
            DisplayTreeByDate();
        }
    }

    /// <summary>
    /// Display the TreeView with files ordered by WhenSaved.
    /// </summary>
    private void DisplayTreeByDate()
    {
        List<FileInfo> listAllFiles = new List<FileInfo>();
        treeViewFiles.ItemsSource = null;

        //
        // Get all the history files, keyed by date string.
        //
        foreach (VSHistoryFile vsHistoryFile in AllSolutionFiles!)
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
        List<TreeViewItem> treeViewItems = new();
        TreeViewItem? treeViewItem = null;

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
                treeViewItem = new()
                {
                    Header = sDate,
                    ToolTip = "Files saved " + sDate,
                    Tag = fileInfo
                };

                treeViewItems.Add(treeViewItem);
            }

            string sBaseFilename = Path.GetFileName(fileInfo.DirectoryName);
            long iSize = SizeOfVSHistoryFile(fileInfo);
            string sTitle = string.Format("{0} {1}, {2:N0} bytes", sBaseFilename,
                sThisTime, iSize);

            TreeViewItem child = new()
            {
                ToolTip = fileInfo.FullName,
                Tag = fileInfo
            };

            child.Header = GetHeaderBlock(fileInfo, sTitle);

            treeViewItem?.Items.Add(child);
        }

        treeViewFiles.ItemsSource = treeViewItems;
    }

    /// <summary>
    /// Display the TreeView with files ordered by filename.
    /// </summary>
    private void DisplayTreeByFilename()
    {
        List<TreeViewItem> treeViewItems = new();

        //
        // Load up the TreeView elements.
        //
        foreach (VSHistoryFile vsHistoryFile in
            AllSolutionFiles.Where(f => f.HasHistoryFiles).OrderBy(n => n.VSFileInfo.Name))
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

            TreeViewItem treeViewItem = new()
            {
                ToolTip = thisFile.FullName,
                Tag = thisFile
            };

            treeViewItem.Header = GetHeaderBlock(thisFile, sTitle);

            treeViewItems.Add(treeViewItem);

            foreach (FileInfo child in vsHistoryFile.VSHistoryFiles)
            {
                long iSize = SizeOfVSHistoryFile(child);
                sTitle = string.Format("{0}, {1:N0} bytes", PrettyDateTime(child), iSize);

                TreeViewItem nodeChild = new()
                {
                    Header = sTitle,
                    ToolTip = child.FullName,
                    Tag = child
                };

                treeViewItem.Items.Add(nodeChild);
            }
        }

        treeViewFiles.ItemsSource = null;
        treeViewFiles.ItemsSource = treeViewItems;
    }

    /// <summary>
    /// Build the TextBlock for the header of the TreeViewItem.
    /// This will contain the icon for the file (or a special
    /// TextBlock for certain file types) and the title.
    /// </summary>
    /// <param name="thisFile"></param>
    /// <param name="sTitle"></param>
    /// <returns></returns>
    private TextBlock GetHeaderBlock(FileInfo thisFile, string sTitle)
    {
        //
        // The TextBlock to use for C# files.  This is used in place
        // of the icon for C# files because it's closer to the
        // icon used in the VS IDE.  The color is taken from
        //
        // https://learn.microsoft.com/en-us/visualstudio/extensibility/ux-guidelines/images-and-icons-for-visual-studio?view=vs-2022#visual-studio-languages
        //
        TextBlock CSharpTextBlock = new()
        {
            Text = "C#",
            Foreground = new SolidColorBrush(
                new Color() { A = 255, R = 56, G = 138, B = 52 }),
            Height = SystemParameters.SmallIconHeight,
            Width = SystemParameters.SmallIconWidth,
        };

        TextBlock textBlock = new()
        {
            VerticalAlignment = VerticalAlignment.Center,
        };

        switch (thisFile.Extension.ToLower())
        {
            case ".cs":
                textBlock.Inlines.Add(CSharpTextBlock);
                break;

            default:
                Image image = new();
                image.Source = m_BitmapSources.GetBitmapSource(thisFile.FullName);
                textBlock.Inlines.Add(image);
                break;
        }

        textBlock.Inlines.Add(" " + sTitle);

        return textBlock;
    }

    /// <summary>
    /// A TreeViewItem was double-clicked.  If it has children, toggle the
    /// expanded state of the item.  If not, show a diff to the file.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        //
        // We're handling this.
        //
        e.Handled = true;

        TreeViewItem? selected = treeViewFiles.SelectedItem as TreeViewItem;
        if (selected == null)
        {
            Debug.Assert(false, "Selected item is null");
            return;
        }

        //
        // If the selected node has children, toggle the expanded state of the item.
        //
        if (selected.HasItems)
        {
            selected.IsExpanded = !selected.IsExpanded;
            return;
        }

        //
        // The selected item is a history file.
        // The parent is the project file.
        // Show the difference.
        //
        TreeViewItem? parent = selected.Parent as TreeViewItem;
        if (parent == null)
        {
            Debug.Assert(false, "Parent item is null");
            return;
        }

        FileInfo fileInfo;
        FileInfo parentFileInfo;

        if (radOrderByFile.IsChecked == true &&
            selected.Tag is FileInfo &&
            parent != null &&
            parent.Tag is FileInfo)
        {
            fileInfo = (FileInfo)selected.Tag;
            parentFileInfo = (FileInfo)parent.Tag;
        }
        else if (radOrderByDate.IsChecked == true && selected.Tag is FileInfo)
        {
            //
            // Get the base file.
            //
            fileInfo = (FileInfo)selected.Tag;
            parentFileInfo = GetBaseFileInfo(fileInfo) ??
                throw new ArgumentNullException(fileInfo.FullName);
        }
        else
        {
            Debug.Assert(false, "Can't find parent file info");
            return;
        }

#if VSHISTORY_PACKAGE
        ThreadHelper.ThrowIfNotOnUIThread();
        FileDifferenceClass.FileDifference(fileInfo, parentFileInfo);
#else
        string sMsg = $"Show the difference in\n\n{parentFileInfo.DirectoryName}\n\n between\n\n{parentFileInfo.Name}\n\nand\n\n{fileInfo.Name}";

        MessageBox.Show(sMsg, "Show Diff", MessageBoxButton.OK, MessageBoxImage.Information);
#endif

    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SolutionPath) || AllSolutionFiles.Count == 0)
        {
            return;
        }

        //
        // Make sure we have an up-to-date list of files.
        //
        if (TheSolution == null)
        {
            InitFolderInfo(SolutionPath!);
        }
        else
        {
            ThreadHelper.JoinableTaskFactory.Run(InitSolutionInfoAsync);
        }

        VSLogMsg($"TabAllFiles Loaded: {AllSolutionFiles.Count} files");
        RefreshTreeView();
    }
}
