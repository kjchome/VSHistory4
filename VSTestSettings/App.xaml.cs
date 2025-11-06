global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.IO;
global using System.Linq;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Threading;

global using Community.VisualStudio.Toolkit;

global using Microsoft.VisualStudio.Shell;
global using Microsoft.VisualStudio.Shell.Interop;

global using Microsoft.Win32.SafeHandles;

global using static VSHistoryShared.SolutionInfo;
global using static VSHistoryShared.Win32Utilities;

global using Task = System.Threading.Tasks.Task;

using System.Runtime.CompilerServices;
using System.Windows;

namespace VSHistory;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Application startup event handler.
    /// This is set in the App.xaml file.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Application_Startup(object sender, StartupEventArgs e)
    {
        //
        // To test the VersionFilters window.
        //
        string sPath = @"C:\Users\kjcho\source\repos\VSHistory4\VSTestSettings\App.xaml.cs";
        //@"\\?\C:\Users\kjcho\source\repos\WeatherV2\.vshistory\mainwindow.xaml.cs"));
        //@"\\?\C:\Users\kjcho\source\repos\KJCWeatherWFP\.vshistory\WaterTemp.xaml.cs"));
        //@"\\?\C:\Users\kjcho\source\repos\FileKeeper11d\FKControl\.vshistory\FKControl.xaml.cs"));

        VersionFilters winFilter = new(new VSHistoryFile(sPath));
        winFilter.ShowDialog();

        //
        // To test the Settings window.
        //
        //TestStartup();
    }

    private void TestStartup([CallerFilePath] string sourceFilePath = "")
    {
        //
        // For testing purposes, get all the files in this solution (VSTestSettings).
        //
        // sourceFilePath is the path to this file, e.g., 
        // C:\SomeDir\VSTestSettings\App.xaml.cs
        //
        string sDir = Path.GetDirectoryName(sourceFilePath);
        SolutionName = Path.GetFileName(sDir);
        SolutionPath = LongPath(sDir);

        ProcessDirs(sDir);

        //
        // Start with one of these tabs selected:
        //
        //  tabAllFiles
        //  tabDateFormat
        //  tabDirectoryExclusions
        //  tabFileExclusions
        //  tabFileLocation
        //  tabGeneral
        //  tabLogging
        //
        MainWindow mainWindow = new("tabAllFiles");

        mainWindow.ShowDialog();

        //
        // Done.
        //
        Shutdown();
    }
}