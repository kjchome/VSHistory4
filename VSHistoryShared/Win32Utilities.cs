using System.ComponentModel;
using System.Globalization;

namespace VSHistoryShared;
public static class Win32Utilities
{
    /// <summary>
    /// Known locations of localized strings in DLLs.
    /// From, e.g., https://windows10dll.nirsoft.net/propsys_dll.html
    /// </summary>
    private static readonly StringLookup[] axLookups = [
        new StringLookup( "All files", "shell32.dll", 34193 ),
        new StringLookup( "Date", "propsys.dll", 38780 ),
        new StringLookup( "Diff", "", 0),   // Can't find this localized
        new StringLookup( "Long", "propsys.dll", 41056 ),
        new StringLookup( "Open folder", "shell32.dll", 17410 ),
        new StringLookup( "Open", "shell32.dll", 12850 ),
        new StringLookup( "Settings", "gpedit.dll", 57 ),
        new StringLookup( "Short", "propsys.dll", 41054 ),
        new StringLookup( "Size", "propsys.dll", 38657 ),
        new StringLookup( "Today", "propsys.dll", 106 ),
        new StringLookup( "Version", "propsys.dll", 38946 ),
        new StringLookup( "Yesterday", "propsys.dll", 105 ),
    ];

    /// <summary>
    /// Local variables to avoid calling GetDiskFreeSpace over and over again.
    /// </summary>
    private static uint _ClusterSize;

    /// <summary>
    /// The volume name, if any, where we already have the cluster size.
    /// </summary>
    private static string? _Volume;

    /// <summary>
    /// Create a pretty string for a size in bytes, KB, MB, GB, TB, or PB.
    /// </summary>
    /// <param name="iSize">
    /// Size to be displayed
    /// </param>
    /// <param name="bSuppressZeros">
    /// If True, values ending in ".00" or ".0" will have them stripped.
    /// </param>
    /// <returns>
    /// String containing the size in bytes, KB, MB, GB, TB, or PB.
    /// to one decimal place, e.g., "15.6 KB".  The decimal
    /// place is dropped if zero ("16.0" becomes "16").
    /// </returns>
    /// <example>
    ///         532 -> 532 bytes
    ///        1340 -> 1.3 KB
    ///       23506 -> 23.5 KB
    ///     2400016 -> 2.4 MB
    ///  2400000000 -> 2.4 GB
    /// </example>
    public static string FormatSize(long iSize, bool bSuppressZeros = false)
    {
        //
        // Use the Win32 StrFormatByteSize() function.
        //
        StringBuilder sb = new(16);
        StrFormatByteSize(iSize, sb, sb.Capacity);
        string sSize = sb.ToString();

        if (bSuppressZeros)
        {
            sSize = sSize.Replace(".00", "").Replace(".0", "");
        }

        return sSize;
    }

    public static string FormatSize(ulong iSize, bool bSuppress = false) =>
            FormatSize((long)iSize, bSuppress);

    /// <summary>
    /// Return the cluster size, in bytes, of the volume specified by "path".
    /// </summary>
    /// <param name="sPath">
    /// Absolute path on the volume, must start with the drive specification, "C:\".
    /// </param>
    /// <returns></returns>
    public static uint GetClusterSize(string sPath)
    {
        //
        // We need information about the disk on which the repository resides.
        //
        string? sVolume = Path.GetPathRoot(sPath)?.ToUpper();

        if (string.IsNullOrEmpty(sVolume))
        {
            // Huh??
            Debug.Assert(!string.IsNullOrEmpty(sVolume));
            return 4096;
        }

        //
        // Compute the cluster size if the saved volume doesn't match.
        //
        if (string.IsNullOrEmpty(_Volume) || _Volume != sVolume)
        {
            bool bOK = GetDiskFreeSpace(sVolume!,
                out uint lpSectorsPerCluster,
                out uint lpBytesPerSector,
                out _,
                out _);

            Debug.Assert(bOK, "GetDiskFreeSpace failed");

            _ClusterSize = lpBytesPerSector * lpSectorsPerCluster;
            _Volume = sVolume;

            VSLogMsg(string.Format("Cluster size for {0} = {1} = 0x{1:x}",
                sVolume, _ClusterSize), Severity.Verbose);
        }

        return _ClusterSize;
    }

