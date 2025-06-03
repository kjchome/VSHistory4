using System.Drawing;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using static VSHistoryShared.VSHistoryFile;

namespace VSHistoryShared;

/// <summary>
/// Class to build BitmapSource objects from file extensions.
/// </summary>
public class BitmapSources
{
    /// <summary>
    /// Dictionary of file extensions to BitmapSource objects.
    /// </summary>
    private Dictionary<string, BitmapSource> sourcesByExt = new();

    /// <summary>
    /// Get a BitmapSource from an Icon.
    /// </summary>
    /// <param name="icon"></param>
    /// <returns></returns>
    public BitmapSource GetBitmapSource(Icon icon) => GetBitmapSource(icon.Handle);

    public BitmapSource GetBitmapSource(IntPtr hIcon) =>
        Imaging.CreateBitmapSourceFromHIcon(
            hIcon, Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(
                (int)SystemParameters.SmallIconWidth,
                (int)SystemParameters.SmallIconHeight));

    /// <summary>
    /// Get the BitmapSource based on the file extension.
    /// </summary>
    /// <param name="sFullName"></param>
    /// <returns></returns>
    public BitmapSource GetBitmapSource(string sFullName)
    {
        BitmapSource bitmapSource;

        //
        // Use the file extension in upper case to avoid duplications.
        //
        string sExt = Path.GetExtension(sFullName).ToUpper();

        if (sExt == ".EXE")
        {
            //
            // This is a .exe file.  Check to see if it has its own icon.
            //
            try
            {
                bitmapSource = GetBitmapSource(Icon.ExtractAssociatedIcon(sFullName));
            }
            catch
            {
                //
                // Nope -- use the default icon.
                //
                bitmapSource = GetBitmapSource(SystemIcons.Application);
            }

            return bitmapSource;
        }

        //
        // Check to see if we already have this extension.
        //
        if (sourcesByExt.TryGetValue(sExt, out bitmapSource))
        {
            return bitmapSource;
        }

        //
        // Try to get the file icon.
        //
        SHFILEINFO shinfo = new();
        SHGFI uFlags =
            SHGFI.TYPENAME |
            SHGFI.USEFILEATTRIBUTES |
            SHGFI.ICON |
            SHGFI.LARGEICON;

        nint n = SHGetFileInfo(
            sExt,
            FileAttributes.Normal,
            ref shinfo,
            (uint)Marshal.SizeOf(shinfo),
            uFlags);

        if (n == 1)
        {
            if (shinfo.hIcon != IntPtr.Zero)
            {
                bitmapSource = GetBitmapSource(shinfo.hIcon);
            }
            else
            {
                //
                // Use generic result for no extension.
                //
                bitmapSource = GetBitmapSource(SystemIcons.Question);
            }
        }

        //
        // Save this BitmapSource for later use.
        //
        sourcesByExt.Add(sExt, bitmapSource);

        return bitmapSource;
    }
}

/// <summary>
/// Generic static functions used by the VS History package.
/// </summary>
public static class VSHistoryUtilities
{
    /// <summary>
    /// Match a filename to a wildcard pattern.
    /// </summary>
    /// <remarks>
    ///
    /// https://github.com/microsoft/referencesource/blob/master/System/services/io/system/io/PatternMatcher.cs#L114
    ///
    /// StrictMatchPattern supports extended wildcards:
    ///
    ///  ~* is DOS_STAR, ~? is DOS_QM, and ~. is DOS_DOT
    ///
    ///  * matches 0 or more characters.
    ///
    ///  ? matches exactly 1 character.
    ///
    ///  DOS_STAR matches 0 or more characters until encountering and matching
    ///     the final . in the name.
    ///
    ///  DOS_QM matches any single character, or upon encountering a period or
    ///     end of name string, advances the expression to the end of the
    ///     set of contiguous DOS_QMs.
    ///
    ///  DOS_DOT matches either a . or zero characters beyond name string.
    ///
    /// </remarks>
    private class PatternMatcher
    {
        public delegate bool StrictMatchPatternDelegate(string expression, string name);

        public StrictMatchPatternDelegate StrictMatchPattern;

