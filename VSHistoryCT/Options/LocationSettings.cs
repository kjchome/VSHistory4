using System.ComponentModel;
using System.Runtime.InteropServices;

namespace VSHistory;
internal partial class OptionsProvider
{
    // Register the options with this attribute on your package class:
    // [ProvideOptionPage(typeof(OptionsProvider.LocationOptions), "VSHistory", "Location", 0, 0, true, SupportsProfiles = true)]
    [ComVisible(true)]
    public class LocationOptions : BaseOptionPage<LocationSettings> { }
}

public class LocationSettings : BaseOptionModel<LocationSettings>
{
    [Category("My category")]
    [DisplayName("My Option")]
    [Description("An informative description.")]
    [DefaultValue(true)]
    public bool MyOption { get; set; } = true;
}
