using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace VSHistory;

internal partial class OptionsProvider
{
    /// <summary>
    /// The name that will be displayed in the Options list.
    /// </summary>
    public const string OptionsName = "VaS History Files";

    [ComVisible(true)]
    public class SettingsOptions : BaseOptionPage<Settings> { }

    /// <summary>
    /// Values from a singleton instance of the options.
    /// MUST be called from UI thread only.
    /// </summary>
    public bool KeepLatest => Settings.Instance.NumberOfFiles > 0;
    public bool KeepForTime => Settings.Instance.Days > 0;
}

public class Settings : BaseOptionModel<Settings>
{
    [Category("Enable VSHistory")]
    [DisplayName("Enable Visual Studio History Files")]
    [Description("If enabled, VSHistory can maintain the history of Visual Studio project files. Whenever a file is changed and saved, it will be saved in a special folder for that file.")]
    [DefaultValue(true)]
    public bool VSHistoryEnabled { get; set; } = true;

    [Category("If VSHistory is Enabled")]
    [DisplayName("Keep this may VSHistory files")]
    [Description("Specify the maximum number of VSHistory files to keep for each project file. 0 means keep an unlimited number.")]
    [DefaultValue(0)]
    public uint NumberOfFiles { get; set; }

    [Category("If VSHistory is Enabled")]
    [DisplayName("Days to keep VSHistory files")]
    [Description("Specify the number of days that VSHistory files should be kept. 0 means keep them indefinitely.")]
    [DefaultValue(0)]
    public uint Days { get; set; }

    [Category("If VSHistory is Enabled")]
    [DisplayName("Maximum storage for VSHistory files")]
    [Description("Specify the amount of disk space, in KB, that VSHistory files will use for any given project file.  At least two previous files will always be kept. 0 means unlimited storage.")]
    [DefaultValue(0)]
    public uint MaxKB { get; set; }

    [Category("If VSHistory is Enabled")]
    [DisplayName("Compress (GZIP) VSHistory files")]
    [Description("Specify the minimum size file, in KB, where the VSHistory file will be compressed using GZIP compression. These VSHistory files will have \".gz\" file extensions in the VSHistory directories. 0 means no compression.")]
    [DefaultValue(0)]
    public uint GZip_KB { get; set; }

    [Category("Debugging")]
    [DisplayName("Enable VSHistory Debug Messages")]
    [Description("If enabled, VSHistory Debug messages will be displayed in the Output window.")]
    [DefaultValue(true)]
    public bool DebugMessages { get; set; } = true;
}
