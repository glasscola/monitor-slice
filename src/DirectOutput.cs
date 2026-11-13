using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
namespace MonitorSlice {
public static class DirectOutput {
 [StructLayout(LayoutKind.Sequential)] struct Support { public uint Type,Size;public Ccd.Luid Adapter;public uint Id,Flags; }
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct SourceName {public uint Type,Size;public Ccd.Luid Adapter;public uint Id;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Device;}
 [DllImport("user32.dll")] static extern int DisplayConfigGetDeviceInfo(ref Support data);
 [DllImport("user32.dll")] static extern int DisplayConfigGetDeviceInfo(ref SourceName data);
 [DllImport("MonitorSlice.Capture.dll",CallingConvention=CallingConvention.Cdecl)] static extern int ProbeDirectTarget(uint low,int high,uint id,out int acquired);
 public class Result {public string Device;public uint Target;public int QueryResult;public bool SystemAvailable,MonitorAvailable,Enabled,Acquired;public string AcquireHResult;public bool PresenterImplemented=false;}
 public static List<Result> Probe(){var rows=new List<Result>();var virtualNames=new HashSet<string>(Displays.Virtual().Select(d=>d.Name));foreach(var path in Ccd.Read(false).Paths){var name=new SourceName{Type=1,Size=(uint)Marshal.SizeOf(typeof(SourceName)),Adapter=path.Source.Adapter,Id=path.Source.Id};if(DisplayConfigGetDeviceInfo(ref name)!=0||virtualNames.Contains(name.Device))continue;var q=new Support{Type=12,Size=(uint)Marshal.SizeOf(typeof(Support)),Adapter=path.Target.Adapter,Id=path.Target.Id};int code=DisplayConfigGetDeviceInfo(ref q);int acquired;int hr=ProbeDirectTarget(q.Adapter.Low,q.Adapter.High,q.Id,out acquired);rows.Add(new Result{Device=name.Device,Target=q.Id,QueryResult=code,SystemAvailable=code==0&&(q.Flags&4)!=0,MonitorAvailable=code==0&&(q.Flags&2)!=0,Enabled=code==0&&(q.Flags&1)!=0,Acquired=acquired!=0,AcquireHResult="0x"+unchecked((uint)hr).ToString("X8")});}return rows;}
 public static string WriteReport(){var before=Ccd.Read(false);var rows=System.Threading.Tasks.Task.Factory.StartNew(()=>Probe()).GetAwaiter().GetResult();var after=Ccd.Read(false);var json=new JavaScriptSerializer();bool unchanged=json.Serialize(before)==json.Serialize(after);string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","direct-output-probe.json");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,json.Serialize(new{Utc=DateTime.UtcNow.ToString("o"),TopologyUnchanged=unchanged,Targets=rows,ExactSplitImplemented=false}));if(!unchanged)throw new Exception("출력 진단 중 디스플레이 구성이 변경되었습니다. 보고서를 확인하세요.");return path;}
}
}
