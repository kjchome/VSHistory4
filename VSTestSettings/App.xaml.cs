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
    /// Display the VSHistory window for testing.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Application_Startup(object sender, StartupEventArgs e)
    {
        //
        // To test the SearchFiles window.
        //
        SearchFiles sSearch = new();
        sSearch.ShowDialog();

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