using namespace System.Collections.Generic

class ZipRelease {
    [string] $name
    [string] $platform
    [string] $architecture
    [bool] $selfContained
    [string] $hash

    ZipRelease($name, $plat, $arch, $sc, $hash) {
        $this.name = $name
        $this.platform = $plat
        $this.architecture = $arch
        $this.selfContained = $sc
        $this.hash = $hash
    }
}

$releases = [List[ZipRelease]]::new()

$zipFiles = (Get-ChildItem "UnattendGen*.zip" -File)
if ($zipFiles.Count -gt 0) {
    foreach ($zipFile in $zipFiles) {
        $fileName = [IO.Path]::GetFileNameWithoutExtension("$($zipFile.Name)")
        $fileNameParts = $fileName -split "-"
        # If it contains selfcontained then we're dealing with self-contained stuff
        if ($fileNameParts.Contains("SelfContained")) {
            $releases.Add([ZipRelease]::new("UnattendGen", $fileNameParts[3], $fileNameParts[1], $true, (Get-FileHash "$zipFile").Hash))
        } else {
            $releases.Add([ZipRelease]::new("UnattendGen", $fileNameParts[2], $fileNameParts[1], $false, (Get-FileHash "$zipFile").Hash))
        }
        
    }
}

$releases = $releases | Sort-Object -Property Platform, Architecture -Descending

if ($releases.Count -gt 0) {
    $selfContainedReleases = $releases | Where-Object { $_.SelfContained -eq $true }
    $regularReleases = $releases | Where-Object { $_.SelfContained -eq $false }
    
    Write-Host "## File hashes`n"
    Write-Host "| File | Hash (SHA256) |"
    Write-Host "|:--:|:--:|"
    foreach ($regularRelease in $regularReleases) {
        Write-Host "| $($regularRelease.name) ($($regularRelease.platform)) $($regularRelease.architecture) | **$($regularRelease.hash)** |"
    }
    foreach ($selfContainedRelease in $selfContainedReleases) {
        Write-Host "| $($selfContainedRelease.name) ($($selfContainedRelease.platform)) $($selfContainedRelease.architecture) (Self-Contained) | **$($selfContainedRelease.hash)** |"
    }
    Write-Host "`nAmount of releases: $($releases.Count)"
}