    /// <summary>
    /// Get the true file allocation on disk. If the file data is totally
    /// contained within the file record (<600 bytes or so), the length will be 0.
    /// Otherwise, if it is compressed, it'll be the count of clusters in
    /// the retrieval pointers. If not compressed, it is simply the file
    /// size rounded up to the cluster size.
    /// </summary>
    /// <param name="fileInfo">
    /// File to get size on disk.
    /// </param>
    /// <param name="lBytesPerCluster">
    /// Total number of bytes per cluster (typically 4096).
    /// </param>
    /// <returns>
    /// </returns>
    public static long GetSizeOnDisk(FileInfo fileInfo, long lBytesPerCluster)
    {
        //
        // For compressed or very small files, get the actual retrieval pointers.
        //
        // This is always done for compressed files because testing has shown
        // that GetCompressedFileSize doesn't always return the correct value,
        // especially for compressed files less than the cluster size (4 KB).
        //
        // Using retrieval pointers is always correct but much slower.
        //
        if (fileInfo.Length < 1024 ||
            ((fileInfo.Attributes & FileAttributes.Compressed) != 0 &&
                fileInfo.Length < lBytesPerCluster))
        {
            try
            {
                var VcnBuffer = new StartingVcnInputBuffer();
                RetrievalPointersBuffer rpBuffer;
                int iBytesReturned = 0;
                long lLength = 0;
                int errorLevel;

                using SafeFileHandle handle = fileInfo.Open
                    (FileMode.Open, FileAccess.Read, FileShare.ReadWrite).SafeFileHandle;

                do
                {
                    //
                    // Get the retrieval pointers for this segment.
                    //
                    DeviceIoControl(
                        handle, FSCTL_GET_RETRIEVAL_POINTERS,
                        ref VcnBuffer, StartingVcnInputBuffer.Size,
                        out rpBuffer, RetrievalPointersBuffer.Size,
                        iBytesReturned, IntPtr.Zero);

                    errorLevel = Marshal.GetLastWin32Error();

                    switch (errorLevel)
                    {
                        case ERROR_HANDLE_EOF:
                            break;

                        case NO_ERROR:
                        case ERROR_MORE_DATA:
                            //
                            // NOTE: Compression is performed in 16-cluster units.
                            // So if a file is compressed and a given 16-cluster unit
                            // compresses to fit in, for example, 9 clusters, there will be a
                            // 7-cluster extent of the file with an LCN of -1.
                            // These don't take space on the disk.
                            //
                            VcnBuffer.StartingVcn = rpBuffer.NextVcn;
                            if (rpBuffer.Lcn > 0)
                            {
                                lLength += (rpBuffer.NextVcn - rpBuffer.StartingVcn) * lBytesPerCluster;
                            }
                            break;

                        default:
                            throw new Win32Exception(errorLevel);
                    }
                } while (errorLevel != ERROR_HANDLE_EOF);

#if DEBUG
                if (lLength > 0)
                {
                    uint uiLow;
                    uint uiHigh;
                    uiLow = GetCompressedFileSizeW(fileInfo.FullName, out uiHigh);

                    if (uiLow != (lLength & 0xFFFFFFFF) || uiHigh != (lLength >> 32))
                    {
                        VSLogMsg($"GetCompressedFileSizeW Mismatch: {lLength:N0} vs {uiLow:N0}");
                    }
                }
#endif

                return lLength;
            }
            catch
            {
                //
                // Something happened -- fall through to using the file's length.
                //
            }
        }

        //
        // If the file is not compressed and is too large to be contained within the
        // MFT file record, we can simply round up the file size to the next cluster.
        //
        return (fileInfo.Length + lBytesPerCluster - 1) / lBytesPerCluster * lBytesPerCluster;
    }

