Build Prerequisites:

- Visual Studio 2012
- InnoSetup (in MediaPoint\Build_dependencies\issetup.exe)
- DirectX 9.0c SDK http://www.microsoft.com/en-ca/download/details.aspx?id=6812
- Windows 7 SDK 
- WPF ShaderEffects Build Task "MediaPoint\Build_dependencies"

When building VSFilter in debug mode VSFilter wont show subtitles. Therefore VSFilter builds in release always.

Build process goes like this:

- BaseClasses project is compiled, which runs a post build event
- Post build event executes the yasm batch file in the solution folder (if this fail execute the bat file manually yasm_obj_build_all.bat)
- All other VSFilter projects are compiled
- VSFilter output is copied to Libs/codex/%Proc_Arch%
- All MediaPoint projects are compiled in order
- MediaPoint.App runs a build event that copies all files from Libs/AllArchitectures and Libs/%Proc_Arch% to the output folder
- Output folder "output/bin/%Proc_Arch%" contains a runnable MediaPoint
- If installer is build (for now works only x86) it compiles in "output/Installer", you need to install Build_dependencies\issetup.exe


Folders: include, src, lib are related to VSFilter (c++)

All output (intermediate obj and final) goes to output.