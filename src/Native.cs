using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace MonitorSlice {
public static class Native {
 public delegate bool EnumProc(IntPtr h,IntPtr p);
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; public Rectangle Box {get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}} }
 [StructLayout(LayoutKind.Sequential)] public struct Point {public int X,Y;}
 [StructLayout(LayoutKind.Sequential)] public struct Placement {public int length,flags,showCmd;public Point min,max;public Rect normal;}
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb,IntPtr p);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int z,uint flags);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] public static extern bool GetWindowPlacement(IntPtr h,ref Placement p);
 [DllImport("user32.dll")] public static extern bool SetWindowPlacement(IntPtr h,ref Placement p);
 [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr h,int show);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h,int index);
 [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h,uint cmd);
 [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
 [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h,int id);
 [DllImport("shcore.dll")] public static extern int SetProcessDpiAwareness(int value);
 public static Placement ReadPlacement(IntPtr h){var p=new Placement();p.length=Marshal.SizeOf(typeof(Placement));if(!GetWindowPlacement(h,ref p))throw new Exception("창 위치를 읽을 수 없습니다.");return p;}
 public static List<WindowItem> Windows(){var list=new List<WindowItem>();int own=Process.GetCurrentProcess().Id;
  EnumWindows(delegate(IntPtr h,IntPtr p){try{uint id;GetWindowThreadProcessId(h,out id);if(id==own||!IsWindowVisible(h)||GetWindow(h,4)!=IntPtr.Zero)return true;
   var s=new StringBuilder(512);GetWindowText(h,s,s.Capacity);if(s.Length==0)return true;var process=Process.GetProcessById((int)id);
   list.Add(new WindowItem{Handle=h,Pid=(int)id,StartTicks=process.StartTime.ToUniversalTime().Ticks,Title=s.ToString(),ProcessName=process.ProcessName});}catch{}return true;},IntPtr.Zero);return list; }
}
public class WindowItem {
 public IntPtr Handle;public int Pid;public long StartTicks;public string Title,ProcessName;
 public override string ToString(){return Handle==IntPtr.Zero?"연결할 창을 선택하세요":ProcessName+" · "+Title;}
}
}
