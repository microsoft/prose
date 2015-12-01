[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $WorkingDir = ${Get-Location}
)

# Find TypeScript compiler
$tsc = "tsc.exe"
if (-not (Get-Command $tsc -ErrorAction SilentlyContinue)) {
    $typescriptSDKs = [IO.Path]::Combine($env:ProgramFiles, "Microsoft SDKs", "TypeScript")
    if (-not (Test-Path $typescriptSDKs -ErrorAction SilentlyContinue)) {
        $typescriptSDKs = [IO.Path]::Combine(${env:ProgramFiles(x86)}, "Microsoft SDKs", "TypeScript")
        if (-not (Test-Path $typescriptSDKs -ErrorAction SilentlyContinue)) {
            throw "TypeScript is not installed."
        }
    }

    $tsc = dir -Directory -Path $typescriptSDKs | 
        sort Name -Descending | 
        select -Property FullName,@{Name="TSC"; Expression={[IO.Path]::Combine($_.FullName, "tsc.exe")}} | 
        where { Test-Path $_.TSC } |
        select -ExpandProperty TSC -First 1
}
if (-not $tsc) {
    throw "TypeScript is not installed."
}

$mainTS = [IO.Path]::Combine($WorkingDir, "static", "js", "main.ts")
&"$tsc" --module amd --sourcemap  --target ES5 "$mainTS"