    /// <summary>
    /// Look up a string in a DLL or EXE.  If the Region has been
    /// changed to non-English, it should get the localized value.
    /// </summary>
    /// <param name="sKey"></param>
    /// <param name="culture">
    /// The culture to use to open the library, e.g., "fr-FR".
    /// </param>
    /// <returns></returns>
    public static string LocalizedString(string sKey, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;

        StringLookup xLookup = axLookups.Where(x => x.sKey == sKey).FirstOrDefault();
        return xLookup?.Lookup(culture) ?? sKey;
    }

    /// <summary>
    /// Class to contain information to look up a string in a DLL.
    /// </summary>
    private class StringLookup
    {
        private Dictionary<CultureInfo, string> cultureStrings = new();

        public uint iStringIndex { get; }

        public string sKey { get; }

        public string sLibrary { get; }

        public StringLookup(string s1, string s2, uint i)
        {
            sKey = s1;
            sLibrary = s2;
            iStringIndex = i;
        }

        /// <summary>
        /// Look up the string for this key given a specific culture.
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public string Lookup(CultureInfo culture)
        {
            //
            // Quick exit if we can't localize it.
            //
            if (string.IsNullOrEmpty(sLibrary) || iStringIndex == 0)
            {
                return sKey;
            }

            //
            // Check to see if we've already looked this up.
            //
            if (cultureStrings.ContainsKey(culture))
            {
                return cultureStrings[culture];
            }

            //
            // We assume the library file is in C:\Windows\System32
            //
            FileInfo fiBaseFile = new(Path.Combine(Environment.SystemDirectory, sLibrary));

            if (!fiBaseFile.Exists)
            {
                Debug.Assert(fiBaseFile.Exists);
                cultureStrings[culture] = sKey;
                return sKey;
            }

            //
            // See if a culture-specific version of the DLL exists.
            // For example, for propsys.dll:
            //
            //  c:\Windows\System32\en-US\propsys.dll.mui
            //
            string sCultureFile = Path.Combine(
                Environment.SystemDirectory,
                culture.Name,
                sLibrary + ".mui");

            FileInfo fiCultureFile = new(sCultureFile);

            //
            // If it doesn't exist, fall back to the default DLL.
            //
            string sLibPath = fiCultureFile.Exists ?
                fiCultureFile.FullName : fiBaseFile.FullName;

            IntPtr hInstance = LoadLibraryEx(
                sLibPath,
                IntPtr.Zero,
                2 /* LOAD_LIBRARY_AS_DATAFILE */);

            if (hInstance == IntPtr.Zero)
            {
                Debug.Assert(hInstance != IntPtr.Zero);
                cultureStrings[culture] = sKey;
                return sKey;
            }

            //
            // Get the string from the string table.
            //
            StringBuilder sb = new StringBuilder(1024);
            int iLen = LoadString(hInstance, iStringIndex, sb, sb.Capacity);
            FreeLibrary(hInstance);

            if (iLen > 0)
            {
                cultureStrings[culture] = sb.ToString();
                return cultureStrings[culture];
            }

            //
            // Not found.  If using the culture-specific file, try the default.
            //
            if (!fiCultureFile.Exists)
            {
                cultureStrings[culture] = sKey;
                return sKey;
            }

            hInstance = LoadLibraryEx(
                fiBaseFile.FullName,
                IntPtr.Zero,
                2 /* LOAD_LIBRARY_AS_DATAFILE */);

            if (hInstance != IntPtr.Zero)
            {
                iLen = LoadString(hInstance, iStringIndex, sb, sb.Capacity);
                FreeLibrary(hInstance);
                if (iLen > 0)
                {
                    cultureStrings[culture] = sb.ToString();
                    return cultureStrings[culture];
                }
            }

            Debug.Assert(false, "Both DLLs failed?");
            cultureStrings[culture] = sKey;
            return sKey;
        }
    }
}
