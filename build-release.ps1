param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$publishDirectory = Join-Path $projectRoot "publish\$Runtime"
$distDirectory = Join-Path $projectRoot "dist"
$archivePath = Join-Path $distDirectory "QA-Automation-Studio-$Runtime.zip"

Remove-Item -LiteralPath $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $distDirectory -Force | Out-Null

dotnet publish (Join-Path $projectRoot "MyWinFormsApp.csproj") `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item (Join-Path $projectRoot "config.sample.json") $publishDirectory
Remove-Item -LiteralPath (Join-Path $publishDirectory "QA-Automation-Studio.pdb") -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $archivePath -Force
Write-Host "Release package: $archivePath"
