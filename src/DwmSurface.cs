using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace MonitorSlice {
// One composed crop spanning all virtual outputs, rather than four independent crops.
public sealed class DwmSurface:Form {
 [StructLayout(LayoutKind.Sequential)]struct Transform {public float A,B,C,D,E,F,G,H,I;}
 [DllImport("Magnification.dll")]static extern bool MagInitialize();
 [DllImport("Magnification.dll")]static extern bool MagUninitialize();
 [DllImport("Magnification.dll")]static extern bool MagSetWindowSource(IntPtr h,Native.Rect rectangle);
 [DllImport("Magnification.dll")]static extern bool MagSetWindowTransform(IntPtr h,ref Transform matrix);
 [DllImport("Magnification.dll")]static extern bool MagSetWindowFilterList(IntPtr h,int mode,int count,IntPtr[] excluded);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateWindowEx(int ex,string name,string title,int style,int x,int y,int w,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr parameter);
 [DllImport("user32.dll")]static extern bool SetLayeredWindowAttributes(IntPtr h,uint key,byte alpha,uint flags);
 [DllImport("user32.dll")]static extern bool DestroyWindow(IntPtr h);
 readonly List<Viewport> views;readonly Timer timer=new Timer();IntPtr lens;bool initialized;Native.Rect source;
 protected override bool ShowWithoutActivation {get{return true;}}
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80000|0x08000000;return p;}}
 public DwmSurface(Rectangle target,List<Viewport> views){this.views=views;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=target;ShowInTaskbar=false;TopMost=true;BackColor=Color.Black;AutoScaleMode=AutoScaleMode.None;timer.Interval=16;timer.Tick+=delegate{MagSetWindowSource(lens,source);};}
 public void Open(){if(!MagInitialize())throw new Exception("DWM 출력 초기화 실패");initialized=true;source=new Native.Rect{Left=views.Min(v=>v.Source.Left),Top=views.Min(v=>v.Source.Top),Right=views.Max(v=>v.Source.Right),Bottom=views.Max(v=>v.Source.Bottom)};if(source.Right-source.Left!=Width||source.Bottom-source.Top!=Height)throw new Exception("합성 출력 크기를 확인할 수 없습니다.");Show();SetLayeredWindowAttributes(Handle,0,255,2);lens=CreateWindowEx(0,"Magnifier","",0x50000001,0,0,Width,Height,Handle,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);if(lens==IntPtr.Zero)throw new Exception("DWM 출력 영역 생성 실패");var identity=new Transform{A=1,E=1,I=1};MagSetWindowTransform(lens,ref identity);MagSetWindowFilterList(lens,0,1,new[]{Handle});if(!MagSetWindowSource(lens,source))throw new Exception("DWM 바탕화면 연결 실패");timer.Start();}
 protected override void Dispose(bool disposing){if(disposing){timer.Stop();timer.Dispose();if(lens!=IntPtr.Zero){DestroyWindow(lens);lens=IntPtr.Zero;}}base.Dispose(disposing);if(initialized){MagUninitialize();initialized=false;}}
}
}
