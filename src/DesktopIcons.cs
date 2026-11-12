using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
namespace MonitorSlice {
static class DesktopIcons {
 static readonly string FilePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","desktop-icons.bin");
 [DllImport("MonitorSlice.Capture.dll",CharSet=CharSet.Unicode,CallingConvention=CallingConvention.Cdecl)]static extern int SaveDesktopIcons(string path);
 [DllImport("MonitorSlice.Capture.dll",CharSet=CharSet.Unicode,CallingConvention=CallingConvention.Cdecl)]static extern int RestoreDesktopIcons(string path);
 public static void Save(){if(File.Exists(FilePath))throw new Exception("이전 바탕화면 아이콘 복구를 먼저 완료해야 합니다.");Directory.CreateDirectory(Path.GetDirectoryName(FilePath));Marshal.ThrowExceptionForHR(SaveDesktopIcons(FilePath));}
 public static void Restore(bool finish){if(!File.Exists(FilePath))return;int result=0;for(int i=0;i<3;i++){Thread.Sleep(350);result=RestoreDesktopIcons(FilePath);if(result<0&&i==2)Marshal.ThrowExceptionForHR(result);}if(finish){File.Copy(FilePath,FilePath+".last",true);File.Delete(FilePath);}}
 }
}
