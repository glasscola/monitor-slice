param([string]$OutputName = "MonitorSlice.exe")
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /platform:x64 /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Core.dll /reference:System.Xml.dll "/out:$root\$OutputName" $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
