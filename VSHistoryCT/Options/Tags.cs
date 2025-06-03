using System.ComponentModel;
using System.Runtime.InteropServices;

namespace VSHistory;

internal partial class OptionsProvider
{
    // Register the options with this attribute on your package class:
    // [ProvideOptionPage(typeof(OptionsProvider.TagsOptions), "VSHistory", "Tags", 0, 0, true, SupportsProfiles = true)]
    [ComVisible(true)]
    public class TagsOptions : BaseOptionPage<Tags> { }
}

public class Tags : BaseOptionModel<Tags>
{
    [Category("My category")]
    [DisplayName("My Option")]
    [Description("An informative description.")]
    [DefaultValue(true)]
    public bool MyOption { get; set; } = true;
}
