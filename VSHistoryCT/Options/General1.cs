using System.ComponentModel;
using System.Runtime.InteropServices;

using Community.VisualStudio.Toolkit;

namespace VSHistory;

internal partial class OptionsProvider
{
    [ComVisible(true)]
    public class General1Options : BaseOptionPage<General1> { }

    [ComVisible(true)]
    public class SubGeneralOptions : BaseOptionPage<SubGeneral> { }
}

public class General1 : BaseOptionModel<General1>
{
    //[Category("My category")]
    [DisplayName("My Option")]
    [Description("An informative description.")]
    [DefaultValue(true)]
    public bool MyOption { get; set; } = true;
}

public class SubGeneral: BaseOptionModel<SubGeneral>
{
    //[Category("My subcategory")]
    [DisplayName("My subOption")]
    [Description("An informative description.")]
    [DefaultValue(true)]
    public bool SubMyOption { get; set; } = true;
}
