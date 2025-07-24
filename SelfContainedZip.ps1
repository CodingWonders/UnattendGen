if ((Get-ChildItem "bin\Release\net9.0\sc").Count -gt 0)
{
	foreach ($dir in $(Get-ChildItem "bin\Release\net9.0\sc"))
	{
		Write-Host "Zipping $($dir.FullName)..." -BackgroundColor Black -ForegroundColor DarkYellow
		# Determine platform
		if ($dir.Name.Contains("win")) {
			Compress-Archive -Path "$($dir.FullName)\*" -DestinationPath "bin\Release\net9.0\$($dir.Name.Replace("win", $([IO.Path]::GetFileName(((Get-Location).Path)))))--Windows-SelfContained.zip"
		} elseif ($dir.Name.Contains("osx")) {
			Compress-Archive -Path "$($dir.FullName)\*" -DestinationPath "bin\Release\net9.0\$($dir.Name.Replace("osx", $([IO.Path]::GetFileName(((Get-Location).Path)))))--MacOS-SelfContained.zip"
		} elseif ($dir.Name.Contains("linux")) {
			Compress-Archive -Path "$($dir.FullName)\*" -DestinationPath "bin\Release\net9.0\$($dir.Name.Replace("linux", $([IO.Path]::GetFileName(((Get-Location).Path)))))--Linux-SelfContained.zip"
		}
	}
}