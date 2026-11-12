using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
namespace MonitorSlice {
static class DesktopIcons {
 static readonly string FilePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","desktop-icons.bin");
 [DllImport("MonitorSlice.Capture.dll",CharSet=CharSet.Unicode,CallingConvention=CallingConvention.Cdecl)]static extern int SaveDesktopIcons(string path);
 [DllImport("MonitorSlice.Capture.dll",CharSet=CharSet.Unicode,CallingConvention=CallingConvention.Cdecl)]static extern int RestoreDesktopIcons(string path);
 [DllImport("MonitorSlice.Capture.dll",CharSet=CharSet.Unicode,CallingConvention=CallingConvention.Cdecl)]static extern int MapDesktopIcons(string path,Native.Rect from,Native.Rect to);
 public static void MoveTo(System.Drawing.Rectangle from,System.Drawing.Rectangle to){int result=0;for(int i=0;i<3;i++){Thread.Sleep(350);result=MapDesktopIcons(FilePath,new Native.Rect{Left=from.Left,Top=from.Top,Right=from.Right,Bottom=from.Bottom},new Native.Rect{Left=to.Left,Top=to.Top,Right=to.Right,Bottom=to.Bottom});if(result>=0)return;}if(result==unchecked((int)0x8007007A))throw new Exception("기존 아이콘을 첫 칸에 모두 표시할 공간이 부족합니다. 더 넓은 첫 칸 배치를 선택하세요.");Marshal.ThrowExceptionForHR(result);}
 public static void Save(){if(File.Exists(FilePath))throw new Exception("이전 바탕화면 아이콘 복구를 먼저 완료해야 합니다.");Directory.CreateDirectory(Path.GetDirectoryName(FilePath));Marshal.ThrowExceptionForHR(SaveDesktopIcons(FilePath));}
 public static void Restore(bool finish){if(!File.Exists(FilePath))return;int result=0;for(int i=0;i<3;i++){Thread.Sleep(350);result=RestoreDesktopIcons(FilePath);if(result<0&&i==2)Marshal.ThrowExceptionForHR(result);}if(finish){File.Copy(FilePath,FilePath+".last",true);File.Delete(FilePath);}}
 }
}
