if ((Get-ChildItem "bin\Release\net9.0\sc").Count -gt 0)
{
	foreach ($dir in $(Get-ChildItem "bin\Release\net9.0\sc"))
	{
		Compress-Archive -Path "$($dir.FullName)\*" -DestinationPath "bin\Release\net9.0\$($dir.Name.Replace("win", $([IO.Path]::GetFileName(((Get-Location).Path)))))--SelfContained.zip"
	}
}