        private static Type patternMatcherType =
            typeof(FileSystemWatcher).Assembly.GetType("System.IO.PatternMatcher");

        private static MethodInfo patternMatchMethod = patternMatcherType.GetMethod(
                "StrictMatchPattern", BindingFlags.Static | BindingFlags.Public);

        public PatternMatcher()
        {
            StrictMatchPattern = (expression, name) =>
                (bool)patternMatchMethod.Invoke(null, new object[] { expression, name });
        }
    }

    private static PatternMatcher? patternMatcher = null;

    /// <summary>
    /// Match a filename to a wildcard pattern.
    /// </summary>
    /// <param name="sPattern">
    /// The pattern to match against.  This may contain wildcards.
    /// </param>
    /// <param name="sFilename">
    /// The filename to match.  This may NOT contain wildcards.
    /// </param>
    /// <returns></returns>
    public static bool MatchFilename(string sPattern, string sFilename)
    {
        //
        // Check for a wildcard pattern.
        //
        if (sPattern.IndexOfAny(['*', '?']) >= 0)
        {
            patternMatcher ??= new();

            return patternMatcher.StrictMatchPattern(sPattern.ToLower(), sFilename.ToLower());
        }

        //
        // No wildcards -- just check for equality.
        //
        return string.Compare(sPattern, sFilename, true) == 0;
    }

    /// <summary>
    /// Extract the timestamp from the filename, e.g., convert "2016-09-17_12_10_26_725" to
    /// a DateTime equivalent of "2016-09-17 12:10:26.725".
    /// </summary>
    /// <param name="sFilenameIn">
    /// Filename to be parsed.  Expected to be in the standard VS History filename format.
    /// </param>
    /// <returns>
    /// DateTime extracted from the filename, or DateTime.MinValue if not successful.
    /// </returns>
    public static DateTime DateTimeFromFilename(string sFilenameIn)
    {
        string sFilename = Path.GetFileNameWithoutExtension(sFilenameIn);
        DateTime dtLastWrite = DateTime.MinValue;

        if (sFilename.Length >= VSHistoryTimestampFormat.Length &&
            sFilename[10] == '_' &&
            sFilename[13] == '_' &&
            sFilename[16] == '_' &&
            sFilename[19] == '_')
        {
            string sParseable =
                sFilename.Substring(0, 10) + " " +
                sFilename.Substring(11, 8).Replace('_', ':') + "." +
                sFilename.Substring(20, 3);

            DateTime.TryParse(sParseable, out dtLastWrite);
        }

        return dtLastWrite;
    }

    /// <summary>
    /// Build a path using the "\\?\" prefix
    /// to support long (>260 character) paths.
    /// </summary>
    /// <param name="sPath">
    /// The path to get the long-path prefix.
    /// </param>
    /// <returns>/// </returns>
    public static string LongPath(string sPath)
    {
        const string Prefix = @"\\?\";

        //
        // In case it's already there...
        //
        if (sPath.StartsWith(Prefix))
        {
            return sPath;
        }

        //
        // Path.GetFullPath already handles long (>260) path names.
        // It will also canonicalize it, resolving "..\", ".\", etc.
        //
        string sFullPath = Path.GetFullPath(sPath);

        //
        // The most common case of "C:\..."
        //
        if (sFullPath.Length > 2 && char.IsLetter(sFullPath[0]) && sFullPath[1] == ':')
        {
            return Prefix + sFullPath;
        }

        //
        // Check for a UNC path.
        //
        if (sFullPath.StartsWith(@"\\"))
        {
            try
            {
                Uri uri = new Uri(sFullPath);
                if (uri.IsFile && uri.IsUnc)
                {
                    //
                    // Change \\server\share\dir to \\?\UNC\server\share\dir
                    //
                    return Prefix + "UNC" + sFullPath.Substring(1);
                }
            }
            catch
            {
            }
        }

        //
        // Shouldn't get here?
        //
        return sFullPath;
    }

