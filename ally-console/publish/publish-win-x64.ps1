# Self-contained Release folder for Windows x64.
# Trimming is off. The Ally does not need a .NET SDK to run the result.
# Does not change startup, services, power, or firmware.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File ally-console\publish\publish-win-x64.ps1

$ErrorActionPreference = "Stop"

$publishDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $publishDir
$csproj = Join-Path $projectRoot "src\AllyConsole.App\AllyConsole.App.csproj"
$out = Join-Path $projectRoot "artifacts\win-x64"

if (-not (Test-Path -LiteralPath $csproj)) {
    Write-Error "Project not found: $csproj"
    exit 1
}

$commit = "unavailable"
$git = Get-Command git -ErrorAction SilentlyContinue
if ($git) {
    $sha = & git -C $projectRoot rev-parse HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($sha)) {
        $commit = $sha.Trim()
    }
}

$version = "1.0.0"
$rawVersion = & dotnet msbuild $csproj -nologo -getProperty:Version
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
if (-not [string]::IsNullOrWhiteSpace($rawVersion)) {
    $version = $rawVersion.Trim()
}

$informational = $version
if ($commit -ne "unavailable") {
    $informational = "$version+$commit"
}

& dotnet publish $csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishTrimmed=false `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=false `
    -p:IncludeSourceRevisionInInformationalVersion=false `
    -p:InformationalVersion=$informational `
    -o $out
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$exe = Join-Path $out "AllyConsole.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    Write-Error "Publish finished without AllyConsole.exe in $out"
    exit 1
}

Copy-Item -LiteralPath (Join-Path $publishDir "folder-README.txt") -Destination (Join-Path $out "README.txt") -Force

$publishedUtc = [DateTime]::UtcNow.ToString("o")
$buildInfo = @"
product=Ally Console
version=$version
commit=$commit
informationalVersion=$informational
configuration=Release
runtime=win-x64
selfContained=true
publishTrimmed=false
publishSingleFile=false
publishedUtc=$publishedUtc
"@
Set-Content -LiteralPath (Join-Path $out "build-info.txt") -Value $buildInfo -Encoding utf8

Write-Output "Published $out"
Write-Output "publishTrimmed=false"
Write-Output "version=$version"
Write-Output "commit=$commit"
