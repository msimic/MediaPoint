Build Prerequisites:

- Visual Studio 2012
- Installshield 2013 LE
- DirectX 9.0c SDK
- Windows 7 SDK
- WPF ShaderEffects Build Task

When building in debug mode the application will crush when loading file subtitles because of strange bug with our custom VSFilter.
You can overcome that by setting the VSFilter project to be built in release on the debug configuration. Don't commit that.
The release build works ok.

Build process goes like this:

- BaseClasses project is compiled, which runs a post build event
- Post build event executes the yasm batch file in the solution folder
- All other VSFilter projects are compiled
- VSFilter output is copied to Libs/codex/%Proc_Arch%
- All MediaPoint projects are cimpiled in order
- MediaPoint.App runs a build event that copies all files from Libs/AllArchitectures and Libs/%Proc_Arch% to the output folder
- Output folder "output/bin/%Proc_Arch%" contains a runnable MediaPoint
- If installer is build (for now works only x86) it compiles InstallTool and then produces output in "output/Installer"


Folders: include, src, lib are related to VSFilter (c++)

All output (intermediate obj and final) goes to output.