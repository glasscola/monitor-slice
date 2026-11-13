$ErrorActionPreference='Stop'
$sliceRoot=$PSScriptRoot
$sliceBundle=Join-Path $sliceRoot 'dist\bundle'
New-Item -ItemType Directory -Force -Path $sliceBundle | Out-Null
$sliceLauncher=@'
using System;using System.IO;using System.Linq;using System.Reflection;using System.Diagnostics;using System.Security.Cryptography;using System.Windows.Forms;
static class Launcher {
 [STAThread]static void Main(){try{string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MonitorSlice","1.4.1");string[] files={"MonitorSlice.exe","MonitorSlice.Capture.dll","README.md","driver/LICENSE.txt","driver/package/VirtualDisplayDriver/MttVDD.inf","driver/package/VirtualDisplayDriver/MttVDD.dll","driver/package/VirtualDisplayDriver/mttvdd.cat"};for(int i=0;i<files.Length;i++){string path=Path.Combine(root,files[i]);byte[] bytes;using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload"+i))using(var memory=new MemoryStream()){stream.CopyTo(memory);bytes=memory.ToArray();}if(File.Exists(path)){using(var hash=SHA256.Create())if(hash.ComputeHash(File.ReadAllBytes(path)).SequenceEqual(hash.ComputeHash(bytes)))continue;}Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path+".new",bytes);if(File.Exists(path))File.Replace(path+".new",path,null);else File.Move(path+".new",path);}Process.Start(new ProcessStartInfo(Path.Combine(root,"MonitorSlice.exe")){WorkingDirectory=root,UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message,"Monitor Slice 실행 오류");}}
}
'@
$sliceLauncherFile=Join-Path $sliceBundle 'Launcher.cs'
Set-Content -LiteralPath $sliceLauncherFile -Value $sliceLauncher -Encoding UTF8
$sliceResources=@('MonitorSlice.exe','MonitorSlice.Capture.dll','README.md','driver/LICENSE.txt','driver/package/VirtualDisplayDriver/MttVDD.inf','driver/package/VirtualDisplayDriver/MttVDD.dll','driver/package/VirtualDisplayDriver/mttvdd.cat')
$sliceArgs=@('/nologo','/platform:x64','/target:winexe','/reference:System.Windows.Forms.dll','/reference:System.Core.dll',('/out:'+(Join-Path $sliceRoot 'dist\MonitorSlice-1.4.1.exe')))
for($sliceIndex=0;$sliceIndex -lt $sliceResources.Count;$sliceIndex++){$sliceArgs+=('/resource:'+(Join-Path $sliceRoot $sliceResources[$sliceIndex])+',payload'+$sliceIndex)}
$sliceArgs+=$sliceLauncherFile
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') @sliceArgs
if($LASTEXITCODE -ne 0){throw 'Single executable build failed'}
