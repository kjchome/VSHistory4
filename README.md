**NOTE:** The version of VSHistory compatible with **Visual Studio 2019** can be found [here](https://marketplace.visualstudio.com/items?itemName=KenCross.VSHistory).

## VSHistory

VSHistory for Visual Studio saves a copy of your files every time you save them. They are stored in a special `.vshistory` directory and can be opened or differences with the current file can be viewed.   

A custom VSHistory tool window displays each version of project files as they're saved.  With each version, you can:

* View the difference between that version and your current file.
* View the difference between that version and another version.
* Open that version in Visual Studio.

**VSHistory version 4 is a complete re-write of VSHistory** using the [Community.VisualStudio.Toolkit.17](https://www.nuget.org/packages/Community.VisualStudio.Toolkit.17/) package. For details, see _Changes in VSHistory 4_ below.


### Current File History

The VSHistory tool window showing the history of the current file (the document that currently has focus) can be displayed from the menu via **`Extensions | VSHistory | VSHistory Tool Window`**

It is recommended that you dock the VSHistory tool window somewhere, such as:

![Full Screen](FullScreen.png)

You can view the difference between any version with the current file by clicking **`Diff`** for that version in the VSHistory tool window.  Clicking **`Open`** will simply display that version of the file.

You can see the difference between two VSHistory versions by checking the checkboxes of 2 VSHistory versions.

## VSHistory Settings

You can access the VSHistory Settings using the menu **`Extensions | VSHistory | Settings`** or clicking the **`Settings`** button on the VSHistory tool window.

The settings are saved in an XML file in `%LOCALAPPDATA%\VSHistory\Settings.xml`. Each user has their own settings on the computer.

> **Note:** You must click the **`OK`** button to save any changes to the settings.

![Settings General](Settings_General.png)

### General

#### Default and Solution Settings

The Default Settings apply to all Visual Studio solutions unless you explicitly change the settings for a solution. However, once you make changes to the settings for a solution, changes to the Default Settings will no longer apply to that solution.

#### Copy Default Settings

The `Copy defaults to all solutions` button will delete any settings you have made for all solutions.

The `Copy default settings to this solution` button will delete any settings you have made for the current solution. Settings for other solutions won't be changed.

#### Enable Visual Studio History

If this checkbox is cleared, VSHistory versions will no longer be saved. Any existing VSHistory versions will remain.

The other General settings let you select how many versions should be maintained:

* Limit the number of VSHistory  versions that should be kept (default is unlimited)
* Specify how long versions should be kept (default is unlimited)
* Don't save versions more often than once every "n" seconds/minutes/hours
* Set the maximum amount of disk space used for versions (but at least 2 versions will always be kept).
* Indicate whether VSHistory versions should be compressed using GZIP compression when they are saved.  This can save disk space if there are a lot of large files being saved in the VSHistory directories. They will automatically be decompressed before use.

### All VSHistory Versions

This will display **all** VSHistory versions for the current solution. You can choose to display them ordered by filename or by when they were saved. (You can also access this from the **`All Files`** button on the VSHistory Tool Window.)

Double-clicking a version will display a diff of that version with the current file.

The `Delete All` button will delete all VSHistory versions for the current solution.  This is a permanent deletion and cannot be undone.

You can delete individual versions by clicking the `Open` button on the VSHistory Tool Window. This will open the directory containing the versions and you can delete any or all of them. 

> NOTE: When a file in your solution is deleted, its VSHistory versions are kept.  The files that were deleted will have `(deleted)` next to their name.  Double-clicking a version will display that version.

![Settings AllFiles_1](Settings_AllFiles_1.png)

### Location of VSHistory Files

By default, VSHistory files are maintained in subdirectories below the source files.  You can optionally select an alternate location for VSHistory files.

> NOTE: If the VSHistory files are maintained with the source files and you use a source control system like git, you may want to add the `.vshistory` directory to be excluded in the `.gitignore` file.

![Settings Location](Settings_Location.png)

[Help Me Choose the Location](Help_Location.png)

### File Exclusions

You can choose to have a variety of files be excluded from VSHistory processing.

> NOTE: Any file exclusions specified in Default Settings apply to **all** solutions.

[Help for File Exclusions](Help_FileExclusions.png)

### Directory Exclusions

You can choose to have a variety of directories be excluded from VSHistory processing.  All files in that directory and any subdirectories will be excluded.

> NOTE: Any directory exclusions specified in Default Settings apply to **all** solutions.

[Help for Directory Exclusions](Help_DirExclusions.png)

### Changes in VSHistory 4.0

#### Updated Settings

The VSHistory settings are now managed in a tabbed window rather than through the Options pages. The settings can be accessed through **`Extensions | VSHistory | Settings`** or by clicking the **`Settings`** button on the VSHistory tool window.

#### Separate Solution Settings

You can now specify settings on a per-solution basis. For example, you can exclude some files or directories from one solution but not another.

You can also have a different location where the VSHistory files are stored for each solution.  The default is to keep the VSHistory files with the solution.  See _Location of VSHistory Files_.

#### Support for Long Paths

Visual Studio doesn't natively support long paths for files or directories (>260 characters). Since VSHistory can potentially produce long paths, the paths used in VSHistory include the prefix to support long paths, such as `\\?\C:\SomeDir\MyProject\Genius.cs`.

This works regardless of whether or not long paths are enabled in the registry (`HKLM\SYSTEM\CurrentControlSet\Control\FileSystem\LongPathsEnabled `).

#### Easier Access to the VSHistory Tool Window

The VSHistory Tool Window can now be opened through **`Extensions | VSHistory | VSHistory Tool Window`**. It is no longer shown in `View | Other Windows`. 

Once you open the VSHistory Tool Window, it is recommended to dock it somewhere convenient.

#### Views are Shown in the Preview Window

Views of differences are now shown in the "Preview" or "Provisional" window. You have the option to "pin" the view if you want to keep it around.

#### User-defined Tags are No Longer Supported

I think this feature was rarely used and added complexity. If there is enough demand, I will consider restoring it.

#### More Comprehensive File and Directory Exclusions

General patterns can be used to specify files and/or directories that should be excluded from VSHistory processing.

File and directory exclusions in the Default Settings apply to all solutions, but each solution can add their own exclusions.

#### Better Localization

The VSHistory Tool Window has better localization. The rest of VSHistory still has very little localization.

#### Comprehensive Logging Capabilities

Extensive logging is available although it is primarily used for testing and debugging.  It is disabled by default.

#### VSTestSettings Program

The Settings are much more elaborate than in previous versions so a separate program is included just for testing and development of the Settings.

It shares much of the code of the actual VSHistory extension but only shows the Settings page.  It is not included in VSHistory.vsix but is included in the source code.

#### Published Source Code

The VSHistory source code is now available in [GitHub](https://github.com/kjchome/VSHistory4).

## Revision History

* 4.0.7 The current live project file is now saved with the previous versions whenever you save a file.
* 4.0.6 Update README.md
* 4.0.5 Bug fixes and requested improvements
    * Added a `Delete All` button to the **All VSHistory Versions** page to delete all versions for the current solution.
    * Fixed a problem where the right side of the differences page didn't edit the live file.
    * Fixed Bug #1 -- null item when enumerating children.
* 4.0.4 Complete re-write.  See _Changes in VSHistory 4_ above for details.
* 3.10 Make column widths resizable
* 3.9 Mouseover of "Size" now shows the true "Size on disk" of a VSHistory version (may be zero for small files)
* 3.8 Allow non-admin installations (no longer "All Users", so each user must install)
* 3.7 Fixed handling of a rare case of a non-existent directory that is in the solution.
* 3.6 Improved support for localized date/time formats, including regional settings.
* 3.5 Incorporate fix for detecting "dirty" documents in Visual Studio 2022 17.4 and higher.
* 3.4 Make saving versions unconditional due to quirky VS2022 behavior in detecting documents as "dirty" (changed)
    * Added option to enable viewing VSHistory Debug Messages
* 3.3 Minor fixes and new feature
    * Added ability to only save versions once every "n" seconds/minutes/hours
    * Handle a rare exception trying to find the active document.
* 3.2 Stability and performance update
    * Improved performance
    * Better tracking of the "current" document, especially if the VSHistory toolbox is not visible
    * Minor bug fixes
* 3.1.5 Assorted fixes and improvements
    * Added setting to select the file size threshold for GZIP compression
    * Better support for color themes
    * Localization of dates/times based on Visual Studio International Settings
    * Minor cosmetic fixes
* 3.1.4 Fixed icon, added license, removed "Preview"
* 3.1.2 Cosmetic fix on Exclusions settings page
* 3.1.1 Fixed some cosmetic issues.  Limit installation to **Visual Studio 2019 16.9.11 and higher** (but not Visual Studio 2022).
* 3.1.0 Add features and bring VSHistory for 2019 and 2022 into sync.
    * Removed support for Visual Studio 2017
    * Support displaying difference between 2 VSHistory files by selecting checkboxes
    * Options to set the date format, including long, short and ISO formats
    * Use standard built-in image monikers to accommodate different screen resolutions better
    * "Modernized" the VSIX Project
* 2.4.2 Restored creating VSHistory directories as compressed.
* 2.4.1 Added option to determine whether large files are gzipped when saved.
* 2.4.0 Added support for excluding files.  Fixed problem where **All History Files** were sometimes empty even though there were some VSHistory files.
* 2.3.0 VSHistory files are now gzipped  to reduce storage and you can select any or all of the options to limit file versions. Previous (unzipped) version files are fully supported.  Added some performance improvements.
* 2.2.2 Fix problem with "transparent" window; show deleted files in **All History Files** when in Folder view
* 2.2.1 Fix problem displaying All VSHistory Files when in Folder View
* 2.2.0 Added support for using an alternate directory for storing VSHistory files
* 2.1.0 Added support for user-defined tags. Fixed a problem with VS extensions that was introduced with the release version of .NET Framework 4.8 which caused the toolbox and settings pages to appear blank.  Added support to adjust colors to different themes, like Dark Theme.
* 2.0.4 Added feature to view **All History Files** by date/time
* 2.0.3 Fix a problem where settings weren't getting save/restored properly
* 2.0.2 Initial release of VSHistory 2.0

