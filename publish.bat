@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

where dotnet >nul 2>nul

if %ERRORLEVEL% gtr 0 (
	echo .NET SDK not installed. Exiting...
	exit /b
)

if exist "bin\release\net9.0" (
	rd "bin\release\net9.0" /s /q
)

cls

echo UnattendGen Release Publish
echo (c) 2024-2026. CodingWonders Software

REM Publish Script Options
REM ----------------------------------------------------------
REM - This option specifies whether to disable dotnet publish telemetry.
REM   - Set to 1 to disable telemetry
REM   - Set to 0 to enable telemetry
set DOTNET_CLI_TELEMETRY_OPTOUT=1
REM - This option adds compatibility for Intel (x64) releases of macOS
REM   - Set to 1 to add compatibility
REM   - Set to 0 to remove compatibility
REM   After .NET either stops supporting Intel systems or macOS Tahoe, this option will be disabled
set UNATTENDGEN_ADD_OSX_X64_COMPAT=1

echo.
echo OPTIONS:
echo - Opt out of dotnet telemetry: %DOTNET_CLI_TELEMETRY_OPTOUT%
echo - Compile build for Intel macOS: %UNATTENDGEN_ADD_OSX_X64_COMPAT%
echo.
echo.

echo Publishing self-contained binaries...

:: Publish self-contained versions
for %%a in (win-x86 win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64) do (
	echo ------------------------ Building self-contained binary for target %%a ------------------------
	:: Yes, this is not the ideal way to do this, but batch is so archaic there's no CONTINUE statement
	set target="%%a"
	if "!target:~1,3!" NEQ "win" (
		call :nonwindowswarning %%a
	)
	if "%%a" == "osx-x64" (
		if !UNATTENDGEN_ADD_OSX_X64_COMPAT! EQU 0 (
			echo Platform %%a not supported by publish flags.
			echo Set the UNATTENDGEN_ADD_OSX_X64_COMPAT option in this script to 1 to enable Intel builds of UnattendGen for macOS.
			echo.
			echo After a .NET version either stops supporting Intel systems or macOS Tahoe, both this flag and the %%a target will be removed
			echo from this script.
		) else (
			dotnet publish -r:%%a --sc -o "bin\release\net9.0\sc\%%a" --framework net9.0
		)
	) else (
		dotnet publish -r:%%a --sc -o "bin\release\net9.0\sc\%%a" --framework net9.0
	)
)

if exist "bin\release\net9.0\sc" (
	echo Zipping self-contained binaries...
	powershell -ExecutionPolicy Bypass ".\SelfContainedZip.ps1"
	rd "bin\release\net9.0\sc" /s /q
)

echo Publishing regular binaries...

:: Publish release for x86, amd64 and arm64 (win, osx, linux)
for %%a in (win-x86 win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64) do (
	echo ------------------------ Building regular binary for target %%a ------------------------
	:: Yes, this is not the ideal way to do this, but batch is so archaic there's no CONTINUE statement
	set target="%%a"
	if "!target:~1,3!" NEQ "win" (
		call :nonwindowswarning %%a
	)
	if "%%a" == "osx-x64" (
		if !UNATTENDGEN_ADD_OSX_X64_COMPAT! EQU 0 (
			echo Platform %%a not supported by publish flags.
			echo Set the UNATTENDGEN_ADD_OSX_X64_COMPAT option in this script to 1 to enable Intel builds of UnattendGen for macOS.
			echo.
			echo After a .NET version either stops supporting Intel systems or macOS Tahoe, both this flag and the %%a target will be removed
			echo from this script.
		) else (
			dotnet publish -r:%%a --framework net9.0
			rd "bin\release\net9.0\%%a\publish" /s /q
		)
	) else (
		dotnet publish -r:%%a --framework net9.0
		rd "bin\release\net9.0\%%a\publish" /s /q
	)
)

echo Zipping regular binaries...
powershell -ExecutionPolicy Bypass ".\RegularZip.ps1"

echo Completed.
ENDLOCAL
exit /b

:nonwindowswarning
for /f %%A in ('echo prompt $E ^| cmd') do set "ESC=%%A"
echo %ESC%[30;43mTarget %1 is not a Windows platform. UNIX builds will be made, however, they WON'T be executable. The end-user will need%ESC%[0m
echo %ESC%[30;43mto run "chmod +x ./UnattendGen" to mark program as executable.%ESC%[0m