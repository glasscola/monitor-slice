$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$folder=Join-Path $root 'dist\MonitorSlice-1.4.1'
New-Item -ItemType Directory -Force (Join-Path $folder 'driver\package\VirtualDisplayDriver') | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'MonitorSlice.exe'),(Join-Path $root 'MonitorSlice.Capture.dll'),(Join-Path $root 'README.md') -Destination $folder -Force

Copy-Item -LiteralPath (Join-Path $root 'driver\LICENSE.txt') -Destination (Join-Path $folder 'driver') -Force
foreach($name in @('MttVDD.inf','MttVDD.dll','mttvdd.cat')){Copy-Item -LiteralPath (Join-Path $root "driver\package\VirtualDisplayDriver\$name") -Destination (Join-Path $folder 'driver\package\VirtualDisplayDriver') -Force}
Copy-Item -LiteralPath (Join-Path $root 'docs\MonitorSlice-User-Guide-ko.pdf') -Destination $folder -Force
$expected=@('MonitorSlice-User-Guide-ko.pdf','MonitorSlice.exe','MonitorSlice.Capture.dll','README.md','driver\LICENSE.txt','driver\package\VirtualDisplayDriver\MttVDD.inf','driver\package\VirtualDisplayDriver\MttVDD.dll','driver\package\VirtualDisplayDriver\mttvdd.cat')
foreach($file in Get-ChildItem -LiteralPath $folder -Recurse -File){$relative=$file.FullName.Substring($folder.Length+1);if($expected -notcontains $relative){throw "Unexpected runtime file in package: $relative"}}
Compress-Archive -LiteralPath $folder -DestinationPath (Join-Path $root 'dist\MonitorSlice-v1.4.1-win-x64.zip') -Force
