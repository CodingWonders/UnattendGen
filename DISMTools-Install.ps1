# UnattendGen - DISMTools Self-Contained Installer

param (
	[Parameter(Mandatory = $true, Position = 0)] [string] $tag
)

[Net.ServicePointManager]::SecurityProtocol = "Tls12"

Write-Host "Downloading self-contained UnattendGen..."
Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/CodingWonders/UnattendGen/releases/download/$tag/UnattendGen-x64--SelfContained.zip" -OutFile ".\unattendgen-sc-amd64.zip"
Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/CodingWonders/UnattendGen/releases/download/$tag/UnattendGen-x86--SelfContained.zip" -OutFile ".\unattendgen-sc-x86.zip"

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
