using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VSHistoryShared;

/// <summary>
/// The settings to filter VS History files.
/// </summary>
public class FilterVersions : INotifyPropertyChanged, ICloneable
{
    /// <summary>
    /// Suffix applied to VS History versions to indicate that
    /// they are filtered out, e.g., "2025-03-16_11_09_23_754-.cs".
    /// </summary>
    public static char FilterSuffix => '-';

    /// <summary>
    /// The filename of the file in the VS History directory
    /// that contains the filter settings in JSON form.
    /// </summary>
    public static string FilterSettingsFilename => ".Filter.json";

    /// <summary>
    /// If true, there are some filter settings.
    /// If false, searchString1 is empty, so no settings.
    /// </summary>
    [JsonIgnore]
    public bool HasFilters => !string.IsNullOrEmpty(searchString1);

    /// <summary>
    /// If true, the file exists.
    /// If false, the file doesn't exist.
    /// If null, unknown.
    /// </summary>
    [JsonIgnore]
    public bool? FileExists { get; } = null;

    /// <summary>
    /// If true, searchString1 must not be found.
    /// </summary>
    public bool radExclude1 { get; set; } = false;

    /// <summary>
    /// If true, searchString2 must not be found.
    /// </summary>
    public bool radExclude2 { get; set; } = false;

    /// <summary>
    /// If true, searchString1 must be found.
    /// </summary>
    public bool radInclude1 { get; set; } = true;

    /// <summary>
    /// If true, searchString2 must be found.
    /// </summary>
    public bool radInclude2 { get; set; } = false;

    /// <summary>
    /// If true, then the version passes if the
    /// second search string is found.
    /// </summary>
    public bool radOrInclude { get; set; } = true;

    /// <summary>
    /// The first search string.  If empty, then
    /// there are no filters to apply.
    /// </summary>
    public string searchString1 { get; set; } = "";

    /// <summary>
    /// The second search string.  This is only used
    /// if the first search string is not empty.
    /// </summary>
    public string searchString2 { get; set; } = "";

    /// <summary>
    /// The filename of the highest version since the
    /// filters were applied, e.g., "2025-03-16_11_09_23_754",
    /// with no suffixes or file type.
    /// </summary>
    public string highestVersion { get; set; } = "";

    /// <summary>
    /// Load the settings from the specified path, if it exists.
    /// </summary>
    /// <param name="filterJsonPath"></param>
    public FilterVersions(string filterJsonPath)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(filterJsonPath);
            FileExists = fileInfo.Exists;

            if (fileInfo.Exists)
            {
                //
                // Read the JSON from the file into filterSettings.
                //
                FilterVersions? filterSettings =
                    JsonSerializer.Deserialize<FilterVersions>(fileInfo.OpenRead());

                if (filterSettings != null)
                {
                    //
                    // Copy all the public properties of filterSettings to this.
                    //
                    PropertyInfo[] properties = typeof(FilterVersions)
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public );

                    foreach (PropertyInfo property in properties)
                    {
                        //
                        // Set the value if the field has a set accessor.
                        //
                        if (property.GetSetMethod() != null)
                        {
                            property.SetValue(this, property.GetValue(filterSettings, null), null);
                        }
                    }
                }
            }
        }
        catch
        {
            Debug.Assert(false, "Failed to parse " + filterJsonPath);
        }
    }

    public FilterVersions()
    {
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public object Clone()
    {
        return MemberwiseClone();
    }

    /// <summary>
    /// Determine whether all the fields in another FilterVersions match this one.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        FilterVersions filterOther = (FilterVersions)obj;

        //
        // Iterate through all the fields.
        //
        PropertyInfo[] properties = typeof(FilterVersions)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (PropertyInfo property in properties)
        {
            //
            // Since GetValue() is considered a "reference type", we cannot compare
            // them directly.  We must convert them to strings first and compare those.
            //
            if (property.GetValue(this, null).ToString() != property.GetValue(filterOther, null).ToString())
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    /// <summary>
    /// Filter the version files in versionDir per set FilterVersions in settings.
    /// </summary>
    /// <param name="versionDir"></param>
    /// <param name="settings"></param>
    public static void Filter(DirectoryInfo versionDir, FilterVersions? settings = null)
    {
        if (settings == null)
        {
            //
            // Read the settings from the settings file, if any.
            //
            string sSettingsPath = Path.Combine(versionDir.FullName, FilterVersions.FilterSettingsFilename);
            settings = new(sSettingsPath);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
