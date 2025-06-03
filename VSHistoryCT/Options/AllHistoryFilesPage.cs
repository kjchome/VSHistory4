using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace VSHistory;


/// <summary>
/// DialogPage to display *all* history files.
/// </summary>
[ComVisible(true)]
[Guid(PackageGuids.AllFilesString)]
public class AllHistoryFilesPage : DialogPage
{
    private readonly AllHistoryFiles m_AllHistoryFiles = new();

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    protected override IWin32Window Window
    {
        get
        {
            return m_AllHistoryFiles;
        }
    }

    /// <summary>
    /// Getter/Settor for the radOrderByDate CheckBox.
    /// </summary>
    public bool OrderByDate
    {
        get
        {
            return m_AllHistoryFiles.radOrderByDate.Checked;
        }

        set
        {
            //
            // This happens when settings are restored from the registry.
            //
            m_AllHistoryFiles.m_Initializing = true;
            m_AllHistoryFiles.radOrderByDate.Checked = value;
            m_AllHistoryFiles.m_Initializing = false;
        }
    }
}
