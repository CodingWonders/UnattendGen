@echo off

where dotnet >nul 2>nul

if %ERRORLEVEL% gtr 0 (
	echo .NET SDK not installed. Exiting...
	exit /b
)

if exist "bin\release\net9.0" (
	rd "bin\release\net9.0" /s /q
)

cls

set DOTNET_CLI_TELEMETRY_OPTOUT=1

echo Publishing self-contained binaries...

:: Publish self-contained versions
echo 1 of 3 -- Windows Binaries
dotnet publish -r:win-x86 --sc -o "bin\release\net9.0\sc\win-x86" --framework net9.0
dotnet publish -r:win-x64 --sc -o "bin\release\net9.0\sc\win-x64" --framework net9.0
dotnet publish -r:win-arm64 --sc -o "bin\release\net9.0\sc\win-arm64" --framework net9.0
echo 2 of 3 -- macOS Binaries
dotnet publish -r:osx-x64 --sc -o "bin\release\net9.0\sc\osx-x64" --framework net9.0
dotnet publish -r:osx-arm64 --sc -o "bin\release\net9.0\sc\osx-arm64" --framework net9.0
echo 3 of 3 -- Linux Binaries
dotnet publish -r:linux-x64 --sc -o "bin\release\net9.0\sc\linux-x64" --framework net9.0
dotnet publish -r:linux-arm64 --sc -o "bin\release\net9.0\sc\linux-arm64" --framework net9.0

if exist "bin\release\net9.0\sc" (
	echo Zipping self-contained binaries...
	powershell -ExecutionPolicy Bypass ".\SelfContainedZip.ps1"
	rd "bin\release\net9.0\sc" /s /q
)

echo Publishing regular binaries...

:: Publish release for x86, amd64 and arm64 (win, osx, linux)
for %%a in (win-x86 win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64) do (
	echo ------------------------ Building regular binary for target %%a ------------------------
	dotnet publish -r:%%a --framework net9.0
	rd "bin\release\net9.0\%%a\publish" /s /q
)

echo Zipping regular binaries...
powershell -ExecutionPolicy Bypass ".\RegularZip.ps1"