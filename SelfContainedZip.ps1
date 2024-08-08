if ((Get-ChildItem "bin\Release\net8.0\sc").Count -gt 0)
{
	foreach ($dir in $(Get-ChildItem "bin\Release\net8.0\sc"))
	{
		Compress-Archive -Path "$($dir.FullName)\*" -DestinationPath "bin\Release\net8.0\sc\$($dir.Name.Replace("win", $([IO.Path]::GetFileName(((Get-Location).Path))))).zip"
	}
}