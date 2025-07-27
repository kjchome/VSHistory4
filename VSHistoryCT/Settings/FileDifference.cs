namespace VSHistory;

internal class FileDifferenceClass
{
    /// <summary>
    /// ToolTip for ComparisonWindow to confirm window type.
    /// </summary>
    public const string ComparisonWindowToolTip = "[VSHistory Comparison]";

    /// <summary>
    /// Display a difference window in Visual Studio between 2 files.
    ///
    /// The current filename looks like:
    ///     C:\Users\user\ConsoleApp17\Program.cs
    ///
    /// The history filename looks like:
    ///     C:\Users\user\ConsoleApp17\.vshistory\Program.cs\2021-08-07_12_47_18_805.cs
    ///
    /// </summary>
    /// <param name="fLeftFileIn">
    /// The left side of the difference window.  This is a VSHistory file.
    /// It may or may not be compressed.  If so, uncompress to a temporary
    /// file before displaying the difference.
    /// </param>
    /// <param name="fRightFileIn">
    /// The right side of the difference window.  This may be a VSHistory file,
    /// in which case it may or may not be compressed, or the real (current) file.
    /// If compressed, uncompress to a temporary file before displaying the difference.
    /// </param>
    public static void FileDifference(FileInfo fLeftFileIn, FileInfo fRightFileIn)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        VSLogMsg($"Show difference between '{fLeftFileIn.FullName}' " +
            $"and '{fRightFileIn.FullName}'");

        //
        // Verify that the left file is a VSHistory file.
        //
        DateTime dtHistoryDateTime = DateTimeFromFilename(fLeftFileIn.Name);
        if (dtHistoryDateTime == DateTime.MinValue)
        {
            Debug.Assert(dtHistoryDateTime != DateTime.MinValue);
            return;
        }

        //
        // Uncompress the files if necessary.  If the file isn't
        // compressed, the original FileInfo will be returned.
        //
        FileInfo fRightFile = UncompressFile(fRightFileIn);
        FileInfo fLeftFile = UncompressFile(fLeftFileIn);

        try
        {
            string sBaseFilename = Path.GetFileName(fLeftFileIn.DirectoryName);
            string sRightFile = fRightFile.FullName;
            string sLeftFile = fLeftFile.FullName;
            string sRightLabel;
            string sLeftLabel;
            uint diffOptions = 0;

            //
            // Handle the case where there are history files but the project file doesn't exist.
            // This is normal for deleted files that have history files.
            //
            if (!fRightFile.Exists)
            {
                //
                // No "current" file -- just open the history (left) file.
                //
                VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider,
                    sLeftFile, Guid.Empty, out _, out _, out IVsWindowFrame frame);

                //
                // Make this a "Provisional" (preview) window.
                //
                frame.SetProperty((int)__VSFPROPID5.VSFPROPID_IsProvisional, true);

                // KJC Make it read-only?

                return;
            }

            //
            // Set the Temporary flags for the compare window.
            //
            // The OpenComparisonWindow2 method has a convenient option
            // (VSDIFFOPT_XxxxFileIsTemporary) to indicate that the
            // file is a temporary file.  If one or both of our files is
            // a temporary file (i.e., it was decompressed) and this flag
            // isn't specified, it will make a copy of the temporary file
            // as a new temporary file and use that.  Sheesh.
            //
            if (fLeftFile != fLeftFileIn)
            {
                diffOptions |= (uint)__VSDIFFSERVICEOPTIONS.VSDIFFOPT_LeftFileIsTemporary;
            }

            if (fRightFile != fRightFileIn)
            {
                diffOptions |= (uint)__VSDIFFSERVICEOPTIONS.VSDIFFOPT_RightFileIsTemporary;
            }

            //
            // Left label.
            //
            sLeftLabel = sBaseFilename + " as of " + dtHistoryDateTime.ToString("G");

            //
            // Check if the right file is a VSHistory file.
            //
            dtHistoryDateTime = DateTimeFromFilename(fRightFileIn.Name);
            if (dtHistoryDateTime != DateTime.MinValue)
            {
                sRightLabel = sBaseFilename + " as of " + dtHistoryDateTime.ToString("G");
            }
            else
            {
                sRightLabel = sBaseFilename;
            }

            IVsDifferenceService differenceService =
                (IVsDifferenceService)Package
                .GetGlobalService(typeof(SVsDifferenceService));

            if (differenceService == null)
            {
                Debug.Assert(differenceService != null);
                return;
            }

            //
            // If the right file is a project file (which it always should be?),
            // then we want to remove the "\\?\" prefix so that it is recognized
            // as a project file by the difference service.
            //
            if (sRightFile.StartsWith(@"\\?\"))
            {
                sRightFile = sRightFile.Substring(4);
            }

            IVsWindowFrame differenceFrame = differenceService.OpenComparisonWindow2(
                sLeftFile,
                sRightFile,
                "VSHistory of " + sBaseFilename,
                ComparisonWindowToolTip,
                sLeftLabel,
                sRightLabel,
                null,
                null,
                diffOptions);

            //
            // Make this a "Provisional" (preview) window.
            //
            differenceFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_IsProvisional, true);
        }
        finally
        {
            //
            // Delete the left and/or right files if they were temporary.
            //
            if (fLeftFile != fLeftFileIn)
            {
                fLeftFile.Delete();
            }

            if (fRightFile != fRightFileIn)
            {
                fRightFile.Delete();
            }
        }
    }
}
