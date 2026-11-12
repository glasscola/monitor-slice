using System;
using System.IO;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace MonitorSlice {
static class SetupFlow {
 static readonly string Journal=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","setup-display-recovery.json");
 public static void RecoverDesktop(){if(!File.Exists(Journal))return;var state=new JavaScriptSerializer().Deserialize<Ccd.State>(File.ReadAllText(Journal));Ccd.Set(state,false);DesktopIcons.Restore(true);File.Delete(Journal);}
 public static void Run(string operation){RecoverDesktop();Directory.CreateDirectory(Path.GetDirectoryName(Journal));DesktopIcons.Save();File.WriteAllText(Journal,new JavaScriptSerializer().Serialize(Ccd.Read(false)));try{using(var p=Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--setup-"+operation){UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Hidden})){while(!p.HasExited){Application.DoEvents();System.Threading.Thread.Sleep(50);}if(p.ExitCode!=0){string log=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","setup-result.txt");throw new Exception(File.Exists(log)?File.ReadAllText(log):"설치 작업을 완료하지 못했습니다. 설치 복구를 눌러 주세요.");}}}finally{RecoverDesktop();}}
}
}