    /// <summary>
    /// Build a "pretty" date string for a VS history file.
    /// The filename should be in the form "2016-12-17_12_10_26_725.ext".
    /// The resulting string will be something like "Yesterday, 12/17/2016".
    /// </summary>
    /// <param name="dtLastWrite"></param>
    /// <returns></returns>
    public static string PrettyDate(DateTime dtLastWrite)
    {
        DateTime dtToday = DateTime.Now.Date;
        DateTime dtYesterday = dtToday.AddDays(-1);

        VS_Settings settings = VsSettings;
        string? sDay = null;

        // For debugging (keep consistent with PrettyTime)
        //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
        //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

        //
        // Get the culture settings depending on selected format.
        //
        CultureInfo culture = (settings.Date_Long || settings.Date_Short)
            ? CultureInfo.CurrentUICulture : CultureInfo.CurrentCulture;

        //
        // The Long options change the day to Yesterday or Today if appropriate.
        //
        if (settings.Date_Long || settings.Date_LongCurrent)
        {
            //
            // Get the day of the timestamp.
            //
            DateTime dtLastWriteDay = dtLastWrite.Date;

            //
            // Include the day, but substitute "Today" or "Yesterday" if appropriate.
            //
            if (dtLastWriteDay == dtToday)
            {
                sDay = LocalizedString("Today", culture);
            }
            else if (dtLastWriteDay == dtYesterday)
            {
                sDay = LocalizedString("Yesterday", culture);
            }
            else
            {
                sDay = dtLastWrite.ToString("dddd", culture);
            }

            return string.Format("{0}, {1}", sDay, dtLastWrite.ToString("d", culture));
        }

        if (settings.Date_Short || settings.Date_ShortCurrent)
        {
            // Wed 12/30/2020 15:15:22
            return string.Format("{0} {1}",
                dtLastWrite.ToString("ddd", culture),
                dtLastWrite.ToString("d", culture));
        }

        if (settings.Date_ISO)
        {
            // 2020-30-12 15:15:22
            return dtLastWrite.ToString("yyyy-MM-dd");
        }
        else if (settings.Date_ISO_UT)
        {
            // 2020-30-12 23:15:22Z
            DateTime dtLastWriteUTC = dtLastWrite.ToUniversalTime();
            return dtLastWriteUTC.ToString("yyyy-MM-dd");
        }

        // Something is broken...
        return "";
    }

    /// <summary>
    /// Build a "pretty" date/time string for a VS history file.
    /// The filename should be in the form "2016-12-17_12_10_26_725.ext".
    /// The resulting string will be something like "Yesterday, 12/17/2016 12:10:26 PM".
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    public static string PrettyDateTime(FileInfo fileInfo)
    {
        //
        // Extract the timestamp from the filename rather than LastWriteTime
        // in case it was copied or saved/restored.  Convert "2016-09-17_12_10_26_725"
        // to "2016-09-17 12:10:26.725" to be parsed as a DateTime string.
        //
        return PrettyDateTime(DateTimeFromFilename(fileInfo.Name));
    }

    public static string PrettyDateTime(DateTime dtLastWrite)
    {
        return PrettyDate(dtLastWrite) + " " + PrettyTime(dtLastWrite);
    }

    /// <summary>
    /// Build a "pretty" time string for a VS history file.
    /// The filename should be in the form "2016-12-17_12_10_26_725.ext".
    /// The resulting string will be something like "12:10:26 PM".
    /// </summary>
    /// <param name="dtLastWrite"></param>
    /// <returns></returns>
    public static string PrettyTime(DateTime dtLastWrite)
    {
        VS_Settings settings = VsSettings;

        if (settings.Date_Long ||
            settings.Date_LongCurrent ||
            settings.Date_Short ||
            settings.Date_ShortCurrent)
        {
            // for debugging (keep consistent with PrettyDate)
            //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
            //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            //
            // Get the culture settings depending on selected format.
            //
            CultureInfo culture = (settings.Date_Long || settings.Date_Short)
                ? CultureInfo.CurrentUICulture : CultureInfo.CurrentCulture;

            // Wednesday, 12/30/2020 3:15:22 PM
            return dtLastWrite.ToString("T", culture);
        }

        if (settings.Date_ISO_UT)
        {
            // 2020-30-12 23:15:22Z
            DateTime dtLastWriteUTC = dtLastWrite.ToUniversalTime();
            return dtLastWriteUTC.ToString("HH:mm:ssZ");
        }

        Debug.Assert(settings.Date_ISO);
        // settings.Date_ISO   2020-30-12 15:15:22
        return dtLastWrite.ToString("HH:mm:ss");
    }

