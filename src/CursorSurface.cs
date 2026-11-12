using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace MonitorSlice {
sealed class CursorSurface:Form {
 [StructLayout(LayoutKind.Sequential)]struct Info {public int Size,Flags;public IntPtr Handle;public Native.Point Position;}
 [StructLayout(LayoutKind.Sequential)]struct IconInfo {public bool Icon;public uint X,Y;public IntPtr Mask,Color;}
 [DllImport("user32.dll")]static extern bool GetCursorInfo(ref Info info);
 [DllImport("user32.dll")]static extern bool GetIconInfo(IntPtr icon,out IconInfo info);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr item);
 [DllImport("user32.dll")]static extern bool DrawIconEx(IntPtr dc,int x,int y,IntPtr icon,int width,int height,uint step,IntPtr brush,uint flags);
 readonly List<Viewport> views;readonly Timer timer=new Timer{Interval=15};IntPtr icon;
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x20|0x80;return p;}}
 public CursorSurface(List<Viewport> values){views=values;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.Magenta;TransparencyKey=Color.Magenta;Size=new Size(64,64);StartPosition=FormStartPosition.Manual;timer.Tick+=Tick;timer.Start();}
 void Tick(object sender,EventArgs args){var info=new Info{Size=Marshal.SizeOf(typeof(Info))};if(!GetCursorInfo(ref info)||(info.Flags&1)==0){Hide();return;}var point=new Point(info.Position.X,info.Position.Y);foreach(var v in views)if(v.Source.Contains(point)){point=PointerBridge.Map(point,v.Source,v.Destination);IconInfo shape;int x=0,y=0;if(GetIconInfo(info.Handle,out shape)){x=(int)shape.X;y=(int)shape.Y;if(shape.Mask!=IntPtr.Zero)DeleteObject(shape.Mask);if(shape.Color!=IntPtr.Zero)DeleteObject(shape.Color);}icon=info.Handle;Location=new Point(point.X-x,point.Y-y);if(!Visible)Show();Invalidate();return;}Hide();}
 protected override void OnPaint(PaintEventArgs e){if(icon==IntPtr.Zero)return;var dc=e.Graphics.GetHdc();try{DrawIconEx(dc,0,0,icon,0,0,0,IntPtr.Zero,3);}finally{e.Graphics.ReleaseHdc(dc);}}
 protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
}
