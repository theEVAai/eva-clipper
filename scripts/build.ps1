param([Parameter(Mandatory=$true)][string]$Compiler)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$compilerPath = (Resolve-Path -LiteralPath $Compiler).Path
$build = Join-Path $repo 'build'
New-Item -ItemType Directory -Force -Path $build | Out-Null
$outputPath = Join-Path $build 'EVA Clipper.exe'
$sources = @('Core.cs','Overlay.cs','AssemblyInfo.cs') | ForEach-Object { Join-Path $repo "src/$_" }
& $compilerPath /nologo /target:winexe /platform:x64 /optimize+ /deterministic+ /debug- "/pathmap:$repo=/src" "/out:$outputPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Write-Output $outputPath
