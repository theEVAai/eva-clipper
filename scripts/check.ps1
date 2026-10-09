param([Parameter(Mandatory=$true)][string]$Compiler)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$compilerPath = (Resolve-Path -LiteralPath $Compiler).Path
$build = Join-Path $repo 'build'
New-Item -ItemType Directory -Force -Path $build | Out-Null
$testExe = Join-Path $build 'ContractChecks.exe'
$files = @('src/Core.cs','src/CaptureSources.cs','src/CaptureSessions.cs','src/ObsRpc.cs','src/MediaTools.cs','tests/ContractChecks.cs') | ForEach-Object { Join-Path $repo $_ }
& $compilerPath /nologo /target:exe /platform:x64 /optimize+ "/out:$testExe" /reference:System.Web.Extensions.dll $files
if ($LASTEXITCODE -ne 0) { throw 'Check compilation failed' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Contract checks failed' }
$wheelExe=Join-Path $build 'WheelChecks.exe'
& $compilerPath /nologo /target:exe /platform:x64 /optimize+ ('/out:'+$wheelExe) /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $repo 'src/Controls.cs') (Join-Path $repo 'tests/WheelChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Wheel check compilation failed' }
& $wheelExe
if ($LASTEXITCODE -ne 0) { throw 'Wheel checks failed' }
