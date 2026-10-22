@echo off
rem Builds the component and copies the DLL into LiveSplit's Components folder, which LiveSplit loads it from.
rem Usage: deploy [Debug|Release]   (Debug if left out)
rem LiveSplit's folder is the LiveSplitDir environment variable, or ..\..\Programs\Streaming\LiveSplit from the repo, as in the csproj.
rem The build reads LiveSplitDir from the environment too, so it references the LiveSplit the DLL is copied into.
setlocal
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Debug"
if not defined LiveSplitDir set "LiveSplitDir=%~dp0..\..\Programs\Streaming\LiveSplit"
set "COMPONENTS=%LiveSplitDir%\Components"

rem LiveSplit locks the DLL while it runs, so the copy would fail. findstr by full path: Git's tools can shadow Windows' find.
tasklist /FI "IMAGENAME eq LiveSplit.exe" /NH | "%SystemRoot%\System32\findstr.exe" /I /C:"LiveSplit.exe" >nul
if not errorlevel 1 (
	echo LiveSplit is running. Close it first: it locks the DLL.
	exit /b 1
)
if not exist "%COMPONENTS%\" (
	echo LiveSplit's Components folder isn't at %COMPONENTS%
	exit /b 1
)

rem The project, not the solution: the tests in Tests\ aren't needed to deploy
dotnet build "%~dp0LiveSplit.OutputToFile.csproj" -c %CONFIG%
if errorlevel 1 exit /b 1

copy /Y "%~dp0bin\%CONFIG%\LiveSplit.OutputToFile.dll" "%COMPONENTS%\" >nul
if errorlevel 1 exit /b 1
echo Deployed bin\%CONFIG%\LiveSplit.OutputToFile.dll to %COMPONENTS%
