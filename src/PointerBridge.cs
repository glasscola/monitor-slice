using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace MonitorSlice {
// Windows still delivers real keyboard and click input to its own desktop.
// Only the pointer's physical-display-to-virtual-display coordinates are mapped.
public sealed class PointerBridge:IDisposable {
 [StructLayout(LayoutKind.Sequential)]struct MouseData {public Native.Point Point;public uint Data,Flags,Time;public IntPtr Extra;}
 delegate IntPtr Hook(int code,IntPtr message,IntPtr data);
 [DllImport("user32.dll")]static extern IntPtr SetWindowsHookEx(int id,Hook callback,IntPtr module,uint thread);
 [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
 [DllImport("user32.dll")]static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")]static extern bool GetCursorPos(out Native.Point point);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string name);
 IntPtr handle;Hook callback;readonly List<Viewport> views;readonly Rectangle physical;bool warping;Viewport previous;
 public readonly List<Point> VerifiedProjections=new List<Point>();
 public PointerBridge(Rectangle physical,List<Viewport> views){this.physical=physical;this.views=views;callback=HandleMouse;handle=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);if(handle==IntPtr.Zero)throw new Exception("마우스 좌표 연결 실패");}
 public static Point Map(Point p,Rectangle from,Rectangle to){return new Point(to.X+(p.X-from.X)*to.Width/from.Width,to.Y+(p.Y-from.Y)*to.Height/from.Height);}
 void Warp(Point p){warping=true;try{if(SetCursorPos(p.X,p.Y)){Native.Point actual;if(GetCursorPos(out actual)){VerifiedProjections.Add(new Point(actual.X,actual.Y));if(VerifiedProjections.Count>32)VerifiedProjections.RemoveAt(0);}}}finally{warping=false;}}
 IntPtr HandleMouse(int code,IntPtr message,IntPtr data){if(code<0||warping||message.ToInt32()!=0x200)return CallNextHookEx(handle,code,message,data);var m=(MouseData)Marshal.PtrToStructure(data,typeof(MouseData));var p=new Point(m.Point.X,m.Point.Y);
  foreach(var v in views)if(v.Source.Contains(p)){previous=v;return CallNextHookEx(handle,code,message,data);}
  if(previous!=null&&physical.Contains(p)){
   int left=int.MaxValue,top=int.MaxValue,right=int.MinValue,bottom=int.MinValue;
   foreach(var view in views){left=Math.Min(left,view.Source.Left);top=Math.Min(top,view.Source.Top);right=Math.Max(right,view.Source.Right);bottom=Math.Max(bottom,view.Source.Bottom);}
   if(left==physical.Right){Warp(new Point(left,Math.Max(top,Math.Min(bottom-1,p.Y))));return new IntPtr(1);}
   if(right==physical.Left){Warp(new Point(right-1,Math.Max(top,Math.Min(bottom-1,p.Y))));return new IntPtr(1);}
  }
  foreach(var v in views)if(v.Destination.Contains(p)){previous=v;Warp(Map(p,v.Destination,v.Source));return new IntPtr(1);}
  previous=null;return CallNextHookEx(handle,code,message,data);
 }
 public void Dispose(){if(handle!=IntPtr.Zero){UnhookWindowsHookEx(handle);handle=IntPtr.Zero;}if(views.Count>0)SetCursorPos(views[0].Source.X+20,views[0].Source.Y+20);}
}
}
