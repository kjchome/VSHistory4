using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;

namespace VSHistoryShared;

/// <summary>
/// The settings to filter VS History files.
/// </summary>
public class FilterVersions : INotifyPropertyChanged, ICloneable
{
    /// <summary>
    /// The filename of the file in the VS History directory
    /// that contains the filter settings in XML form.
    /// </summary>
    public static string FilterSettingsName => ".Filter.xml";

    /// <summary>
    /// If true, there are some filter settings.
    /// If false, searchString1 is empty, so no settings.
    /// </summary>
    [XmlIgnore]
    public bool HasFilters => !string.IsNullOrEmpty(searchString1);

    /// <summary>
    /// If true, there are some filter settings for the second string.
    /// </summary>
    [XmlIgnore]
    public bool HasPart2 => !string.IsNullOrEmpty(searchString2);

    /// <summary>
    /// List of filenames that are filtered out.
    /// </summary>
    public HashSet<string> FilteredFiles { get; set; } = new();

    /// <summary>
    /// If true, the file exists.
    /// If false, the file doesn't exist.
    /// If null, unknown.
    /// </summary>
    [XmlIgnore]
    public bool? FileExists { get; } = null;

    /// <summary>
    /// If true, searchString1 must not be found.
    /// </summary>
    public bool Exclude1 { get; set; }

    /// <summary>
    /// If true, searchString2 must not be found.
    /// </summary>
    public bool Exclude2 { get; set; }

    /// <summary>
    /// If true, searchString1 must be found.
    /// </summary>
    public bool Include1 { get; set; } = true;

    /// <summary>
    /// If true, searchString2 must be found.
    /// </summary>
    public bool Include2 { get; set; }

    /// <summary>
    /// If true, first search is case-insensitive.
    /// </summary>
    public bool IgnoreCase1 { get; set; }

    /// <summary>
    /// If true, second search is case-insensitive.
    /// </summary>
    public bool IgnoreCase2 { get; set; }

