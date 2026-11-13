using System;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace MonitorSlice {
// Only rescue windows whose centre is on the presentation output. Other screens
// and user moves between visible screens remain under Windows control.
public sealed class WindowSafety:IDisposable {
 readonly Rectangle hidden;readonly List<Viewport> views;readonly Timer timer;readonly bool tests;
 Rectangle destination;bool scanning;public int Moved,Failed;
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern bool GetCursorPos(out Native.Point point);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr window,StringBuilder text,int length);
 public WindowSafety(Rectangle output,List<Viewport> regions,bool testOnly=false){hidden=output;views=regions;tests=testOnly;destination=views.OrderBy(v=>v.Source.Top).ThenBy(v=>v.Source.Left).First().Source;timer=new Timer{Interval=250};timer.Tick+=delegate{Rescue();};timer.Start();}
 Rectangle RegionFor(IntPtr h){Native.Rect rect;if(h!=IntPtr.Zero&&Native.GetWindowRect(h,out rect)){var box=rect.Box;var centre=new Point(box.Left+box.Width/2,box.Top+box.Height/2);foreach(var v in views)if(v.Source.Contains(centre))return v.Source;}return Rectangle.Empty;}
 public void Rescue(){if(scanning)return;scanning=true;try{var active=RegionFor(GetForegroundWindow());Native.Point cursor;if(!active.IsEmpty)destination=active;else if(GetCursorPos(out cursor)){foreach(var v in views)if(v.Source.Contains(cursor.X,cursor.Y)){destination=v.Source;break;}}int own=Process.GetCurrentProcess().Id;int handled=0;Native.EnumWindows(delegate(IntPtr h,IntPtr unused){try{if(handled>=64||!Native.IsWindowVisible(h))return true;uint pid;Native.GetWindowThreadProcessId(h,out pid);var title=new StringBuilder(512);Native.GetWindowText(h,title,title.Capacity);if(tests){if(pid!=own||!title.ToString().StartsWith("CarryTest"))return true;}else if(pid==own)return true;var cls=new StringBuilder(128);GetClassName(h,cls,cls.Capacity);if(new[]{"Progman","WorkerW","Shell_TrayWnd","Shell_SecondaryTrayWnd","tooltips_class32","IME","MSCTFIME UI"}.Contains(cls.ToString()))return true;if(title.Length==0&&Native.GetWindow(h,4)==IntPtr.Zero)return true;var placement=Native.ReadPlacement(h);if(placement.showCmd==2)return true;Native.Rect rect;if(!Native.GetWindowRect(h,out rect))return true;var box=rect.Box;if(box.Width<=0||box.Height<=0||!hidden.Contains(box.Left+box.Width/2,box.Top+box.Height/2))return true;handled++;var parent=RegionFor(Native.GetWindow(h,4));var to=WindowCarry.WorkArea(parent.IsEmpty?destination:parent);var fit=WindowCarry.Fit(box,hidden,to);if(placement.showCmd==3){placement.showCmd=1;Native.SetWindowPlacement(h,ref placement);}if(Native.SetWindowPos(h,IntPtr.Zero,fit.X,fit.Y,fit.Width,fit.Height,0x214))Moved++;else Failed++;}catch{Failed++;}return true;},IntPtr.Zero);}finally{scanning=false;}}
 public void Dispose(){timer.Stop();timer.Dispose();}
}
}
