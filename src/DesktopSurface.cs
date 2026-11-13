using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace MonitorSlice {
public class Viewport {public int Slot;public Rectangle Source,Destination;public IntPtr Lens,Capture;}
public sealed class DesktopSurface:Form {
 [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowDisplayAffinity(IntPtr window,uint affinity);
 [DllImport("MonitorSlice.Capture.dll",CallingConvention=CallingConvention.Cdecl)]static extern int StartCapture(IntPtr window,IntPtr monitor,out IntPtr result);
 [DllImport("MonitorSlice.Capture.dll",CallingConvention=CallingConvention.Cdecl)]static extern void StopCapture(IntPtr result);
 [DllImport("MonitorSlice.Capture.dll",CallingConvention=CallingConvention.Cdecl)]static extern ulong CaptureFrames(IntPtr result);
 [DllImport("MonitorSlice.Capture.dll",CallingConvention=CallingConvention.Cdecl)]static extern int CaptureError(IntPtr result);
 [DllImport("MonitorSlice.Capture.dll",CallingConvention=CallingConvention.Cdecl)]static extern int StartDesktopCopy(IntPtr window,int x,int y,out IntPtr result);
 [DllImport("MonitorSlice.Capture.dll",CallingConvention=CallingConvention.Cdecl)]static extern int StartDupCapture(IntPtr window,IntPtr monitor,out IntPtr result);
 bool copy=Environment.CommandLine.Contains("--gdi-output");
 [DllImport("user32.dll")]static extern IntPtr MonitorFromPoint(Native.Point point,uint flags);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateWindowEx(int ex,string name,string title,int style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr parameter);
 [DllImport("user32.dll")]static extern bool DestroyWindow(IntPtr window);
 [DllImport("MonitorSlice.Capture.dll",CallingConvention=CallingConvention.Cdecl)]static extern ulong CaptureReceived(IntPtr result);
 [DllImport("MonitorSlice.Capture.dll",CallingConvention=CallingConvention.Cdecl)]static extern double CaptureAgeMs(IntPtr result);
 public double[] FrameAgeMs{get{return Views.Select(v=>CaptureAgeMs(v.Capture)).ToArray();}}
 public ulong Received {get{if(copy)return CaptureReceived(Views[0].Capture)*(ulong)Views.Count;return Views.Aggregate<Viewport,ulong>(0,(count,v)=>count+CaptureReceived(v.Capture));}}
 CursorSurface cursor;DwmSurface direct;readonly bool useDirect=Environment.CommandLine.Contains("--compat-output");
 public string Backend{get{return Environment.CommandLine.Contains("--wgc-capture")?"wgc":"desktop-duplication";}}
 public void ConnectPointer(Func<Point> position){if(cursor!=null)cursor.PositionProvider=position;}
 readonly Rectangle outputTarget;public readonly List<Viewport> Views;
 public ulong Frames {get{if(copy)return CaptureFrames(Views[0].Capture)*(ulong)Views.Count;return Views.Aggregate<Viewport,ulong>(0,(count,v)=>count+CaptureFrames(v.Capture));}}
 public ulong[] PerViewFrames {get{return Views.Select(v=>CaptureFrames(copy?Views[0].Capture:v.Capture)).ToArray();}}
 public int[] Errors {get{return Views.Select(v=>CaptureError(v.Capture)).ToArray();}}
 protected override bool ShowWithoutActivation {get{return true;}}
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x20;return p;}}
 public DesktopSurface(Rectangle target,List<Viewport> views){outputTarget=target;Views=views;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=target;Opacity=0.99;BackColor=Color.Black;ShowInTaskbar=false;TopMost=true;Text="Monitor Slice 바탕화면 · GPU";AutoScaleMode=AutoScaleMode.None;}
 public void Open(){if(useDirect){direct=new DwmSurface(Bounds,Views);direct.Open();return;}Show();if(!SetWindowDisplayAffinity(Handle,0x11))throw new System.ComponentModel.Win32Exception();if(!Native.SetWindowPos(Handle,new IntPtr(-1),outputTarget.X,outputTarget.Y,outputTarget.Width,outputTarget.Height,0x210))throw new System.ComponentModel.Win32Exception();if(copy){Marshal.ThrowExceptionForHR(StartDesktopCopy(Handle,Views.Min(v=>v.Source.Left),Views.Min(v=>v.Source.Top),out Views[0].Capture));cursor=new CursorSurface(Views);return;}foreach(var v in Views){var d=v.Destination;v.Lens=CreateWindowEx(0,"STATIC","",0x50000000,d.X-Left,d.Y-Top,d.Width,d.Height,Handle,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);if(v.Lens==IntPtr.Zero)throw new Exception("출력 영역 생성 실패");int result=0;for(int attempt=0;attempt<80;attempt++){var mon=MonitorFromPoint(new Native.Point{X=v.Source.X+v.Source.Width/2,Y=v.Source.Y+v.Source.Height/2},0);result=!Environment.CommandLine.Contains("--wgc-capture")?StartDupCapture(v.Lens,mon,out v.Capture):StartCapture(v.Lens,mon,out v.Capture);if(result>=0)break;Application.DoEvents();System.Threading.Thread.Sleep(75);}if(result<0)Marshal.ThrowExceptionForHR(result);}cursor=new CursorSurface(Views);}
 void FallbackCopy(){foreach(var v in Views){if(v.Capture!=IntPtr.Zero){StopCapture(v.Capture);v.Capture=IntPtr.Zero;}if(v.Lens!=IntPtr.Zero){DestroyWindow(v.Lens);v.Lens=IntPtr.Zero;}}copy=true;Marshal.ThrowExceptionForHR(StartDesktopCopy(Handle,Views.Min(v=>v.Source.Left),Views.Min(v=>v.Source.Top),out Views[0].Capture));cursor=new CursorSurface(Views);}
 protected override void Dispose(bool disposing){if(disposing){if(cursor!=null){cursor.Dispose();cursor=null;}if(direct!=null){direct.Dispose();direct=null;}foreach(var v in Views){if(v.Capture!=IntPtr.Zero){StopCapture(v.Capture);v.Capture=IntPtr.Zero;}if(v.Lens!=IntPtr.Zero){DestroyWindow(v.Lens);v.Lens=IntPtr.Zero;}}}base.Dispose(disposing);}
}
}
