# UnattendGen - DISMTools Self-Contained Installer

param (
	[Parameter(Mandatory = $true, Position = 0)] [string] $tag
)

[Net.ServicePointManager]::SecurityProtocol = "Tls12"

$useAlternateName = $false
$newNames = @("UnattendGen-x64--Windows-SelfContained.zip", "UnattendGen-x86--Windows-SelfContained.zip")
$alternateNames = @("UnattendGen-x64--SelfContained.zip", "UnattendGen-x86--SelfContained.zip")

$successfulDownloads = 0

Write-Host "Downloading self-contained UnattendGen..."
$ProgressPreference = 'SilentlyContinue'
try {
	foreach ($name in $newNames) {
		if ($name.Contains("x64")) {
			Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/CodingWonders/UnattendGen/releases/download/$tag/$name" -OutFile ".\unattendgen-sc-amd64.zip" -ErrorAction SilentlyContinue
		} else {
			Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/CodingWonders/UnattendGen/releases/download/$tag/$name" -OutFile ".\unattendgen-sc-x86.zip" -ErrorAction SilentlyContinue
		}
		if ($?) { $successfulDownloads++ }
	}
	if ($successfulDownloads -lt 2) { throw }
} catch {
	$successfulDownloads = 0
	foreach ($name in $alternateNames) {
		if ($name.Contains("x64")) {
			Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/CodingWonders/UnattendGen/releases/download/$tag/$name" -OutFile ".\unattendgen-sc-amd64.zip" -ErrorAction SilentlyContinue
		} else {
			Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/CodingWonders/UnattendGen/releases/download/$tag/$name" -OutFile ".\unattendgen-sc-x86.zip" -ErrorAction SilentlyContinue
		}
		if ($?) { $successfulDownloads++ }
	}
	if ($successfulDownloads -lt 2) { exit 1 }
}
$ProgressPreference = 'Continue'

Write-Host "Expanding archives..."
Expand-Archive -Path ".\unattendgen-sc-amd64.zip" -Destination ".\Tools\UnattendGen\SelfContained\amd64" -Force
Expand-Archive -Path ".\unattendgen-sc-x86.zip" -Destination ".\Tools\UnattendGen\SelfContained\x86" -Force

if ($?)
{
	New-Item -Path "$((Get-Location).Path)\Tools\UnattendGen\SelfContained\amd64\DT" -ItemType File -Force | Out-Null
	Set-ItemProperty -Path "$((Get-Location).Path)\Tools\UnattendGen\SelfContained\amd64\DT" -Name Attributes -Value Hidden
	New-Item -Path "$((Get-Location).Path)\Tools\UnattendGen\SelfContained\x86\DT" -ItemType File -Force | Out-Null
	Set-ItemProperty -Path "$((Get-Location).Path)\Tools\UnattendGen\SelfContained\x86\DT" -Name Attributes -Value Hidden
}

Write-Host "Deleting temporary files..."
Remove-Item -Path ".\unattendgen-sc-amd64.zip" -Force
Remove-Item -Path ".\unattendgen-sc-x86.zip" -Force
