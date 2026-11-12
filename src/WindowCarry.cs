using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace MonitorSlice {
static class WindowCarry {
 public class Saved {public long Handle;public int Pid;public long Start;public Native.Placement Placement;public int X,Y,W,H;public bool Selected;}
 static readonly string Journal=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","window-recovery.json");
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr window,StringBuilder text,int length);
 [StructLayout(LayoutKind.Sequential)]struct MonitorInfo{public int Size;public Native.Rect Monitor,Work;public uint Flags;}
 [DllImport("user32.dll")]static extern IntPtr MonitorFromPoint(Native.Point point,uint flags);
 [DllImport("user32.dll")]static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
 public static Rectangle WorkArea(Rectangle bounds){var info=new MonitorInfo{Size=Marshal.SizeOf(typeof(MonitorInfo))};var monitor=MonitorFromPoint(new Native.Point{X=bounds.X+bounds.Width/2,Y=bounds.Y+bounds.Height/2},0);if(monitor==IntPtr.Zero||!GetMonitorInfo(monitor,ref info))throw new Exception("분할 화면 작업 영역을 확인할 수 없습니다.");return Rectangle.Intersect(bounds,info.Work.Box);}
 public static bool IncludeTestWindows;
 public static int Failed;
 public static void Save(Rectangle target){if(File.Exists(Journal))throw new Exception("이전 창 위치 복구를 완료한 뒤 다시 시작하세요.");var saved=new List<Saved>();int own=Process.GetCurrentProcess().Id;Native.EnumWindows(delegate(IntPtr h,IntPtr unused){try{uint pid;Native.GetWindowThreadProcessId(h,out pid);if(!Native.IsWindowVisible(h)||Native.GetWindow(h,4)!=IntPtr.Zero)return true;var title=new StringBuilder(512);Native.GetWindowText(h,title,title.Capacity);if(title.Length==0||(pid==own&&!(IncludeTestWindows&&title.ToString().StartsWith("CarryTest"))))return true;var cls=new StringBuilder(128);GetClassName(h,cls,cls.Capacity);if(new[]{"Progman","WorkerW","Shell_TrayWnd","Shell_SecondaryTrayWnd"}.Contains(cls.ToString()))return true;Native.Rect rect;if(!Native.GetWindowRect(h,out rect))return true;var box=rect.Box;var placement=Native.ReadPlacement(h);if(placement.showCmd==2)return true;var process=Process.GetProcessById((int)pid);saved.Add(new Saved{Handle=h.ToInt64(),Pid=(int)pid,Start=process.StartTime.ToUniversalTime().Ticks,Placement=placement,X=box.X,Y=box.Y,W=box.Width,H=box.Height,Selected=target.Contains(new Point(box.X+box.Width/2,box.Y+box.Height/2))});}catch{}return true;},IntPtr.Zero);Directory.CreateDirectory(Path.GetDirectoryName(Journal));File.WriteAllText(Journal,new JavaScriptSerializer().Serialize(saved));}
 static bool Valid(Saved s){try{var h=new IntPtr(s.Handle);uint pid;Native.GetWindowThreadProcessId(h,out pid);return Native.IsWindow(h)&&pid==s.Pid&&Process.GetProcessById(s.Pid).StartTime.ToUniversalTime().Ticks==s.Start;}catch{return false;}}
 public static Rectangle Fit(Rectangle box,Rectangle from,Rectangle to){int w=Math.Min(box.Width,to.Width),h=Math.Min(box.Height,to.Height);double fx=(box.X-from.X)/(double)Math.Max(1,from.Width),fy=(box.Y-from.Y)/(double)Math.Max(1,from.Height);return new Rectangle(Math.Max(to.Left,Math.Min(to.Right-w,to.Left+(int)(fx*to.Width))),Math.Max(to.Top,Math.Min(to.Bottom-h,to.Top+(int)(fy*to.Height))),w,h);}
 public static void Move(Rectangle from,Rectangle to){Failed=0;foreach(var s in Read()){if(!Valid(s))continue;var h=new IntPtr(s.Handle);if(!s.Selected){if(!RestoreOne(s))Failed++;continue;}var placement=s.Placement;placement.showCmd=1;Native.SetWindowPlacement(h,ref placement);var box=Fit(new Rectangle(s.X,s.Y,s.W,s.H),from,to);if(!Native.SetWindowPos(h,IntPtr.Zero,box.X,box.Y,box.Width,box.Height,0x214))Failed++;}}
 static List<Saved> Read(){return File.Exists(Journal)?new JavaScriptSerializer().Deserialize<List<Saved>>(File.ReadAllText(Journal)):new List<Saved>();}
 static bool RestoreOne(Saved s){var p=s.Placement;var h=new IntPtr(s.Handle);Native.Rect current;if(Native.GetWindowRect(h,out current)&&current.Box==new Rectangle(s.X,s.Y,s.W,s.H)&&Native.ReadPlacement(h).showCmd==p.showCmd)return true;bool ok=Native.SetWindowPlacement(h,ref p);if(p.showCmd==1)ok=Native.SetWindowPos(h,IntPtr.Zero,s.X,s.Y,s.W,s.H,0x214)&&ok;return ok;}
 public static void Restore(){if(!File.Exists(Journal))return;int failed=0;var saved=Read();for(int attempt=0;attempt<3;attempt++){var until=DateTime.UtcNow.AddMilliseconds(250);while(DateTime.UtcNow<until){Application.DoEvents();System.Threading.Thread.Sleep(10);}failed=0;foreach(var s in saved){if(!Valid(s))continue;if(!RestoreOne(s))failed++;}}Failed=failed;if(failed>0)File.Copy(Journal,Journal+".unrestored",true);File.Delete(Journal);}
}
}
