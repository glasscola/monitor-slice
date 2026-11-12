using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace MonitorSlice {
public static class Displays {
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct Device {
  public int Size;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Name;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Description;public int Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Id;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Key;
 }
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct Mode {
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string DeviceName;public short SpecVersion,DriverVersion,Size,Extra;public int Fields,X,Y,Orientation,FixedOutput;public short Color,Duplex,YResolution,TTOption,Collate;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string FormName;public short LogPixels;public int Bits,Width,Height,Flags,Frequency,ICMMethod,ICMIntent,Media,Dither,Reserved1,Reserved2,PanningWidth,PanningHeight;
 }
 public class Snapshot {public string Name,Description;public bool Attached,Primary;public Mode Mode;}
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool EnumDisplayDevices(string device,uint number,ref Device data,uint flags);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool EnumDisplaySettingsEx(string name,int mode,ref Mode data,int flags);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int ChangeDisplaySettingsEx(string name,ref Mode mode,IntPtr hwnd,uint flags,IntPtr parameter);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="ChangeDisplaySettingsExW")]static extern int ApplyAll(string name,IntPtr mode,IntPtr hwnd,uint flags,IntPtr parameter);
 public static List<Snapshot> Virtual(){return All().Where(d=>d.Description.IndexOf("Virtual Display Driver",StringComparison.OrdinalIgnoreCase)>=0).ToList();} public static List<Snapshot> All(){var result=new List<Snapshot>();for(uint i=0;i<32;i++){var d=new Device();d.Size=Marshal.SizeOf(typeof(Device));if(!EnumDisplayDevices(null,i,ref d,0))break;if(d.Description==null)continue;var m=new Mode();m.Size=(short)Marshal.SizeOf(typeof(Mode));bool attached=(d.Flags&1)!=0;if(!EnumDisplaySettingsEx(d.Name,attached?-1:-2,ref m,0)){m=new Mode();m.Size=(short)Marshal.SizeOf(typeof(Mode));}result.Add(new Snapshot{Name=d.Name,Description=d.Description,Attached=attached,Primary=(d.Flags&4)!=0,Mode=m});}return result.OrderBy(d=>d.Name,StringComparer.Ordinal).ToList();}
 public static Mode Resolution(string name,int width,int height){for(int i=0;i<4000;i++){var m=new Mode();m.Size=(short)Marshal.SizeOf(typeof(Mode));if(!EnumDisplaySettingsEx(name,i,ref m,0))break;if(m.Width==width&&m.Height==height&&m.Bits==32&&m.Frequency==60)return m;}throw new Exception(width+"×"+height+" 가상 모니터 모드가 없습니다. 드라이버 설정 업데이트가 필요합니다.");}
 public static void Stage(string name,Mode mode,bool primary=false){mode.Size=(short)Marshal.SizeOf(typeof(Mode));mode.Extra=0;mode.Fields=mode.Width>0?0x20|0x80000|0x100000|0x40000|0x400000|0x80:0x20|0x80000|0x100000;int result=ChangeDisplaySettingsEx(name,ref mode,IntPtr.Zero,0x10000001u,IntPtr.Zero);System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","config-trace.txt"),DateTime.UtcNow.ToString("o")+" "+name+" "+mode.X+","+mode.Y+" "+mode.Width+"x"+mode.Height+" @"+mode.Frequency+" primary="+primary+" result="+result+"\\n".Replace("\\n",Environment.NewLine));if(result!=0)throw new Exception("디스플레이 구성 실패: "+name+" / 코드 "+result);}
 public static Mode Detach(Mode original){var m=original;m.Fields=0x20|0x80000|0x100000;m.Width=0;m.Height=0;return m;}
 public static void Probe(){var rows=new List<object>();foreach(var d in All().Where(d=>d.Attached)){var m=d.Mode;rows.Add(new{name=d.Name,test=ChangeDisplaySettingsEx(d.Name,ref m,IntPtr.Zero,2,IntPtr.Zero),current=d.Mode});}System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence","probe.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(rows));}
 public static void Commit(){int result=ApplyAll(null,IntPtr.Zero,IntPtr.Zero,0,IntPtr.Zero);if(result!=0)throw new Exception("display commit "+result);}
 public static void Restore(List<Snapshot> states){foreach(var s in states.OrderByDescending(d=>d.Primary))Stage(s.Name,s.Attached?s.Mode:Detach(s.Mode),s.Primary);Commit();}
}
}
