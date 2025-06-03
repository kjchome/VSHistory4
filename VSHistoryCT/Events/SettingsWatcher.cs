
namespace VSHistory.Events;

/// <summary>
/// Watch for changes to the Settings file.
/// </summary>
internal class SettingsWatcher
{
    public SettingsWatcher()
    {
        //
        // We want to watch the settings file, e.g.,
        // C:\Users\user\AppData\Local\VSHistory\Settings.xml
        //
        FileInfo fiSettings = new(VS_SettingsXmlPath);
        DirectoryInfo diSettings = new(fiSettings.DirectoryName);

        //
        // The directory must exist.
        //
        diSettings.Create();

        //
        // Set the watcher to just look for changes to Settings.xml.
        //
        // N.B. FileSystemWatcher doesn't support the long path
        //      prefix \\?\C:\... but that *shouldn't* be a
        //      problem in the path using %LOCALAPPDATA%.
        //
        FileSystemWatcher fileSystemWatcher = new(diSettings.FullName, fiSettings.Name);
        fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;

        //
        // Add our event handler and enable it.
        //
        fileSystemWatcher.Changed += FileSystemWatcher_Changed;
        fileSystemWatcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Handle changes to the Settings file.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        //
        // The settings file has been written to, either
        // by us or another instance of Visual Studio.
        //
        // Force a refresh of the settings.
        //
        ResetAllSettings();
    }
}
