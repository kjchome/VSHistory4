
namespace VSHistoryShared;

public class Win32Imports
{
    /// <summary>
    /// A file is being opened for a backup or restore operation.
    /// The system ensures that the calling process overrides
    /// file security checks when the process has SE_BACKUP_NAME
    /// and SE_RESTORE_NAME privileges.
    /// </summary>
    public static uint FILE_FLAG_BACKUP_SEMANTICS => 0x02000000;

    public static uint FSCTL_GET_RETRIEVAL_POINTERS => 0x00090073;

    public static uint FSCTL_SET_COMPRESSION => 0x9C040;

    public static uint GENERIC_ALL => (0x10000000);

    public static uint GENERIC_EXECUTE => (0x20000000);

    public static uint GENERIC_READ => (0x80000000);

    public static uint GENERIC_WRITE => (0x40000000);

    public static uint INVALID_FILE_SIZE => 0xFFFFFFFF;

    public static int INVALID_HANDLE_VALUE => -1;

    public const int ERROR_HANDLE_EOF = 0x26;

    public const int ERROR_MORE_DATA = 0xEA;

    public const int NO_ERROR = 0x0;


    /// <summary>
    /// Information transferred in SHGetFileInfo.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;

        public int iIcon;

        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    /// <summary>
    /// Flags for SHGetFileInfo
    /// </summary>
    [FlagsAttribute]
    public enum SHGFI
    {
        ADDOVERLAYS = 0x000000020,  // apply the appropriate overlays

        ATTR_SPECIFIED = 0x000020000,  // get only specified attributes

        ATTRIBUTES = 0x000000800,  // get attributes

        DISPLAYNAME = 0x000000200,  // get display name

        EXETYPE = 0x000002000,  // return exe type

        ICON = 0x000000100,  // get icon

        ICONLOCATION = 0x000001000,  // get icon location

        LARGEICON = 0x000000000,  // get large icon

        LINKOVERLAY = 0x000008000,  // put a link overlay on icon

        OPENICON = 0x000000002,  // get open icon

        OVERLAYINDEX = 0x000000040,  // Get the index of the overlay

        PIDL = 0x000000008,  // pszPath is a pidl

        SELECTED = 0x000010000,  // show icon in selected state

        SHELLICONSIZE = 0x000000004,  // get shell size icon

        SMALLICON = 0x000000001,  // get small icon

        SYSICONINDEX = 0x000004000,  // get system icon index

        TYPENAME = 0x000000400,  // get type name

        USEFILEATTRIBUTES = 0x000000010,  // use passed dwFileAttribute
    }

    /// <summary>
    /// Retrieves information about an object in the file system, such as a file,
    /// folder, directory, or drive root.
    /// </summary>
    /// <param name="pszPath">
    /// A pointer to a null-terminated string of maximum length MAX_PATH that contains the
    /// path and file name. Both absolute and relative paths are valid.
    ///
    /// If the uFlags parameter includes the SHGFI_PIDL flag, this parameter must be
    /// the address of an ITEMIDLIST (PIDL) structure that contains the list of item
    /// identifiers that uniquely identifies the file within the Shell's namespace.
    /// The PIDL must be a fully qualified PIDL. Relative PIDLs are not allowed.
    ///
    /// If the uFlags parameter includes the SHGFI_USEFILEATTRIBUTES flag, this parameter doesn't
    /// have to be a valid file name. The function will proceed as if the file exists with the
    /// specified name and with the file attributes passed in the dwFileAttributes parameter.
    /// This allows you to obtain information about a file type by passing just the extension
    /// for pszPath and passing FILE_ATTRIBUTE_NORMAL in dwFileAttributes.
    /// </param>
    /// <param name="dwFileAttributes">
    /// A combination of one or more file attribute flags. If uFlags does not include the
    /// SHGFI_USEFILEATTRIBUTES flag, this parameter is ignored.
    /// </param>
    /// <param name="pSHFI">
    /// Pointer to a SHFILEINFO structure to receive the file information.
    /// </param>
    /// <param name="cbSizeFileInfo">
    /// The size, in bytes, of the SHFILEINFO structure pointed to by the psfi parameter.
    /// </param>
    /// <param name="uFlags">
    /// The flags that specify the file information to retrieve.
    /// </param>
    /// <returns>
    /// Returns a value whose meaning depends on the uFlags parameter.
    ///
    /// If uFlags does not contain SHGFI_EXETYPE or SHGFI_SYSICONINDEX,
    /// the return value is nonzero if successful, or zero otherwise.
    ///
    /// If uFlags contains the SHGFI_EXETYPE flag, the return value specifies
    /// the type of the executable file.
    /// </returns>
    /// <example>
    ///     SHFILEINFO shinfo = new();
    ///     SHGFI uFlags = SHGFI.TYPENAME | SHGFI.USEFILEATTRIBUTES;
    ///     IntPtr bOK = SHGetFileInfo(".txt", FileAttributes.Normal,
    ///                                ref shinfo, (uint)Marshal.SizeOf(shinfo), uFlags);
    /// </example>
    [DllImport("shell32")]
    public static extern IntPtr SHGetFileInfo(
        string pszPath,
        FileAttributes dwFileAttributes,
        ref SHFILEINFO pSHFI,
        uint cbSizeFileInfo,
        SHGFI uFlags);