    /// <summary>
    /// If true, then the version passes if the
    /// second search string is found.
    /// </summary>
    public bool OrInclude { get; set; } = true;

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
    /// filters were applied, e.g., "2025-03-16_11_09_23_754.cs".
    /// </summary>
    public DateTime highestVersion { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Load the settings from the specified path, if it exists.
    /// </summary>
    /// <param name="filterSettingsPath"></param>
    public FilterVersions(string filterSettingsPath)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(filterSettingsPath);
            FileExists = fileInfo.Exists;

            if (fileInfo.Exists)
            {
                using FileStream fileStream = fileInfo.OpenRead();

                //
                // Read the settings from the file into filterSettings.
                //
                XmlSerializer xml = new(typeof(FilterVersions));

                FilterVersions? filterSettings = (FilterVersions)xml.Deserialize(fileStream);

                if (filterSettings != null)
                {
                    //
                    // Copy all the public properties of filterSettings to this.
                    //
                    PropertyInfo[] properties = typeof(FilterVersions)
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public);

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
            Debug.Assert(false, "Failed to parse " + filterSettingsPath);
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
        string sSettingsPath = Path.Combine(versionDir.FullName, FilterSettingsName);
        FileInfo fiSettings = new(sSettingsPath);

        if (settings == null)
        {
            //
            // Read the settings from the settings file, if any.
            //
            settings = fiSettings.Exists ? new(sSettingsPath) : new();
        }

        //
        // If filters are empty, just delete the filter file and we're done.
        //
        if (!settings.HasFilters)
        {
            //
            // Delete the file if it exists.
            //
            fiSettings.Delete();
            return;
        }

        //
        // Use the highest version timestamp to check
        // whether any files have already been checked.
        // If this is DateTime.MinValue, then the entire
        // list of files will be checked.
        //
        DateTime dtHighest = settings.highestVersion;

        Stopwatch sw = Stopwatch.StartNew();
        long bytesRead = 0;
        int iNumFiles = 0;

        //
        // Set the string comparer to ignore case or not.
        //
        StringComparison comparison1 = settings.IgnoreCase1 ?
             StringComparison.CurrentCultureIgnoreCase :
             StringComparison.CurrentCulture;

        StringComparison comparison2 = settings.IgnoreCase2 ?
             StringComparison.CurrentCultureIgnoreCase :
             StringComparison.CurrentCulture;

        //
        // Get all the filenames in the directory in ascending order.
        // (The OrderBy is probably redundant, but...)
        //
        foreach (string sPath in Directory
            .EnumerateFiles(versionDir.FullName, VSHistoryFile.VSHistoryFilenameMask)
            .OrderBy(s => s)
            )
        {
            //
            // Make sure it's a valid filename and see
            // if it has already been checked.
            //
            DateTime dtFile = DateTimeFromFilename(sPath);
            if (dtFile <= dtHighest)
            {
                continue;
            }

            //
            // Extensive testing indicates that it's almost always
            // faster to read the whole file rather than File.ReadLines().
            //
            // There are exceptions to this, of course, like a very large
            // file where an "Include" string is early in the file.
            // But accommodating these exceptions lead to a lot more
            // complexity, so ... meh.
            //
            string sContents = File.ReadAllText(sPath);
            bytesRead += sContents.Length;

            iNumFiles++;

            //
            // We have a new "Highest" timestamp.
            //
            dtHighest = dtFile;

            //
            // We have to check each line of this file.
            //
            // Part1 options: Include1, Exclude1
            // Part2 options: ORInclude, Include2, Exclude2
            //
            // 1. If string1 is found, include if Include1 is set.
            // 2. If string1 is not found, include if Exclude1 is set.
            // 3. If there is any part 2:
            //    a. If not included and ORInclude is set, include if string2 is found
            //    A. If included and Include2 is set, do not include unless string2 is found
            //    B. If included and Exclude2 is set, do not include
            //

            //
            // The file is not included unless it matches the tests.
            //
            bool bIncludeFile = false;

            bool bFound1 = sContents.IndexOf(settings.searchString1, comparison1) >= 0;
            bool bFound2 = false;

            if ((bFound1 && settings.Include1) || (!bFound1 && settings.Exclude1))
            {
                //
                // Part1 passed.  Check part2.
                //
                bIncludeFile = true;
            }

            if (settings.HasPart2)
            {
                //
                // Check ORInclude if we haven't passed yet.
                //
                if (!bIncludeFile && settings.OrInclude)
                {
                    bFound2 = sContents.IndexOf(settings.searchString2, comparison2) >= 0;

                    if (bFound2)
                    {
                        bIncludeFile = true;
                    }
                }
                else if (bIncludeFile)
                {
                    //
                    // The file is included from part 1.  Check other criteria.
                    //
                    bFound2 = sContents.IndexOf(settings.searchString2, comparison2) >= 0;

                    if ((!bFound2 && settings.Include2) || (bFound2 && settings.Exclude2))
                    {
                        //
                        // Part 2 failed.
                        //
                        bIncludeFile = false;
                    }
                }
            }

            //
            // Either add or remove the filename from the
            // list of filtered files.
            //
            string sNameOnly = Path.GetFileName(sPath);
            if (bIncludeFile)
            {
                settings.FilteredFiles.Remove(sNameOnly);
            }
            else
            {
                settings.FilteredFiles.Add(sNameOnly);
            }

            VSLogMsg($"Read {sContents.Length:N0} chars from {Path.GetFileName(sPath)}, " +
                $"Include: {bIncludeFile}, Found1: {bFound1}, Found2: {bFound2}", Severity.Info);
        }

        VSLogMsg($"Read {bytesRead:N0} chars from {iNumFiles:N0} files in {sw.Elapsed}",
            Severity.Info);

        if (dtHighest != settings.highestVersion)
        {
            //
            // Update the settings file with the new "Highest" timestamp.
            //
            try
            {
                settings.highestVersion = dtHighest;

                XmlSerializer xml = new(typeof(FilterVersions));
                using (FileStream fs = fiSettings.Create())
                {
                    xml.Serialize(fs, settings);
                }
            }
            catch
            {
                Debug.Assert(false, "Failed to write the settings file!");
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
