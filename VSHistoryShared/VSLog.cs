//
// For universal access to VSLogSync.
//
global using static VSHistoryShared.VSLog;

using System.Reflection;
using System.Runtime.CompilerServices;

namespace VSHistoryShared;

public static class VSLog
{
    private static string _AssemblyBuildTime = "";
    private static StreamWriter? _LogWriter;

    /// <summary>
    /// Named mutex for synchronizing access to the log file.
    /// </summary>
    public const string VSLogMutexName = "VSLogFileMutex";

    /// <summary>
    /// The executing assembly (VSHistoryIP or VSHistoryOP).
    /// </summary>
    public static Assembly VSHistoryAssembly => Assembly.GetExecutingAssembly();

    /// <summary>
    /// When the assembly was built.
    /// </summary>
    public static string AssemblyBuildTime
    {
        get
        {
            if (string.IsNullOrEmpty(_AssemblyBuildTime))
            {
                //
                // BuildTimestamp.cs is built automatically
                // by the BeforeBuild Target in the project file
                // every time the project is built or re-built.
                // It has one member, CompileTime, which is the
                // timestamp at the time of the build.
                //
                // The timestamp is in Universal format, e.g., 
                // "2025-05-31 19:57:55Z", so it is always the same
                // regardless of the current culture.
                //
                if (DateTime.TryParse(BuildTimestamp.CompileTime, out DateTime buildTime))
                {
                    _AssemblyBuildTime = buildTime.ToString("G");
                }
                else
                {
                    //
                    // Hmmm.  This shouldn't have failed...
                    //
                    _AssemblyBuildTime = "(Unknown)";
                }
            }

            return _AssemblyBuildTime;
        }
    }

    /// <summary>
    /// The executing assembly name.
    /// </summary>
    public static string AssemblyName => VSHistoryAssembly.GetName().Name!;

    /// <summary>
    /// The log file as a StreamWriter.
    /// </summary>
    public static StreamWriter LogWriter
    {
        get
        {
            if (_LogWriter == null)
            {
                GetLogWriter();
                Debug.Assert(_LogWriter != null);
            }

            return _LogWriter!;
        }
    }

    /// <summary>
    /// Define the severity of the message for filtering.
    /// </summary>
    public enum Severity : uint
    {
        /// <summary>
        /// Verbose messages are used for debugging.
        /// </summary>
        Verbose,

        /// <summary>
        /// Detailed messages mainly showing the flow of the program.
        /// </summary>
        Detail,

        /// <summary>
        /// Informational messages are used for general information.
        /// </summary>
        Info,

        /// <summary>
        /// Warnings are used for messages that are not errors but
        /// indicate something is not right or was unexpected.
        /// </summary>
        Warning,

        /// <summary>
        /// Errors are used for messages that indicate a failure.
        /// </summary>
        Error,
    }

    /// <summary>
    /// Open the log file for writing as a StreamWriter.
    /// </summary>
    public static void GetLogWriter()
    {
        if (_LogWriter != null)
        {
            return;
        }

        //
        // Create the directory if necessary.
        //
        // C:\Users\user\AppData\Local\VSHistory\VSLogs
        //
        string sLogDir = Path.GetDirectoryName(VS_LogFilePath);
        Directory.CreateDirectory(sLogDir);

        //
        // Open the log file, appending if it exists.
        //
        try
        {
            FileStream fileStream = new(VS_LogFilePath,
                FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            if (fileStream == null)
            {
                throw new Exception("NULL??");
            }

            //
            // Seek to EOF so we'll be appending to the file.
            //
            fileStream.Seek(0, SeekOrigin.End);
            _LogWriter = new(fileStream)
            {
                AutoFlush = true
            };

            if (_LogWriter == null)
            {
                throw new Exception("NULL??");
            }
        }
        catch (Exception ex)
        {
            Debug.Fail($"Failed to create {VS_LogFilePath} {ex}");
        }
    }

    /// <summary>
    /// Output a message to the VSHistory-specific output file.
    /// This is typically used for debugging messages.
    /// </summary>
    /// <param name="sString"></param>
    /// <param name="memberName"></param>
    /// <param name="sourceFilePath"></param>
    /// <param name="sourceLineNumber"></param>
    /// <returns></returns>
    public static void VSLogMsg(
        string sString,
        Severity severity = Severity.Info,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        VS_Settings settings = VsSettings;

        if (!settings.LoggingEnabled || (uint)severity < settings.LogLevelIndex)
        {
            return;
        }

        //
        // Check if we're on the UI thread. If not, use the lower case
        // severity character. This is to make it easier to filter
        // messages in the log file.  The UI thread is the main thread.
        //
        char cSeverity = ThreadHelper.CheckAccess() ? severity.ToString()[0] :
            severity.ToString().ToLower()[0];

        //
        // Format the log message for output, e.g., 
        // 
        //   11:31:01.485 I [118624:11] Win32.cs[ 52] GetClusterSize(): Cluster size for C:\ = 4096 = 0x1000
        //
        string sSource = Path.GetFileName(sourceFilePath);

        string sOut = string.Format("{5} {6} [{7,5}:{4,2}] {0}[{1,3}] {2}(): {3}",
            Path.GetFileName(sourceFilePath),
            sourceLineNumber,
            memberName,
            sString,
            Environment.CurrentManagedThreadId,
            DateTime.Now.ToString("HH:mm:ss.fff"),
            cSeverity,
            Process.GetCurrentProcess().Id);

        sOut = sOut.TrimEnd() + Environment.NewLine;

#if VSHISTORY_PACKAGE
        if (settings.LogToOutputWindow)
        {
            //
            // Output to the debug pane.
            // This doesn't exist in the test app.
            //
            g_DebugPane?.Write(sOut);
        }
#endif

        if (settings.LogToFile)
        {
            //
            // Synchronize between possible multiple processes.
            // This requires a seek to get to EOF.
            //
            // We can't use WriteAsync here because if they're too close
            // together and the previous one hasn't completed, it gets tossed.
            //
            using (var mutex = new Mutex(false, VSLogMutexName))
            {
                mutex.WaitOne();

                try
                {
                    LogWriter.BaseStream.Seek(0, SeekOrigin.End);
                    LogWriter.Write(sOut);
                }
                catch
                {
                    // Ignore exceptions for now.
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }

    /// <summary>
    /// Build the version string for the assembly.
    /// </summary>
    /// <returns></returns>
    public static string VSVersion()
    {
        string sVSVersion = "?";

        //
        // Output our name, version and how long after Visual Studio started.
        //
        // "VSHistory v4.1 Built 3/3/2025 12:28:08 PM, 0.428 seconds after VS"
        //
        using (Process xProcess = Process.GetCurrentProcess())
        {
            //
            // The time from start is only valid if we're running in-process.
            //
            if (xProcess.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase))
            {
                TimeSpan ts = DateTime.Now - xProcess.StartTime;
                string sVSStart = xProcess.StartTime.ToString("HH:mm:ss.fff");

                sVSVersion = $"{AssemblyName} v{VSHistoryAssembly.GetName().Version!.ToString(3)} Built {AssemblyBuildTime}, {ts.TotalSeconds:F3} seconds after VS (started {sVSStart}) ";
            }
            else
            {
                sVSVersion = $"{AssemblyName} v{VSHistoryAssembly.GetName().Version!.ToString(3)} Built {AssemblyBuildTime}";
            }
        }

        return sVSVersion;
    }
}
