param(
    [Parameter(Mandatory=$true)][string]$ObsZip,
    [Parameter(Mandatory=$true)][string]$ObsSource,
    [Parameter(Mandatory=$true)][string]$SourceRecordZip,
    [Parameter(Mandatory=$true)][string]$SourceRecordSource
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$exe = Join-Path $repo 'build/EVA Clipper.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Run scripts/build.ps1 first.' }
$zipPath = (Resolve-Path -LiteralPath $ObsZip).Path
$sourcePath = (Resolve-Path -LiteralPath $ObsSource).Path
if ((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash -ne 'FD367A7F156319426F1A600720C1288AD0011DA168DFF41E0F94DBB18CC1DC0F') { throw 'Unexpected OBS Windows archive hash.' }
if ((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne 'D9E62EA24305FBFE14C69A6422DE1525FE84DA232DF2CC0D7E4601A840BDBBA8') { throw 'Unexpected OBS source archive hash.' }
$pluginPath = (Resolve-Path -LiteralPath $SourceRecordZip).Path
$pluginSourcePath = (Resolve-Path -LiteralPath $SourceRecordSource).Path
if ((Get-FileHash -LiteralPath $pluginPath -Algorithm SHA256).Hash -ne 'C2E461AFC42746BB8F1861F0AAEF63C42F5CC436B55DA1A21B3B07BC6D014074') { throw 'Unexpected Source Record binary archive hash.' }
if ((Get-FileHash -LiteralPath $pluginSourcePath -Algorithm SHA256).Hash -ne 'AD60C3CB88FEC5B726FADE90EEB872B527070822AFFC8D7FDD7CFB831C5A539E') { throw 'Unexpected Source Record source archive hash.' }
$dist = Join-Path $repo 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$archivePath = Join-Path $dist 'EVA-Clipper-3.1.0-Windows-x64.zip'
if (Test-Path -LiteralPath $archivePath) { throw 'Release ZIP already exists; move it aside before rebuilding.' }
$stage = Join-Path $dist ('stage-' + [guid]::NewGuid().ToString('N'))
$package = Join-Path $stage 'EVA Clipper'
$engine = Join-Path $package 'engine'
New-Item -ItemType Directory -Force -Path $engine | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($entry in $zip.Entries) {
        $relative = $entry.FullName.Replace('\','/')
        if ($relative -notmatch '^(bin|data|obs-plugins)/' -or $relative -match '(^|/)\.\.(/|$)' -or $relative.Contains(':')) { throw "Unexpected upstream ZIP entry: $relative" }
        if ($relative.EndsWith('.pdb', [StringComparison]::OrdinalIgnoreCase)) { continue }
        $target = [IO.Path]::GetFullPath((Join-Path $engine $relative))
        if (!$target.StartsWith($engine + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'ZIP path escaped staging directory.' }
        if ($relative.EndsWith('/')) { New-Item -ItemType Directory -Force -Path $target | Out-Null; continue }
        New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $false)
    }
} finally { $zip.Dispose() }
$pluginZip = [System.IO.Compression.ZipFile]::OpenRead($pluginPath)
try {
    foreach ($entry in $pluginZip.Entries) {
        $relative = $entry.FullName.Replace('\','/')
        if ($relative -notmatch '^(obs-plugins/64bit/source-record\.dll|data/obs-plugins/source-record/locale/[a-zA-Z-]+\.ini)$') { continue }
        $target = [IO.Path]::GetFullPath((Join-Path $engine $relative))
        if (!$target.StartsWith($engine + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Plugin path escaped staging directory.' }
        New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $false)
    }
} finally { $pluginZip.Dispose() }
if (!(Test-Path -LiteralPath (Join-Path $engine 'obs-plugins/64bit/source-record.dll'))) { throw 'Source Record plugin was not extracted.' }
Copy-Item -LiteralPath $exe -Destination (Join-Path $package 'EVA Clipper.exe')
Copy-Item -LiteralPath (Join-Path $repo 'build/eva-window-capture.dll') -Destination (Join-Path $engine 'obs-plugins/64bit/eva-window-capture.dll')
foreach ($name in @('README.md','LICENSE','THIRD_PARTY.md')) { Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $package }
Copy-Item -LiteralPath (Join-Path $engine 'data/obs-studio/license/gplv2.txt') -Destination (Join-Path $package 'OBS-LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $repo 'licenses/Source-Record-LICENSE.txt') -Destination $package
Copy-Item -LiteralPath (Join-Path $repo 'licenses/BebasNeue-OFL.txt') -Destination $package
$utf8 = New-Object System.Text.UTF8Encoding($false)
$manifest = Get-ChildItem -LiteralPath $package -Recurse -File | Sort-Object FullName | ForEach-Object {
    $relative = $_.FullName.Substring($package.Length + 1).Replace('\','/')
    ((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()) + '  ' + $relative
}
[IO.File]::WriteAllLines((Join-Path $package 'FILES.sha256'), [string[]]$manifest, $utf8)
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $archivePath, [IO.Compression.CompressionLevel]::Optimal, $false)
$sourceAsset = Join-Path $dist 'OBS-Studio-32.0.4-Sources.tar.gz'
Copy-Item -LiteralPath $sourcePath -Destination $sourceAsset
$pluginSourceAsset = Join-Path $dist 'source-record-0.4.8-Sources.tar.gz'
Copy-Item -LiteralPath $pluginSourcePath -Destination $pluginSourceAsset
$sums = @($archivePath,$sourceAsset,$pluginSourceAsset) | ForEach-Object { ((Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()) + '  ' + [IO.Path]::GetFileName($_) }
[IO.File]::WriteAllLines((Join-Path $dist 'SHA256SUMS.txt'), [string[]]$sums, $utf8)
Write-Output $archivePath
Write-Output $sourceAsset
Write-Output $pluginSourceAsset
Write-Output (Join-Path $dist 'SHA256SUMS.txt')
