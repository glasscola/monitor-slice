using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
namespace MonitorSlice {
static class Program {
 [STAThread] static void Main(string[] args){try{try{Native.SetProcessDpiAwareness(2);}catch{}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  if(Array.IndexOf(args,"--ui-shot")>=0){using(var form=new MainForm()){form.Show();Native.ShowWindowAsync(form.Handle,5);Application.DoEvents();using(var bitmap=new System.Drawing.Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new System.Drawing.Rectangle(System.Drawing.Point.Empty,bitmap.Size));System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence"));bitmap.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","mirror-ui-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".png"));}form.Close();}return;}
  if(Array.IndexOf(args,"--mirror-recovery-test")>=0){MirrorRecoveryTest.Run();return;}
  if(Array.IndexOf(args,"--mirror-crash-child")>=0){MirrorRecoveryTest.Child();return;}
  if(args.Length==3&&args[0]=="--watch-parent"){RecoveryGuard.Watch(int.Parse(args[1]),long.Parse(args[2]));return;}
  if(Array.IndexOf(args,"--mirror-integration")>=0){MirrorIntegration.Run();return;}
  if(Array.IndexOf(args,"--mirror-test")>=0){MirrorTests.Run();return;}
  if(Array.IndexOf(args,"--window-safety-test")>=0){WindowSafetyTests.Run();return;}
  if(Array.IndexOf(args,"--direct-output-probe")>=0){DirectOutput.WriteReport();return;}
  if(Array.IndexOf(args,"--carry-test")>=0){MirrorIntegration.Run();return;}
  if(Array.IndexOf(args,"--setup-request-install")>=0){SetupFlow.Run("install");return;}
  if(Array.IndexOf(args,"--setup-rollback-test")>=0){string config=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"MonitorSlice","Driver","vdd_settings.xml");string before=File.ReadAllText(config);bool injected=false;try{SetupFlow.Run("test-failure");}catch(Exception error){injected=error.Message.Contains("실패 주입");}if(!injected||File.ReadAllText(config)!=before||!VirtualSession.Ready)throw new Exception("설치 자동 복구 검사 실패");File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","setup-rollback-test.txt"),"Injected failure rolled back; original configuration and four devices retained.");return;}
  if(Array.IndexOf(args,"--setup-cycle-test")>=0){var results=new System.Collections.Generic.List<object>();foreach(string op in new[]{"install","install","recover","remove","install"}){SetupFlow.Run(op);bool ready=VirtualSession.Ready;if(op!="remove"&&!ready)throw new Exception("설치 후 장치 확인 실패");if(op=="remove"&&Displays.Virtual().Count!=0)throw new Exception("제거 후 장치 잔존");results.Add(new{operation=op,ready=ready,virtualDisplays=Displays.Virtual().Count});}File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","setup-cycle-test.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(results));return;}
  foreach(string op in new[]{"install","recover","remove","test-failure"})if(Array.IndexOf(args,"--setup-"+op)>=0){string result=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","setup-result.txt");Directory.CreateDirectory(Path.GetDirectoryName(result));try{var task=System.Threading.Tasks.Task.Factory.StartNew(()=>DriverSetup.Run(op));while(!task.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(30);}task.GetAwaiter().GetResult();File.WriteAllText(result,"완료");}catch(Exception error){File.WriteAllText(result,error.Message);Environment.ExitCode=1;}return;}
  if(Array.IndexOf(args,"--gpu-bench")>=0){FrameBench.RunGpu();return;}
  if(Array.IndexOf(args,"--frame-bench")>=0){FrameBench.Run();return;}
  if(Array.IndexOf(args,"--icons-test")>=0){DesktopIcons.Restore(true);DesktopIcons.Save();DesktopIcons.Restore(true);File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","icons-api-test.txt"),"Shell desktop icon save/restore completed.");return;}
  if(Array.IndexOf(args,"--api-probe")>=0){Displays.Probe();return;}
  if(Array.IndexOf(args,"--install-driver")>=0){SetupFlow.Run("install");return;}
  if(Array.IndexOf(args,"--driver-idle")>=0){foreach(var display in Displays.Virtual())Displays.Stage(display.Name,Displays.Detach(display.Mode));Displays.Commit();return;}
  if(Array.IndexOf(args,"--virtual-test")>=0){MirrorIntegration.Run();return;}
  if(Array.IndexOf(args,"--mode-check")>=0){VirtualTests.CheckModes();return;}
  using(var mutex=new Mutex(false,"Local\\"+"Monitor"+"Split-native-v2")){if(!mutex.WaitOne(0)){MessageBox.Show("Monitor Slice가 이미 실행 중입니다.");return;}try{RecoveryGuard.Start();Application.Run(new MainForm());}finally{mutex.ReleaseMutex();}}}
 catch(Exception ex){try{Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence"));File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","error-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".txt"),ex.ToString());}catch{}if(Array.IndexOf(args,"--ui-shot")<0&&Array.IndexOf(args,"--mirror-recovery-test")<0&&Array.IndexOf(args,"--mirror-crash-child")<0&&Array.IndexOf(args,"--mirror-integration")<0&&Array.IndexOf(args,"--mirror-test")<0&&Array.IndexOf(args,"--direct-output-probe")<0&&Array.IndexOf(args,"--window-safety-test")<0&&Array.IndexOf(args,"--carry-test")<0&&Array.IndexOf(args,"--setup-cycle-test")<0&&Array.IndexOf(args,"--virtual-test")<0&&Array.IndexOf(args,"--install-driver")<0)MessageBox.Show(ex.Message,"Monitor Slice 오류");Environment.ExitCode=1;}}
}
}
