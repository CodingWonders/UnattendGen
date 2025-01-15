@echo off

where dotnet 2>nul

if %ERRORLEVEL% gtr 0 (
	echo .NET SDK not installed. Exiting...
	exit /b
)

if exist "bin\release\net9.0" (
	rd "bin\release\net9.0" /s /q
)

cls

echo Publishing self-contained binaries...

:: Publish self-contained versions
dotnet publish -r:win-x86 --sc -o "bin\release\net9.0\sc\win-x86" --framework net9.0
dotnet publish -r:win-x64 --sc -o "bin\release\net9.0\sc\win-x64" --framework net9.0
dotnet publish -r:win-arm64 --sc -o "bin\release\net9.0\sc\win-arm64" --framework net9.0

if exist "bin\release\net9.0\sc" (
	echo Zipping self-contained binaries...
	powershell -ExecutionPolicy Bypass ".\SelfContainedZip.ps1"
	rd "bin\release\net9.0\sc" /s /q
)

echo Publishing regular binaries...

:: Publish release for x86, amd64 and arm64
dotnet publish -r:win-x86 --framework net9.0
rd "bin\release\net9.0\win-x86\publish" /s /q
dotnet publish -r:win-x64 --framework net9.0
rd "bin\release\net9.0\win-x64\publish" /s /q
dotnet publish -r:win-arm64 --framework net9.0
rd "bin\release\net9.0\win-arm64\publish" /s /q

echo Zipping regular binaries...
powershell -ExecutionPolicy Bypass ".\RegularZip.ps1"