param([Parameter(Mandatory=$true)][string]$ObsSourceRoot)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$sdk=(Resolve-Path -LiteralPath $ObsSourceRoot).Path
if (!(Test-Path -LiteralPath (Join-Path $sdk 'libobs/obs-module.h'))) { throw 'Pass the extracted OBS 32.0.4 source root.' }
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$vs=& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (!$vs) { throw 'MSVC x64 build tools are required.' }
$vc=Get-ChildItem -LiteralPath (Join-Path $vs 'VC/Tools/MSVC') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$kit=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10'
$version=Get-ChildItem -LiteralPath (Join-Path $kit 'Include') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$bin=Join-Path $vc.FullName 'bin/Hostx64/x64'
$build=Join-Path $repo 'build/native'
New-Item -ItemType Directory -Path $build -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $build 'obsconfig.h'),"#define OBS_INSTALL_PREFIX `"`"`n#define OBS_DATA_PATH `"data`"`n")
& (Join-Path $bin 'lib.exe') /nologo /machine:x64 ('/def:'+(Join-Path $repo 'native/obs.def')) ('/out:'+(Join-Path $build 'obs.lib'))
if ($LASTEXITCODE -ne 0) { throw 'OBS import library generation failed.' }
$includes=@(($vc.FullName+'/include'),($version.FullName+'/ucrt'),($version.FullName+'/shared'),($version.FullName+'/um'),(Join-Path $sdk 'libobs'),$build) | ForEach-Object { '/I'+$_ }
$libs=@(($vc.FullName+'/lib/x64'),(Join-Path $kit ('Lib/'+$version.Name+'/ucrt/x64')),(Join-Path $kit ('Lib/'+$version.Name+'/um/x64')),$build) | ForEach-Object { '/LIBPATH:'+$_ }
& (Join-Path $bin 'cl.exe') /nologo /std:c17 /O2 /MT /LD /W3 $includes ('/Fo'+(Join-Path $build 'eva-window-capture.obj')) (Join-Path $repo 'native/eva-window-capture.c') /link /Brepro $libs obs.lib user32.lib gdi32.lib ('/IMPLIB:'+(Join-Path $build 'eva-window-capture.lib')) ('/OUT:'+(Join-Path $repo 'build/eva-window-capture.dll'))
if ($LASTEXITCODE -ne 0) { throw 'EVA capture plugin compilation failed.' }
Write-Output (Join-Path $repo 'build/eva-window-capture.dll')
