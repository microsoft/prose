param([string]$username, [string]$password)

function findExecutable($name, $commonLocations) {
    $name = if ([Environment]::OSVersion.Platform -eq [System.PlatformID]::Unix) { $name } else { "$name.exe" }
    # Look in PATH
    Write-Host "Considering $name..."
    if (Get-Command $name -ErrorAction SilentlyContinue) { return $name; }
    # Look in Program Files
    foreach ($loc in $commonLocations) {
        $pf = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFiles)        
        $path = [System.IO.Path]::Combine($pf, $loc, $name)
        Write-Host "Considering $path..."
        if (Test-Path $path) { return $path }

        $pf86 = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFilesX86)
        $path = [System.IO.Path]::Combine($pf86, $loc, $name)
        Write-Host "Considering $path..."
        if (Test-Path $path) { return $path }
    }
    # Look in registry
    if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\$name") {
        return (Get-ItemProperty -LiteralPath "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\$name" -ErrorAction SilentlyContinue).'(default)'
    }
    return $null
}

$remote = "azuredeploy"
cd "$env:BUILD_SOURCESDIRECTORY"
$git = findExecutable "git" @("Git/bin")
if (-not $git) {
    throw "Git is not installed or not in the PATH."    
}

& $git config push.default simple
$remoteExists = $(& $git remote | Select-String -Pattern "^${remote}$" -Quiet)
if (-not $remoteExists) {
    echo "Adding remote $remote"
    & $git remote add $remote "https://${username}:${password}@flashm.scm.azurewebsites.net:443/flashm.git"
}
$push = & $git push --porcelain -q -u $remote "$env:BUILD_SOURCEBRANCHNAME" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error $push
}
