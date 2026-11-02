<#
.SYNOPSIS
    Builds the release zip of OpenIris and creates a draft GitHub release with it.

.DESCRIPTION
    Run it on a computer with the Spinnaker and FlyCapture SDKs installed, from a clean checkout of
    the commit with the version tag (GitHub's build machines don't have the SDKs, so their builds
    don't have the camera plugins). It:

    1. checks that there are no changes, and that the commit has a version tag;
    2. builds Release from an empty bin\x64\Release;
    3. checks that the program reports that version and that every plugin is there, without the
       camera SDKs' own DLLs (they come from the installed SDKs);
    4. zips bin\x64\Release as bin\OpenIris_Win_Release_<version>.zip, in a folder OpenIris_<version>,
       without the full quality sample videos (JorgeLeft.avi, JorgeRight.avi);
    5. creates a draft release on GitHub with the zip, to review and publish there.

    Pushing the tag also publishes the NuGet packages (.github/workflows/publish.yml), independently
    of this script. See docs/plugin-development/contributing.md.

.PARAMETER NoRelease
    Only build and zip, without creating the GitHub release.

.EXAMPLE
    git tag 0.2.0; git push origin 0.2.0; .\tools\Make-Release.ps1
#>
param(
    [switch]$NoRelease
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

function Fail([string]$message) {
    Write-Host "ERROR: $message" -ForegroundColor Red
    exit 1
}

# 1. A clean checkout of a tagged commit.
if (git status --porcelain) { Fail 'There are uncommitted changes. Commit or remove them first.' }
$version = git describe --exact-match --tags HEAD 2>$null
if (-not $version) { Fail 'The current commit has no version tag. Tag it first (for example: git tag 0.2.0).' }
if ($version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') { Fail "The tag '$version' isn't a version number." }
Write-Host "Version $version"

# The camera SDKs, or the build would silently skip their plugins.
$spinnaker = Join-Path $env:ProgramW6432 'FLIR Systems\Spinnaker\bin64\vs2015\SpinnakerNET_v140.dll'
$flyCapture = Join-Path $env:ProgramW6432 'Point Grey Research\FlyCapture2\bin64\vs2015\FlyCapture2Managed_v140.dll'
foreach ($sdk in $spinnaker, $flyCapture) {
    if (-not (Test-Path $sdk)) { Fail "Camera SDK not found: $sdk" }
}

# 2. Build Release from an empty folder: files left from older builds would end up in the zip.
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { Fail 'MSBuild not found. Install Visual Studio with the .NET desktop development workload.' }

$bin = Join-Path $repo 'bin\x64\Release'
if (Test-Path $bin) { Remove-Item $bin -Recurse -Force }
& $msbuild source\OpenIris.sln -restore -t:Rebuild -p:Configuration=Release -p:Platform=x64 -m -nologo -v:minimal
if ($LASTEXITCODE -ne 0) { Fail 'The build failed.' }

# 3. Check the build.
$productVersion = (Get-Item (Join-Path $bin 'OpenIris.exe')).VersionInfo.ProductVersion
if ($productVersion -ne $version -and -not $productVersion.StartsWith("$version+")) {
    Fail "OpenIris.exe reports version $productVersion, not $version."
}
$expected = 'OpenIris.exe', 'OpenIrisProcess.exe', 'lib\OpenIrisLib.dll', 'lib\OpenIrisWinForms.dll',
    'Plugins\BasicPlugins.dll', 'Plugins\SpinnakerInterface.dll', 'Plugins\PointGreyInterface.dll',
    'Plugins\RemoteEyeTracker.dll', 'Plugins\Micromedical.dll', 'Plugins\MccDaq.dll', 'Plugins\FTD2XX_NET.dll'
foreach ($file in $expected) {
    if (-not (Test-Path (Join-Path $bin $file))) { Fail "Missing from the build: $file" }
}
$sdkCopies = Get-ChildItem $bin -Recurse -Include 'SpinnakerNET*.dll', 'FlyCapture2Managed*.dll'
if ($sdkCopies) { Fail "The build has the camera SDKs' own DLLs, which must not ship: $($sdkCopies.FullName -join ', ')" }

# 4. The zip, with everything in a folder named after the version.
$name = "OpenIris_$version"
$zip = Join-Path $repo "bin\OpenIris_Win_Release_$version.zip"
$staging = Join-Path $repo "bin\release-staging"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
Copy-Item $bin (Join-Path $staging $name) -Recurse
Get-ChildItem (Join-Path $staging $name) -Recurse -Include 'JorgeLeft.avi', 'JorgeRight.avi' | Remove-Item
if (Test-Path $zip) { Remove-Item $zip }
# Each file added with a path with forward slashes: in Windows PowerShell 5.1, Compress-Archive and
# ZipFile.CreateFromDirectory write backslashes, which other zip programs show as file names instead
# of folders.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem $staging -Recurse -File) {
        $entry = $file.FullName.Substring($staging.Length + 1).Replace('\', '/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal)
    }
}
finally {
    $archive.Dispose()
}
Remove-Item $staging -Recurse -Force
Write-Host ("Zip: {0} ({1:N1} MB)" -f $zip, ((Get-Item $zip).Length / 1MB))

# 5. The draft release on GitHub.
if ($NoRelease) { exit 0 }
$gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
if (-not $gh) { $gh = Join-Path $env:ProgramFiles 'GitHub CLI\gh.exe' }
if (-not (Test-Path $gh)) { Fail 'GitHub CLI (gh) not found: create the release on GitHub by hand, with the zip.' }
$releaseArgs = @('release', 'create', $version, $zip, '--draft', '--title', "OpenIris $version", '--generate-notes', '--verify-tag')
if ($version.Contains('-')) { $releaseArgs += '--prerelease' }
& $gh @releaseArgs
if ($LASTEXITCODE -ne 0) { Fail 'Creating the GitHub release failed.' }
Write-Host 'Draft release created: review the notes on GitHub and publish it.'