    /// <summary>
    /// Get the size of a VS history file.
    /// If the file is compressed, it will have a ".gz" extension
    /// and will contain the size preceding that, e.g., ".cs.82828.gz".
    /// </summary>
    /// <param name="fsInfo"></param>
    /// <returns>
    /// For a compressed (.gz) file, the size extracted from the filename.
    /// For an uncompressed file, the file length.
    /// </returns>
    public static long SizeOfVSHistoryFile(FileInfo fileInfo)
    {
        //
        // There should only be 2 cases: <timestamp>.<ext> and <timestamp>.<ext>.<size>.gz
        //
        string[] elements = fileInfo.Name.Split('.');
        if (elements.Length == 4 && elements[3] == "gz")
        {
            //
            // The 3rd part has the uncompressed file size.
            //
            return long.Parse(elements[2]);
        }
        else
        {
            return fileInfo.Length;
        }
    }

    /// <summary>
    /// If a file is compressed (ends with ".gz"), uncompress it.
    /// </summary>
    /// <param name="fileIn"></param>
    /// <returns>
    /// The FileInfo of the uncompressed file.
    /// If the file was compressed, this is a temporary file that should be deleted.
    /// If the file was not compressed, this is the FileInfo passed in.
    /// </returns>
    public static FileInfo UncompressFile(FileInfo fileIn)
    {
        if (fileIn.Extension != ".gz" || !fileIn.Exists)
        {
            return fileIn;
        }

        //
        // The file is compressed.  Uncompress to a temporary file for display purposes.
        //
        string sTempFile = Path.GetTempFileName();

        //
        // Create a FileInfo object to set the file's attributes.
        //
        // Set the Attribute property of this file to Temporary.
        // Although this is not completely necessary, .NET is able to
        // optimize the use of Temporary files by keeping them cached in memory.
        //
        FileInfo fileInfo = new(sTempFile)
        {
            Attributes = FileAttributes.Temporary
        };

        using (FileStream fTempFile = fileInfo.OpenWrite())
        {
            using FileStream sourceFile = fileIn.OpenRead();
            using GZipStream input = new(sourceFile, CompressionMode.Decompress);

            input.CopyTo(fTempFile);

            VSLogMsg(string.Format(
                "Decompressed {0} {1:N0} bytes to {2} {3:N0} bytes",
                fileIn.Name,
                sourceFile.Length,
                Path.GetFileName(sTempFile),
                fTempFile.Length));
        }
        return fileInfo;
    }

    /// <summary>
    /// Compress a file if target ends with ".gz", else just copy it.
    /// </summary>
    /// <param name="fiFrom"></param>
    /// <param name="fiTo"></param>
    public static void ZipIt(FileInfo fiFrom, FileInfo fiTo)
    {
        if (fiTo.Name.EndsWith(".gz"))
        {
            // GZip the file.
            using FileStream sourceFile = fiFrom.OpenRead();
            using FileStream destinationFile = fiTo.OpenWrite();
            using GZipStream output = new(destinationFile, CompressionMode.Compress);

            sourceFile.CopyTo(output);
        }
        else
        {
            //
            // The file is too small to compress or GZIP is disabled -- just copy it.
            //
            fiFrom.CopyTo(fiTo.FullName);
        }
    }

