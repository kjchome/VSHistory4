global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.IO;
global using System.Linq;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Threading;
global using System.Threading.Tasks;

global using Community.VisualStudio.Toolkit;

global using Microsoft.VisualStudio.Shell;
global using Microsoft.VisualStudio.Shell.Interop;

global using Microsoft.Win32.SafeHandles;

global using VSHistoryShared;

global using static VSHistory.VSHistoryPackage;
global using static VSHistory.VSHistoryToolWindow;
global using static VSHistoryShared.SolutionInfo;
global using static VSHistoryShared.VS_Settings;
global using static VSHistoryShared.VSHistoryUtilities;
global using static VSHistoryShared.VSLog;
global using static VSHistoryShared.Win32Imports;
global using static VSHistoryShared.Win32Utilities;

global using Task = System.Threading.Tasks.Task;

using VSHistory.Events;

using static VSHistory.Events.VSHistorySolutionEvents;

namespace VSHistory;
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]

//
// The VSHistory Tool Window that shows all the VSHistory
// files for the file currently open for editing.
//
[ProvideToolWindow(typeof(VSHistoryToolWindow.Pane),
    Style = VsDockStyle.Float, Window = WindowGuids.PropertyBrowser)]

[ProvideMenuResource("Menus.ctmenu", 1)]

//
// This causes our extension to be loaded if a solution
// exists, even if no documents are in the main window.
//
[ProvideAutoLoad(UIContextGuids80.SolutionExists,
    PackageAutoLoadFlags.BackgroundLoad)]

[Guid(PackageGuids.VSHistoryString)]
public sealed class VSHistoryPackage : ToolkitPackage
{
    /// <summary>
    /// The pane in the Output Window if logging is
    /// enabled and sent to the Output Window.
    /// </summary>
    public static OutputWindowPane? g_DebugPane { get; set; }

    /// <summary>
    /// Get the solution info at startup, if any.
    /// This is used to determine if the solution exists
    /// at startup and we need to keep it even if is
    /// never referenced.
    /// </summary>
    public static SolutionInfo g_SolutionInfo { get; set; } = new();

    public static VSHistoryToolWindowControl? g_VSControl { get; set; }

    public static ToolWindowPane? g_VSToolWindowPane { get; set; }

    /// <summary>
    /// Initialization of the package. This method is called
    /// right after the package is sited, so this is the place
    /// where you can put all the initialization code that
    /// rely on services provided by VisualStudio.
    /// </summary>
    protected override async Task InitializeAsync(
        CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        VSLogMsg(VSVersion());

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await base.InitializeAsync(cancellationToken, progress);

        //
        // Open the Output window lazily -- it won't actually open until written to.
        //
        g_DebugPane = await OutputWindowPane.CreateAsync("VSHistory Log");

        //
        // Register commands, i.e., things with the [Command] attribute.
        //
        await this.RegisterCommandsAsync();

        //
        // Register our tool window.
        //
        this.RegisterToolWindows();

        //
        // Set our event handlers.
        //
        _ = new VSHistoryDocumentEvents();

        SolutionEvents solutionEvents = VS.Events.SolutionEvents;

        solutionEvents.OnAfterOpenSolution += SolutionEvents_OnAfterOpenSolution;
        solutionEvents.OnAfterCloseSolution += SolutionEvents_OnAfterCloseSolution;
        solutionEvents.OnAfterOpenFolder += SolutionEvents_OnAfterOpenFolder;

        //
        // Watch for changes to the Settings file.
        //
        _ = new SettingsWatcher();
    }

    /// <summary>
    /// Get the path of the currently active document or null if not found.
    /// </summary>
    /// <returns></returns>
    public static string? GetActiveDocument()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        Documents docs = new();
        DocumentView? docView = ThreadHelper.JoinableTaskFactory.Run
            (() => docs.GetActiveDocumentViewAsync());

        VSLogMsg($"Active document: '{docView?.FilePath ?? "null"}'", Severity.Verbose);

        return docView?.FilePath;
    }
}
