using System;using System.Drawing;using System.Linq;using System.Collections.Generic;using System.Runtime.InteropServices;
namespace MonitorSlice {
public sealed class MirrorPointer:IDisposable {
 [StructLayout(LayoutKind.Sequential)]struct Data{public Native.Point Position;public uint Mouse,Flags,Time;public IntPtr Extra;}
 delegate IntPtr Hook(int code,IntPtr message,IntPtr data);
 [DllImport("user32.dll")]static extern IntPtr SetWindowsHookEx(int id,Hook callback,IntPtr module,uint thread);
 [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
 [DllImport("user32.dll")]static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")]static extern bool GetCursorPos(out Native.Point point);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string name);
 readonly Rectangle output;readonly List<Viewport> views;readonly Rectangle[] visible;readonly Hook callback;IntPtr handle;RelativeMouse raw;bool warping;Point shown,actual;double fractionX,fractionY;
 public Point VisualPosition{get{return shown;}}
 public static Point Constrain(Point p,IEnumerable<Rectangle> regions){var all=regions.ToArray();if(all.Any(r=>r.Contains(p)))return p;Point best=p;double distance=double.MaxValue;foreach(var r in all){var q=new Point(Math.Max(r.Left,Math.Min(r.Right-1,p.X)),Math.Max(r.Top,Math.Min(r.Bottom-1,p.Y)));double dx=(double)p.X-q.X,dy=(double)p.Y-q.Y;double d=dx*dx+dy*dy;if(d<distance){distance=d;best=q;}}return best;}
 public void MoveRelative(int dx,int dy){if(dx==0&&dy==0)return;double scale=PointerSettings.Scale;fractionX+=dx*scale;fractionY+=dy*scale;int x=(int)Math.Truncate(fractionX),y=(int)Math.Truncate(fractionY);fractionX-=x;fractionY-=y;MoveVisual(new Point(shown.X+x,shown.Y+y));}
 public void MoveVisual(Point point){shown=Constrain(point,visible);Point source=shown;foreach(var v in views)if(v.Destination.Contains(shown)){source=PointerBridge.Map(shown,v.Destination,v.Source);break;}warping=true;try{if(!SetCursorPos(source.X,source.Y))throw new System.ComponentModel.Win32Exception();Native.Point landed;if(GetCursorPos(out landed))actual=new Point(landed.X,landed.Y);else actual=source;}finally{warping=false;}}
 public MirrorPointer(Rectangle target,List<Viewport> regions){output=target;views=regions;var virtualNames=Displays.Virtual().Select(d=>d.Name).ToArray();visible=Displays.All().Where(d=>d.Attached&&!virtualNames.Contains(d.Name)).Select(d=>new Rectangle(d.Mode.X,d.Mode.Y,d.Mode.Width,d.Mode.Height)).ToArray();Native.Point p;GetCursorPos(out p);shown=actual=Constrain(new Point(p.X,p.Y),visible);PointerSettings.RefreshWindowsSpeed();callback=Process;handle=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);if(handle==IntPtr.Zero)throw new System.ComponentModel.Win32Exception();try{raw=new RelativeMouse(MoveRelative);MoveVisual(shown);}catch{UnhookWindowsHookEx(handle);handle=IntPtr.Zero;throw;}}
 IntPtr Process(int code,IntPtr message,IntPtr data){if(code<0||warping||message.ToInt32()!=0x200)return CallNextHookEx(handle,code,message,data);var m=(Data)Marshal.PtrToStructure(data,typeof(Data));var p=new Point(m.Position.X,m.Position.Y);if((m.Flags&1)==0)return new IntPtr(1);if(m.Extra==new IntPtr(0x4D53)){MoveVisual(new Point(shown.X+p.X-actual.X,shown.Y+p.Y-actual.Y));return new IntPtr(1);}actual=p;return CallNextHookEx(handle,code,message,data);}
 public void Dispose(){if(raw!=null){raw.Dispose();raw=null;}if(handle!=IntPtr.Zero){UnhookWindowsHookEx(handle);handle=IntPtr.Zero;}SetCursorPos(output.X+output.Width/2,output.Y+output.Height/2);}
}
}