    /// <summary>
    /// Get the FileInfo for the base file (the VS Project file)
    /// associated with a VS History file.
    ///
    /// For example, given a VS History file
    /// "C:\SomeDir\ProjFile.ext\.vshistory\ProjFile.ext\2016-12-17_12_10_26_725.ext"
    /// return the FileInfo for "C:\SomeDir\ProjFile.ext".
    /// </summary>
    /// <param name="vsHistoryFileInfo"></param>
    /// <returns></returns>
    public static FileInfo? GetBaseFileInfo(FileInfo fileInfo)
    {
        try
        {
            //
            // The filename of the base file is the directory name.
            //
            string sBaseFilename = Path.GetFileName(fileInfo.DirectoryName) ??
                throw new ArgumentNullException(nameof(fileInfo.DirectoryName));

            //
            // The second-level directory must be ".vshistory".
            //
            if (fileInfo.Directory.Parent.Name != VSHistoryDirName)
            {
                throw new ArgumentException($"Parent directory must be '{VSHistoryDirName}' {fileInfo.FullName}");
            }

            //
            // The directory of the base file is the grandparent of the history file.
            //
            DirectoryInfo dirReal = fileInfo.Directory.Parent.Parent ??
                throw new ArgumentException($"Null grandparent: {fileInfo.FullName}");

            //
            // Combine the directory with the filename.
            //
            string sCurrentFilePath = Path.Combine(dirReal.FullName, sBaseFilename);

            return new FileInfo(sCurrentFilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to get base file: {ex.Message}",
                "No Base File", MessageBoxButton.OK, MessageBoxImage.Error);

            return null;
        }
    }

