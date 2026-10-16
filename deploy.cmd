@echo off
rem Builds the component and copies the DLL into LiveSplit's Components folder, which LiveSplit loads it from.
rem Usage: deploy [Debug|Release]   (Debug if left out)
rem LiveSplit is expected where the csproj's HintPath points: ..\..\Programs\Streaming\LiveSplit from the repo.
setlocal
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Debug"
set "COMPONENTS=%~dp0..\..\Programs\Streaming\LiveSplit\Components"

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

dotnet build "%~dp0LiveSplit.OutputToFile.sln" -c %CONFIG%
if errorlevel 1 exit /b 1

copy /Y "%~dp0bin\%CONFIG%\LiveSplit.OutputToFile.dll" "%COMPONENTS%\" >nul
if errorlevel 1 exit /b 1
echo Deployed bin\%CONFIG%\LiveSplit.OutputToFile.dll to %COMPONENTS%