    [StructLayout(LayoutKind.Sequential)]
    public struct RetrievalPointersBuffer
    {
        public static readonly int Size;

        static RetrievalPointersBuffer()
        {
            Size = Marshal.SizeOf(typeof(RetrievalPointersBuffer));
        }

        public readonly int ExtentCount;

        public readonly long StartingVcn;

        public readonly long NextVcn;

        public readonly long Lcn;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StartingVcnInputBuffer
    {
        public static readonly int Size;

        static StartingVcnInputBuffer()
        {
            Size = Marshal.SizeOf(typeof(StartingVcnInputBuffer));
        }

        public long StartingVcn;
    }

    /// <summary>
    /// Creates a file / directory or opens an handle for an existing file.
    /// Otherwise it you'll get an invalid handle
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeFileHandle CreateFile(
         string fullName,
         [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
         [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
         IntPtr lpSecurityAttributes,
         [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
         [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
         IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        SafeFileHandle hFile,
        uint ioctl,
        ref StartingVcnInputBuffer inValue,
        int InSize,
        out RetrievalPointersBuffer outValue,
        int OutSize,
        int BytesReturned,
        IntPtr zero);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        SafeFileHandle hFile,
        uint ioctl,
        ref short inBuffer,
        int InSize,
        IntPtr zero,
        int OutSize,
        ref int BytesReturned,
        IntPtr lpOverlapped
    );

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FreeLibrary(IntPtr hInst);

    /// <summary>
    /// Retrieves information about the specified disk,
    /// including the amount of free space on the disk.
    /// </summary>
    /// <param name="lpRootPathName">
    /// The root directory of the disk for which information is to be returned.
    /// A drive specification must have a trailing backslash (for example, "C:\").
    /// </param>
    /// <param name="lpSectorsPerCluster"></param>
    /// <param name="lpBytesPerSector"></param>
    /// <param name="lpNumberOfFreeClusters"></param>
    /// <param name="lpTotalNumberOfClusters"></param>
    /// <returns></returns>
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetDiskFreeSpace(string lpRootPathName,
       out uint lpSectorsPerCluster,
       out uint lpBytesPerSector,
       out uint lpNumberOfFreeClusters,
       out uint lpTotalNumberOfClusters);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr LoadLibraryEx(string path, IntPtr x, int i);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int LoadString(IntPtr hInst, uint id, StringBuilder buf, int buflen);

    /// <summary>
    /// Converts a numeric value into a string that represents the number expressed as a
    /// size value in bytes, kilobytes, megabytes, or gigabytes, depending on the size.
    /// </summary>
    /// <param name="fileSize">
    /// The numeric value to be converted
    /// </param>
    /// <param name="buffer">
    /// A pointer to the converted string
    /// </param>
    /// <param name="bufferSize">
    /// The size of pszBuf, in characters
    /// </param>
    [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
    public static extern void StrFormatByteSize(
        long fileSize,
        [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer,
        int bufferSize);

    [DllImport("kernel32.dll")]
    public static extern uint GetCompressedFileSizeW(
        [In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
        [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);
}
