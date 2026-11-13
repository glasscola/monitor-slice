@echo off
setlocal
if defined VCToolsInstallDir goto compile
set "vswhere=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%vswhere%" (echo Visual Studio C++ Build Tools are required. & exit /b 1)
for /f "usebackq tokens=*" %%i in (`"%vswhere%" -latest -prerelease -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "vsroot=%%i"
if not defined vsroot (echo C++ toolchain not found. & exit /b 1)
call "%vsroot%\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b 1
:compile
cd /d "%~dp0"
set "captureOutput=..\MonitorSlice.Capture.dll"
if not "%~1"=="" set "captureOutput=%~1"
cl /nologo /std:c++20 /EHsc /O2 /MT /LD Capture.cpp Icons.cpp DirectOutput.cpp /link /OUT:"%captureOutput%" d3d11.lib dxgi.lib windowsapp.lib runtimeobject.lib user32.lib gdi32.lib ole32.lib oleaut32.lib shell32.lib uuid.lib
exit /b %errorlevel%
