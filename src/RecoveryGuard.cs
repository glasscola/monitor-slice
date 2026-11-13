using System;using System.Diagnostics;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace MonitorSlice {
static class RecoveryGuard {
 [DllImport("Magnification.dll")]static extern bool MagInitialize();
 [DllImport("Magnification.dll")]static extern bool MagShowSystemCursor(bool show);
 [DllImport("Magnification.dll")]static extern bool MagUninitialize();
 public static void Start(){var p=Process.GetCurrentProcess();Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--watch-parent "+p.Id+" "+p.StartTime.ToUniversalTime().Ticks){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});}
 public static void Watch(int pid,long ticks){try{var parent=Process.GetProcessById(pid);if(parent.StartTime.ToUniversalTime().Ticks!=ticks)return;parent.WaitForExit();}catch(ArgumentException){}try{if(MagInitialize()){MagShowSystemCursor(true);MagUninitialize();}SetupFlow.RecoverDesktop();new VirtualSession().Recover();}catch(Exception error){System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence"));System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","watchdog-error.txt"),error.ToString());}}
}
}