    /// <summary>
    /// Purge any VS history files that lie outside of the current settings.
    /// </summary>
    /// <param name="vsHistoryFile"></param>
    public static void PurgeHistoryFile(VSHistoryFile vsHistoryFile)
    {
        if (!VsSettings.Enabled || !vsHistoryFile.HasHistoryFiles)
        {
            return;
        }

        ThreadHelper.ThrowIfNotOnUIThread();

        //
        // Get the values from the settings. 0 means that setting is disabled.
        //
        uint days_to_keep = VsSettings.KeepForTime ?
            KeepForTimeValues[VsSettings.KeepForTimeIndex] : 0;
        if (days_to_keep > 0)
        {
            VSLogMsg($"Keep files for {days_to_keep} days", Severity.Detail);
        }

        uint number_to_keep = VsSettings.KeepLatest ?
            KeepLatestValues[VsSettings.KeepLatestIndex] : 0;
        if (number_to_keep > 0)
        {
            VSLogMsg($"Keep {number_to_keep} files", Severity.Detail);
        }

        uint kb_to_keep = VsSettings.MaxStorage ?
            MaxStorageValues[VsSettings.MaxStorageIndex] : 0;
        if (kb_to_keep > 0)
        {
            VSLogMsg($"Keep up to {kb_to_keep} KB", Severity.Detail);
        }

        //
        // "Keep the most recent [number_to_keep] versions"
        //
        if (number_to_keep > 0 && vsHistoryFile.VSHistoryFiles.Count > number_to_keep)
        {
            //
            // Keep the most recent "number_to_keep" history files.
            //
            // vsHistoryFile.VSHistoryFiles is already sorted in descending order by
            // filename, which corresponds to the lastwrite datetime.
            //
            // Delete the files at the end of the list.
            //
            while (vsHistoryFile.VSHistoryFiles.Count > number_to_keep)
            {
                int index = vsHistoryFile.VSHistoryFiles.Count - 1;
                FileInfo fileInfo = vsHistoryFile.VSHistoryFiles[index];
                try
                {
                    fileInfo.Delete();
                }
                catch { }

                vsHistoryFile.VSHistoryFiles.RemoveAt(index);

                VSLogMsg(string.Format(
                    "Too many versions, removed {0} {1:N0} bytes at index {2}",
                    fileInfo.Directory.Name + "\\" + fileInfo.Name, fileInfo.Length, index));
            }
        }

        //
        // "Keep previous files for [days_to_keep] days/weeks/months..."
        //
        if (days_to_keep > 0)
        {
            DateTime dtEndDate = DateTime.Now.AddDays(-(int)days_to_keep);

            //
            // Delete files older than dtEndDate.
            //
            bool bAny = true;
            while (bAny)
            {
                bAny = false;
                foreach (FileInfo fileInfo in vsHistoryFile.VSHistoryFiles)
                {
                    //
                    // Extract the timestamp from the filename rather than LastWriteTime
                    // in case it was copied or saved/restored.
                    // Convert "2016-09-17_12_10_26_725"
                    // to "2016-09-17 12:10:26.725" to be parsed as a DateTime string.
                    //
                    DateTime dtLastWrite = DateTimeFromFilename(fileInfo.Name);

                    if (dtLastWrite == DateTime.MinValue)
                    {
                        //
                        // Didn't work -- use the last write time.
                        //
                        dtLastWrite = fileInfo.LastWriteTime;
                    }

                    if (dtLastWrite < dtEndDate)
                    {
                        //
                        // Delete this one.
                        //
                        try
                        {
                            fileInfo.Delete();
                        }
                        catch { }

                        //
                        // Remove it from the list.  This will change the
                        // ordering of the list, so start all over again.
                        //
                        vsHistoryFile.VSHistoryFiles.Remove(fileInfo);

                        VSLogMsg(string.Format(
                            "Too old, removed {0} {1:N0} bytes",
                            fileInfo.Directory.Name + "\\" + fileInfo.Name, fileInfo.Length));

                        bAny = true;
                        break;
                    }
                }
            }
        }

        //
        // "Limit a file's storage to [kb_to_keep] KB"
        //
        if (kb_to_keep > 0)
        {
            //
            // Compute the total on-disk size of VSHistory files.
            //
            long total_size = 0;

            foreach (FileInfo fileInfo in vsHistoryFile.VSHistoryFiles)
            {
                total_size += GetSizeOnDisk(fileInfo, vsHistoryFile.ClusterSize);
            }

            VSLogMsg(string.Format("Size of {0} VSHistory files on disk for {1} =\n  " +
                "{2} (0x{3:x} bytes), limit {4} (0x{5:x} bytes)",
                vsHistoryFile.VSHistoryFiles.Count,
                vsHistoryFile.VSFileInfo.Name,
                FormatSize(total_size),
                total_size,
                FormatSize(kb_to_keep * 1024),
                kb_to_keep * 1024));

            while (total_size > kb_to_keep * 1024 && vsHistoryFile.VSHistoryFiles.Count > 2)
            {
                //
                // Remove the last (oldest) one in the list of VSHistory files.
                //
                int index = vsHistoryFile.VSHistoryFiles.Count - 1;
                FileInfo fi = vsHistoryFile.VSHistoryFiles[index];

                long ulSize = GetSizeOnDisk(fi, vsHistoryFile.ClusterSize);
                total_size -= ulSize;

                vsHistoryFile.VSHistoryFiles.RemoveAt(index);
                try
                {
                    fi.Delete();

                    VSLogMsg(string.Format(
                        "Too much space, removed {0} {1:N0} bytes at index {2}",
                        fi.Directory.Name + "\\" + fi.Name, ulSize, index));
                }
                catch { }
            }
        }

        //
        // Delete the directory if it's empty.
        //
        if (vsHistoryFile.VSHistoryDir.GetFileSystemInfos().Length == 0)
        {
            try
            {
                vsHistoryFile.VSHistoryDir.Delete();

                VSLogMsg($"Deleted directory {vsHistoryFile.VSHistoryDir.FullName}");
            }
            catch { }
        }

        return;
    }
}

/// <summary>
/// Comparer to use for comparing FileInfo instances by name.
/// </summary>
public class VSHistoryFileDateCompare : Comparer<FileInfo>
{
    public override int Compare(FileInfo? x, FileInfo? y) =>
        string.Compare(x?.Name, y?.Name);
}

/// <summary>
/// Comparer to use for comparing VSHistoryFile instances by filename,
/// which is in the path. Compare the full path of the file in the VS project.
/// </summary>
public class VSHistoryFileCompare : Comparer<VSHistoryFile>
{
    public override int Compare(VSHistoryFile? x, VSHistoryFile? y) =>
        string.Compare(x?.FullPath, y?.FullPath);
}

/// <summary>
/// Comparer to use for sorting FileInfo or DirectoryInfo instances.
/// </summary>
public class FileSystemInfoCompare : Comparer<FileSystemInfo>
{
    public override int Compare(FileSystemInfo? x, FileSystemInfo? y) =>
        string.Compare(x?.FullName, y?.FullName);
}
