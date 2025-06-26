// Uncomment to log "Here." for all events.
// This is normally too verbose even for verbose logging.
//#define LOG_HERE

using Microsoft.VisualStudio;

namespace VSHistory.Events;

/// <summary>
/// Define some of the events not handled by Community.VisualStudio.ToolKit.
/// </summary>
internal class VSHistoryDocumentEvents :
    IVsRunningDocTableEvents,   // OnBeforeDocumentWindowShow
    IVsRunningDocTableEvents2,  // OnAfterAttributeChangeEx
    IVsRunningDocTableEvents3   // OnBeforeSave
{
    private readonly RunningDocumentTable _rdt;

    /// <summary>
    /// Get the filename from the cookie.
    /// </summary>
    /// <param name="cookie"></param>
    /// <returns></returns>
    private string DocName(uint cookie)
    {
        return Path.GetFileName(_rdt.GetDocumentInfo(cookie).Moniker);
    }

    /// <summary>
    /// Determine whether a file should be excluded from processing.
    /// </summary>
    /// <param name="sMoniker"></param>
    /// <returns></returns>
    private bool IsExcluded(string sMoniker)
    {
        Debug.Assert(sMoniker != null);

        if (VsSettings.Name != SolutionName)
        {
            VSLogMsg($"VsSettings.Name '{VsSettings.Name}' != SolutionName '{SolutionName}'");
        }

        //
        // Combine excluded directories from default settings and this solution's settings.
        //
        List<string> exclusions = [.. DefaultSettings.ExcludedDirs];
        exclusions.AddRange(VsSettings.ExcludedDirs);

        string sDir = Path.GetDirectoryName(sMoniker).ToLower();
        foreach (string dir in exclusions)
        {
            if (sDir.Contains(dir.ToLower()))
            {
                VSLogMsg($"Excluded due to directory match '{dir}'");
                return true;
            }
        }

        //
        // Combine excluded files from default settings and this solution's settings.
        //
        exclusions = [.. DefaultSettings.ExcludedFiles];
        exclusions.AddRange(VsSettings.ExcludedFiles);

        string sName = Path.GetFileName(sMoniker);
        foreach (string pattern in exclusions)
        {
            if (MatchFilename(pattern, sName))
            {
                VSLogMsg($"Excluded due to filename match '{pattern}'");
                return true;
            }
        }

        return false;
    }

    internal VSHistoryDocumentEvents()
    {
        _rdt = new RunningDocumentTable();
        _rdt.Advise(this);
    }

    int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents3.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents2.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents3.OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
    {
        VSLogMsg("Should have called IVsRunningDocTableEvents2?!?", Severity.Error);
        return VSConstants.S_OK;
    }

    /// <summary>
    /// Handle a possible file rename.
    /// </summary>
    /// <param name="docCookie"></param>
    /// <param name="grfAttribs"></param>
    /// <param name="pHierOld"></param>
    /// <param name="itemidOld"></param>
    /// <param name="pszMkDocumentOld"></param>
    /// <param name="pHierNew"></param>
    /// <param name="itemidNew"></param>
    /// <param name="pszMkDocumentNew"></param>
    /// <returns></returns>
    int IVsRunningDocTableEvents2.OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs,
        IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld,
        IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (grfAttribs == (uint)__VSRDTATTRIB.RDTA_MkDocument)
        {
            VSHistoryFile vsHistoryFile = new VSHistoryFile(pszMkDocumentOld);
            if (vsHistoryFile.HasHistoryFiles)
            {
                vsHistoryFile.Rename(pszMkDocumentNew);
            }
        }
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents3.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents2.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents3.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents2.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif

        ThreadHelper.ThrowIfNotOnUIThread();

        RunningDocumentInfo rdi = _rdt.GetDocumentInfo(docCookie);
        string sMoniker = rdi.Moniker;
        string sPath = LongPath(sMoniker);

        try
        {
            //
            // Create a VSHistory file from the current file and save it.
            //
            VSHistoryFile vsHistoryFile = new VSHistoryFile(sPath);
            vsHistoryFile.SaveCurrentFile();

            RefreshVSHistoryWindow(sMoniker, true);
        }
        catch (Exception e)
        {
            VSLogMsg($"Exception saving {DocName(docCookie)} : {e}", Severity.Error);
        }

        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents3.OnAfterSave(uint docCookie)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents2.OnAfterSave(uint docCookie)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        string file = _rdt.GetDocumentInfo(docCookie).Moniker;
        RefreshVSHistoryWindow(file);

        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents3.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents2.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents3.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents2.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
    {
#if LOG_HERE
        VSLogMsg("Here. " + DocName(docCookie), Severity.Verbose);
#endif
        return VSConstants.S_OK;
    }

    /// <summary>
    /// A file is about to be saved.  If it has been changed (i.e., "dirty"),
    /// save the old file as a VS History file.
    /// </summary>
    /// <param name="docCookie"></param>
    /// <returns></returns>
    int IVsRunningDocTableEvents3.OnBeforeSave(uint docCookie)
    {
        if (!VsSettings.Enabled)
        {
            return VSConstants.S_OK;
        }
        ThreadHelper.ThrowIfNotOnUIThread();

        RunningDocumentInfo rdi = _rdt.GetDocumentInfo(docCookie);
        string sMoniker = rdi.Moniker;
        string sFilename = Path.GetFileName(sMoniker);
        VSLogMsg("Preparing to save " + sFilename, Severity.Info);

        FileInfo fileInfo = new(sMoniker);
        if (!fileInfo.Exists || fileInfo.Length == 0)
        {
            VSLogMsg("File is empty or non-existent: " + sMoniker);
            return VSConstants.S_OK;
        }

        //
        // If this file is excluded, skip it.
        //
        if (IsExcluded(sMoniker))
        {
            return VSConstants.S_OK;
        }

        //
        // Determine whether the file was modified (dirty).
        //
        bool bSave;

        //
        // Check to see if this is document and it has changed.
        // IsDocDataDirty only works in VS 2022 17.4 and higher.
        //
        if (rdi.DocData is IVsPersistDocData docData)
        {
            int iOK = docData.IsDocDataDirty(out int isDirty);
            bSave = iOK == VSConstants.S_OK && isDirty != 0;
        }
        else
        {
            //
            // A non-document file, like vcsproj or sln.  Save it.
            //
            bSave = true;
        }

        if (bSave)
        {
            try
            {
                VSHistoryFile vsHistoryFile = new VSHistoryFile(sMoniker);
                vsHistoryFile.Save();

                RefreshVSHistoryWindow(sMoniker, true);
            }
            catch (Exception e)
            {
                VSLogMsg($"Exception saving {sFilename} : {e}", Severity.Error);
            }
        }

        return VSConstants.S_OK;
    }
}
