using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VSHistory;

internal partial class OptionsProvider
{
    // Register the options with this attribute on your package class:
    // [ProvideOptionPage(typeof(OptionsProvider.ExclusionsOptions), "VSHistory", "Exclusions", 0, 0, true, SupportsProfiles = true)]
    [ComVisible(true)]
    public class FileExclusionsOptions : BaseOptionPage<FileExclusions> { }
}

public class FileExclusions : BaseOptionModel<FileExclusions>
{
    [Category("My category")]
    [DisplayName("My Option")]
    [Description("An informative description.")]
    [DefaultValue(true)]
    public bool FileExclusionOption { get; set; } = true;

    public DateTimeOffset DateTimeX { get; set; } = DateTime.Now;
    public DateTimeOffset DateTime2 { get; set; } = DateTime.Now;
}
