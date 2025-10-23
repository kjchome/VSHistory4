using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace VSHistory;

/// <summary>
/// Interaction logic for TabDateFormat.xaml
/// </summary>
public partial class TabDateFormat : UserControl
{
    public TabDateFormat()
    {
        InitializeComponent();
    }

    public void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // for debugging
        //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
        //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

        CultureInfo cultureUI = CultureInfo.CurrentUICulture;
        CultureInfo culture = CultureInfo.CurrentCulture;

        DateTime dateTime = DateTime.Now;

        string sLong = LocalizedString("Long", cultureUI);
        labLongDate.Content = $"{sLong} ({cultureUI.Name})*";
        labLongCurrentDate.Content = $"{sLong} ({culture.Name})*";

        string sShort = LocalizedString("Short", cultureUI);
        labShortDate.Content = $"{sShort} ({cultureUI.Name})";
        labShortCurrentDate.Content = $"{sShort} ({culture.Name})";

        Date_Long.Content =
            dateTime.ToString("dddd, ", cultureUI) +
            dateTime.ToString("d", cultureUI) + " " +
            dateTime.ToString("T", cultureUI);

        Date_Short.Content =
            dateTime.ToString("ddd ", cultureUI) +
            dateTime.ToString("d", cultureUI) + " " +
            dateTime.ToString("T", cultureUI);

        Date_LongCurrent.Content =
            dateTime.ToString("dddd, ", culture) +
            dateTime.ToString("d", culture) + " " +
            dateTime.ToString("T", culture);
        
        Date_ShortCurrent.Content =
            dateTime.ToString("ddd ", culture) +
            dateTime.ToString("d", culture) + " " +
            dateTime.ToString("T", culture);

        if (culture.Name == cultureUI.Name)
        {
            //
            // They're the same -- no need for the second set of long/short dates.
            //
            labLongCurrentDate.IsEnabled = false;
            Date_LongCurrent.IsEnabled = false;

            labShortCurrentDate.IsEnabled = false;
            Date_ShortCurrent.IsEnabled = false;
        }

        Date_ISO.Content = dateTime.ToString("yyyy-MM-dd HH:mm:ss");

        dateTime = dateTime.ToUniversalTime();
        Date_ISO_UT.Content = dateTime.ToString("yyyy-MM-dd HH:mm:ssZ");

        string sToday = LocalizedString("Today", cultureUI);
        string sYesterday = LocalizedString("Yesterday", cultureUI);

        labToday.Content = $"* The {sLong} formats will display \"{sToday}\" " +
            $"or \"{sYesterday}\"\r\n   in place of the day as appropriate.";
    }
}
