using System.Xml;
using System.Xml.Serialization;

namespace VSHistoryShared;

/// <summary>
/// Class to hold the settings for VSHistory.
/// </summary>
public class VS_Settings : ICloneable
{
    /// <summary>
    /// The default (empty) constructor is required
    /// to enable VS_Settings to be serialized.
    /// </summary>
    public VS_Settings()
    {
    }

    public override string ToString()
    {
        string sName = string.IsNullOrEmpty(Name) ? "Default" : Name;
        return $"{sName} settings";
    }

    /// <summary>
    /// The default settings.  This must always
    /// be the first element of AllVsSettings.
    /// </summary>
    public static VS_Settings DefaultSettings
    {
        get
        {
            Debug.Assert(
                AllVsSettings != null &&
                AllVsSettings.Count > 0 &&
                AllVsSettings[0].Name == "");

            return AllVsSettings![0];
        }
    }

    /// <summary>
    /// Compare a VS_Settings to the default settings.
    /// </summary>
    /// <param name="settingsIn"></param>
    /// <returns>
    /// True if the input settings are the same as the
    /// default settings except for the name and the
    /// ExcludedDirs and ExcludedFiles.
    /// </returns>
    /// <remarks>
    /// A clone of the input settings with the ExcludedDirs and
    /// ExcludedFiles emptied and the name set to "" is created.
    ///
    /// Then a clone of the default settings with the ExcludedDirs
    /// and ExcludedFiles emptied is created.
    ///
    /// Then both the modified input settings and modified default
    /// settings are converted to XML and the results are compared.
    /// </remarks>
    public static bool IsDefault(VS_Settings settingsIn)
    {
        Debug.Assert(!string.IsNullOrEmpty(settingsIn.Name));

        //
        // If the solution-specific settings have any exclusions
        // then we know they're not the same as default.
        //
        if (settingsIn.ExcludedDirs.Count > 0 || settingsIn.ExcludedFiles.Count > 0)
        {
            return false;
        }

        XmlSerializer xmlSerializer = new XmlSerializer(typeof(VS_Settings));

        //
        // Get a clone of the input settings with the ExcludedDirs and
        // ExcludedFiles emptied and the name set to "".
        //
        VS_Settings settings = (VS_Settings)settingsIn.Clone();
        settings.Name = "";

        //
        // Convert the modified input settings to XML.
        //
        string settingsXML;
        using (StringWriter textWriter = new())
        {
            xmlSerializer.Serialize(textWriter, settings);
            settingsXML = textWriter.ToString();
        }

        //
        // Get a clone of the default settings with
        // the ExcludedDirs and ExcludedFiles emptied.
        //
        VS_Settings def = (VS_Settings)DefaultSettings.Clone();

        string defXML;
        using (StringWriter textWriter = new())
        {
            xmlSerializer.Serialize(textWriter, def);
            defXML = textWriter.ToString();
        }

        return settingsXML == defXML;
    }

    /// <summary>
    /// Indicate if we are editing the settings
    /// and, if so, which settings we are editing.
    /// </summary>
    public enum SettingsState
    {
        /// <summary>
        /// We don't know what we are doing with the settings.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// We are not editing the settings. The assignment
        /// of VsSettings depends on the solution name or
        /// default settings if the solution name isn't set.
        /// </summary>
        NotEditing,

        /// <summary>
        /// We are editing the settings for the current solution.  The
        /// assignment of VsSettings should be from SolutionName,
        /// which must not be empty.
        /// </summary>
        EditSettingsSolution,

        /// <summary>
        /// We are editing the settings for the default settings. The assignment
        /// of VsSettings should be from the default settings (key "").
        /// </summary>
        EditSettingsDefault
    }

    /// <summary>
    /// Named mutex for synchronizing access to the settings file.
    /// </summary>
    public const string VSSettingsMutexName = "VSSettingsFileMutex";

    public static SettingsState EditSettingsState { get; set; } = SettingsState.Unknown;

    private static List<VS_Settings>? _AllVsSettings;

    /// <summary>
    /// Base directory for VSHistory settings and logs, e.g.,
    /// C:\Users\user\AppData\Local\VSHistory
    ///
    /// N.B. We can't user the long path prefix \\?\C:\ here
    ///      because FileSystemWatcher doesn't support it.
    ///      That *shouldn't* be a problem in the path
    ///      using %LOCALAPPDATA%.
    ///
    /// </summary>
    public static string VS_LocalBaseDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VSHistory");

    /// <summary>
    /// The path to the log file, e.g.,
    /// C:\Users\user\AppData\Local\VSHistory\VSHistory.log
    /// </summary>
    public static string VS_LogFilePath => Path.Combine(VS_LocalBaseDir, "VSHistory.log");

    /// <summary>
    /// The path to the settings file, e.g.,
    /// C:\Users\user\AppData\Local\VSHistory\Settings.xml
    /// </summary>
    public static string VS_SettingsXmlPath => Path.Combine(VS_LocalBaseDir, "Settings.xml");

    /// <summary>
    /// VS_Settings for all solutions. The Name is the solution name
    /// or "" (empty string) for the default settings, which must be first.
    /// </summary>
    public static List<VS_Settings> AllVsSettings
    {
        get
        {
            if (_AllVsSettings == null)
            {
                //
                // Synchronize between possible multiple processes.
                //
                using (var mutex = new Mutex(false, VSSettingsMutexName))
                {
                    mutex.WaitOne();

                    //
                    // Get the VS_Settings from the saved settings or use the default.
                    //
                    try
                    {
                        if (File.Exists(VS_SettingsXmlPath))
                        {
                            XmlSerializer xmlSerializer =
                                new XmlSerializer(typeof(List<VS_Settings>));

                            using (XmlReader xmlReader = XmlReader.Create(VS_SettingsXmlPath))
                            {
                                _AllVsSettings =
                                    (List<VS_Settings>)xmlSerializer.Deserialize(xmlReader);
                            }
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }

            //
            // If still null, fall back to a single default setting.
            //
            _AllVsSettings ??= [new VS_Settings()];

            return _AllVsSettings;
        }
    }

    /// <summary>
    /// Reset AllVsSettings to force them to be read from saved settings.
    /// </summary>
    public static void ResetAllSettings() => _AllVsSettings = null;

    /// <summary>
    /// The singleton VS_Settings instance.
    /// </summary>
    public static VS_Settings VsSettings
    {
        get
        {
            //
            // Try to get the settings for the current solution.
            //
            VS_Settings? settings = null;

            //
            // Get the appropriate settings based on the current SettingsState.
            //
            switch (EditSettingsState)
            {
                case SettingsState.NotEditing:
#if VSHISTORY_PACKAGE
                    //
                    // We are running in the extension, so try
                    // to get the solution name if there is one.
                    //
                    if (!string.IsNullOrWhiteSpace(SolutionName))
                    {
                        settings = AllVsSettings
                            .Where(s => s.Name == SolutionName!)
                            .FirstOrDefault();

                        //
                        // If the settings are not found, create one using the default settings.
                        //
                        if (settings == null)
                        {
                            settings = DefaultSettings.FullClone(SolutionName!);
                            AllVsSettings.Add(settings);
                        }
                    }
                    else
                    {
                        //
                        // If the solution name is not set, fall
                        // through to use the default settings.
                        //
                    }
                    break;
#else
                    //
                    // If running in the test harness, something went wrong.
                    //
                    throw new InvalidOperationException("Should not be here!!");
#endif

                case SettingsState.EditSettingsSolution:
                    //
                    // If editing the settings for the current solution, use the
                    // settings for the current solution. The solution name
                    // must not be empty.
                    //
                    if (string.IsNullOrWhiteSpace(SolutionName))
                    {
                        Debug.Assert(!string.IsNullOrWhiteSpace(SolutionName));
                        throw new InvalidOperationException("SolutionName is empty.");
                    }

                    settings = AllVsSettings
                        .Where(s => s.Name == SolutionName!)
                        .FirstOrDefault();

                    //
                    // If the settings are not found, create one using the default settings.
                    //
                    if (settings == null)
                    {
                        settings = DefaultSettings.FullClone(SolutionName!);
                        AllVsSettings.Add(settings);
                    }
                    break;

                case SettingsState.Unknown:
                case SettingsState.EditSettingsDefault:
                    //
                    // Fall through to get the default settings.
                    //
                    break;
            }

            //
            // If the settings are not found, use the default settings.
            //
            return settings ?? DefaultSettings;
        }
    }

    #region Stored Settings

    //
    // The settings below are stored in the XML file.
    //

    /// <summary>
    /// The name of the solution. Must be unique.
    /// This is an empty string for the default solution.
    /// </summary>
    public string Name { get; set; } = "";

    //
    // The following Date_* are in a group -- exactly one must be true.
    //
    public bool Date_ISO { get; set; }

    public bool Date_ISO_UT { get; set; }

    public bool Date_Long { get; set; } = true;

    public bool Date_LongCurrent { get; set; }

    public bool Date_Short { get; set; }

    public bool Date_ShortCurrent { get; set; }

    /// <summary>
    /// Overall enable/disable flag for VSHistory.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The directories to be excluded.
    /// </summary>
    public List<string> ExcludedDirs { get; set; } = new();

    /// <summary>
    /// The files to be excluded.
    /// </summary>
    public List<string> ExcludedFiles { get; set; } = new();

    /// <summary>
    /// The file location where VS History files will be saved.
    /// This is only used if FileLocation_Custom is true.
    /// It must be an absolute path to a directory.
    /// </summary>
    public string FileLocation { get; set; } = "";

    //
    // The following FileLocation_* are in a group -- exactly one must be true.
    //
    public bool FileLocation_AppData { get; set; }

    public bool FileLocation_Custom { get; set; }

    public bool FileLocation_WithSolution { get; set; } = true;

    /// <summary>
    /// "Don't save files more often than:"
    /// </summary>
    public bool Frequency { get; set; } = false;

    /// <summary>
    /// Index into the FrequencyValues array.
    /// 0 = 30 seconds, 1 = 1 minute, ...
    /// </summary>
    public uint FrequencyIndex { get; set; } = 3;

    /// <summary>
    /// How often files can be saved, in seconds.
    /// </summary>
    public static uint[] FrequencyValues => [30, 60, 300, 600, 1800, 3600];

    /// <summary>
    /// "Compress (GZIP) files larger than:"
    /// </summary>
    public bool GZIP { get; set; }

    /// <summary>
    /// Index into the GZIPValues array.
    /// 0 = 4 KB, 1 = 8 KB, ...
    /// </summary>
    public uint GZIPIndex { get; set; } = 3;

    /// <summary>
    /// Minimum file size for GZIP compression, in KB.
    /// </summary>
    public static uint[] GZIPValues => [4, 8, 16, 32, 64, 128, 256, 512, 1024];

    /// <summary>
    /// "Keep VSHistory files for:"
    /// </summary>
    public bool KeepForTime { get; set; }

    /// <summary>
    /// Index into the KeepForTimeValues array.
    /// 0 = 10 days, 1 = 30 days, ...
    /// </summary>
    public uint KeepForTimeIndex { get; set; } = 2;

    /// <summary>
    /// How long to keep VS History files, in days.
    /// </summary>
    public static uint[] KeepForTimeValues => [10, 30, 90, 180, 360];

    /// <summary>
    /// "Keep up to this many VSHistory files:"
    /// </summary>
    public bool KeepLatest { get; set; }

    /// <summary>
    /// Index into the KeepLatestValues array.
    /// 0 = 10 files, 1 = 25 files, ...
    /// </summary>
    public uint KeepLatestIndex { get; set; } = 3;

    /// <summary>
    /// Number of VS History files to keep.
    /// </summary>
    public static uint[] KeepLatestValues => [10, 25, 50, 100, 500, 1000];

#if DEBUG
    public bool LoggingEnabled { get; set; } = true;
#else
    public bool LoggingEnabled { get; set; } = false;
#endif

    public bool LogToOutputWindow { get; set; }

    public bool LogToFile { get; set; } = true;

    /// <summary>
    /// Value of Severity enum, 0 = Verbose, 1 = Detail, ...
    /// </summary>
    public uint LogLevelIndex { get; set; } = (uint)Severity.Detail;

    /// <summary>
    /// "Limit storage for a file to:"
    /// </summary>
    public bool MaxStorage { get; set; }

    /// <summary>
    /// Index into the MaxStorageValues array.
    /// 0 = 64 KB, 1 = 128 KB, ...
    /// </summary>
    public uint MaxStorageIndex { get; set; } = 3;

    /// <summary>
    /// Maximum amount of storage for all VS History files,
    /// for a given file, in KB.
    /// </summary>
    public static uint[] MaxStorageValues => [64, 128, 256, 512, 1024, 10240];

    public double NormalFontSize { get; set; } = 13;

    //
    // The following radOrder* are in a group -- exactly one must be true.
    //
    public bool radOrderByDate { get; set; }

    public bool radOrderByFile { get; set; } = true;

    //
    // The following radTabs* are in a group -- exactly one must be true.
    //
    public bool radTabsLeft { get; set; } = true;

    public bool radTabsRight { get; set; }

    public bool radTabsTop { get; set; }

    #endregion Stored Settings

    #region Dynamic Properties

    //
    // The following properties are not saved in the XML file.
    // They are used to control the display of the settings
    // and are computed from the other properties.
    //
    [XmlIgnore]
    public bool LoggingIsDetail => LogLevelIndex <= (uint)Severity.Detail;

    [XmlIgnore]
    public bool LoggingIsInfo => LogLevelIndex <= (uint)Severity.Info;

    [XmlIgnore]
    public bool LoggingIsVerbose => LogLevelIndex <= (uint)Severity.Verbose;

    [XmlIgnore]
    public bool LoggingIsWarning => LogLevelIndex <= (uint)Severity.Warning;

    /// <summary>
    /// The logging severity level is used to determine which messages are logged.
    /// It assumes the values of the items in comboLogLevel are in the same order
    /// as defined in Severity: "Verbose", "Detail", "Information", "Warning", and "Error".
    /// </summary>
    [XmlIgnore]
    public Severity LoggingSeverity => (Severity)LogLevelIndex;

    [XmlIgnore]
    public double SmallFontSize => NormalFontSize - 1.0;

    [XmlIgnore]
    public double SmallerFontSize => NormalFontSize - 2.0;

    [XmlIgnore]
    public double TitleFontSize => NormalFontSize + 3.0;

    #endregion Dynamic Properties

    /// <summary>
    /// Create a copy of the VS_Settings instance.
    ///
    /// All fields will be copied, except the ExcludedDirs and
    /// ExcludedFiles lists will be cleared because they are
    /// already included in processing for all solutions.
    ///
    /// Note that the name is not modified.
    /// </summary>
    /// <returns></returns>
    public object Clone()
    {
        VS_Settings clone = (VS_Settings)MemberwiseClone();
        clone.ExcludedDirs = new();
        clone.ExcludedFiles = new();

        return clone;
    }

    /// <summary>
    /// Create a full copy of the current settings,
    /// optionally setting the name.
    /// </summary>
    /// <returns></returns>
    public VS_Settings FullClone(string sNewName = "")
    {
        VS_Settings clone = (VS_Settings)MemberwiseClone();
        clone.Name = sNewName;

        return clone;
    }

    /// <summary>
    /// Copy the settings from the default to the current solution.
    /// </summary>
    public void CopyDefaultToSolution()
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(SolutionName));
        Debug.Assert(AllVsSettings.Count > 1);

        //
        // Find this solution in AllVsSettings.
        //
        int iIndex = 1;
        for (; iIndex < AllVsSettings.Count; iIndex++)
        {
            if (AllVsSettings[iIndex].Name == SolutionName!)
            {
                break;
            }
        }

        Debug.Assert(iIndex > 0 && iIndex < AllVsSettings.Count);

        //
        // Reset to the default settings with the solution name.
        //
        AllVsSettings[iIndex] = DefaultSettings.FullClone(SolutionName!);
    }

    /// <summary>
    /// Reset settings to the default, which is AllVsSettings[0].
    /// Solution-specific settings are removed.
    /// </summary>
    public void ResetToDefault()
    {
        while (AllVsSettings.Count > 1)
        {
            AllVsSettings.RemoveAt(1);
        }
    }

    /// <summary>
    /// Return AllVsSettings to only include settings that
    /// are different from the default settings.
    /// </summary>
    /// <returns></returns>
    public static List<VS_Settings> PruneSettings()
    {
        Debug.Assert(
            AllVsSettings != null &&
            AllVsSettings.Count > 0 &&
            AllVsSettings[0].Name == "");

        //
        // If there is only the default settings, return it.
        //
        if (AllVsSettings!.Count == 1)
        {
            return AllVsSettings;
        }

        //
        // Get the default settings for use later.
        // The clone of the default settings is used to compare
        // against the other settings because the ExcludedDirs
        // and ExcludedFiles lists are cleared in the clone.
        //
        var defaultSettings = (VS_Settings)DefaultSettings.Clone();

        //
        // Create a new list with just the default settings.
        //
        List<VS_Settings> pruned = [DefaultSettings];

        //
        // Remove the settings for the current solution from the dictionary.
        // This is used when the solution is closed.
        //
        for (int i = 1; i < AllVsSettings.Count; i++)
        {
            VS_Settings settings = AllVsSettings[i];

            if (IsDefault(settings))
            {
                //
                // The settings are the same as the default, so skip them.
                //
                VSLogMsg($"Dropping settings for {settings.Name}", Severity.Verbose);
            }
            else
            {
                //
                // The settings are different from the default, so save them.
                //
                VSLogMsg($"Keeping settings for {settings.Name}", Severity.Verbose);
                pruned.Add(settings);
            }
        }

        return pruned;
    }

    /// <summary>
    /// Convert the VS_Settings instance to an XML string
    /// and write it to the XML file.
    /// </summary>
    /// <returns></returns>
    public static bool SaveXml(List<VS_Settings> vsSettings)
    {
        //
        // Synchronize between possible multiple processes.
        //
        using (var mutex = new Mutex(false, VSSettingsMutexName))
        {
            mutex.WaitOne();
            try
            {
                //
                // Create an XmlWriter to output to the XML file.
                //
                XmlWriterSettings writerSettings = new()
                {
                    //
                    // Make it pretty.
                    //
                    Indent = true,
                    OmitXmlDeclaration = true,
                };

                using XmlWriter writer = XmlWriter.Create(VS_SettingsXmlPath, writerSettings);

                //
                // Serialize and write it.
                //
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VS_Settings>));
                xmlSerializer.Serialize(writer, vsSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to write XML: {ex}");
                return false;
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return true;
        }
    }
}
