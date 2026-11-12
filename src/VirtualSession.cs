using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace MonitorSlice {
public sealed class VirtualSession:IDisposable {
 readonly string journal=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","virtual-recovery.json");
 List<Displays.Snapshot> original;Ccd.State ccdOriginal;DesktopSurface surface;PointerBridge pointer;
 public bool Active {get{return original!=null;}}public bool Changing;public List<Viewport> Views=new List<Viewport>();
 public Rectangle OutputBounds;public ulong[] PerViewFrames {get{return surface==null?new ulong[0]:surface.PerViewFrames;}}public ulong Received {get{return surface==null?0:surface.Received;}}public ulong Frames {get{return surface==null?0:surface.Frames;}}public int[] RenderErrors {get{return surface==null?new int[0]:surface.Errors;}}
 public string Backend{get{return surface==null?"disabled":surface.Backend;}}
 public double[] FrameAgeMs{get{return surface==null?new double[0]:surface.FrameAgeMs;}}
 public PointerBridge Bridge {get{return pointer;}}
 public static bool Ready {get{return Displays.Virtual().Count>=4;}}
 public void Recover(){if(!File.Exists(journal)){DesktopIcons.Restore(true);return;}var states=new JavaScriptSerializer().Deserialize<Ccd.State>(File.ReadAllText(journal));Changing=true;try{Ccd.Set(states,false);DesktopIcons.Restore(true);File.Delete(journal);}finally{Changing=false;}}
 public void Start(Rectangle target,List<Region> regions,bool input){if(Active)Stop();var adapters=Displays.Virtual();if(adapters.Count!=4)throw new Exception("가상 디스플레이 4개가 필요합니다.");
  var all=Displays.All();var physical=all.FirstOrDefault(s=>s.Attached&&!adapters.Any(a=>a.Name==s.Name)&&s.Mode.X==target.X&&s.Mode.Y==target.Y);if(physical==null)throw new Exception("실제 모니터를 확인할 수 없습니다.");
  var bounds=regions.Select(r=>Layouts.Bounds(r,target,0)).ToArray();var modes=regions.Select((r,i)=>Displays.Resolution(adapters[r.Slot].Name,bounds[i].Width,bounds[i].Height)).ToArray();
  DesktopIcons.Save();ccdOriginal=Ccd.Read(false);original=all.Where(s=>s.Attached||adapters.Any(a=>a.Name==s.Name)).ToList();Directory.CreateDirectory(Path.GetDirectoryName(journal));File.WriteAllText(journal,new JavaScriptSerializer().Serialize(ccdOriginal));Changing=true;
  try{
   // Virtual outputs occupy the old physical monitor coordinates; the actual panel
   // remains enabled solely as an off-layout presentation destination.
   var output=physical.Mode;output.X=all.Where(d=>d.Attached).Min(d=>d.Mode.X)-target.Width;output.Y=target.Y;output.Fields=0x20;var planned=new List<Displays.Snapshot>();
   Views.Clear();for(int i=0;i<4;i++){int ri=regions.FindIndex(r=>r.Slot==i);if(ri<0){continue;}var dest=bounds[ri];var mode=modes[ri];mode.X=dest.X;mode.Y=dest.Y;mode.Orientation=0;mode.Fields=0x20|0x80000|0x100000|0x40000|0x400000|0x80;planned.Add(new Displays.Snapshot{Name=adapters[i].Name,Attached=true,Primary=physical.Primary&&dest.Contains(Point.Empty),Mode=mode});Views.Add(new Viewport{Slot=i,Source=dest,Destination=new Rectangle(output.X+dest.X-target.X,output.Y+dest.Y-target.Y,dest.Width,dest.Height)});}
   planned.Add(new Displays.Snapshot{Name=physical.Name,Attached=true,Mode=output});planned.AddRange(all.Where(d=>d.Attached&&d.Name!=physical.Name&&!adapters.Any(a=>a.Name==d.Name)));Ccd.Apply(planned);var actual=Displays.All();var panel=actual.First(d=>d.Name==physical.Name);OutputBounds=new Rectangle(panel.Mode.X,panel.Mode.Y,panel.Mode.Width,panel.Mode.Height);
   foreach(var view in Views){var current=actual.First(d=>d.Name==adapters[view.Slot].Name);if(!current.Attached||current.Mode.Width!=view.Destination.Width||current.Mode.Height!=view.Destination.Height)throw new Exception("가상 모니터 해상도 적용 실패");view.Source=new Rectangle(current.Mode.X,current.Mode.Y,current.Mode.Width,current.Mode.Height);view.Destination=new Rectangle(OutputBounds.X+view.Source.X-target.X,OutputBounds.Y+view.Source.Y-target.Y,view.Source.Width,view.Source.Height);}
   if(!Environment.CommandLine.Contains("--no-capture")){surface=new DesktopSurface(OutputBounds,Views);surface.Open();}DesktopIcons.Restore(false);if(input)pointer=new PointerBridge(OutputBounds,Views);
  }catch{Stop();throw;}finally{Changing=false;}
 }
 public void Stop(){Changing=true;try{if(pointer!=null){pointer.Dispose();pointer=null;}if(surface!=null){surface.Dispose();surface=null;}if(original!=null){Ccd.Set(ccdOriginal,false);DesktopIcons.Restore(true);original=null;if(File.Exists(journal))File.Delete(journal);}Views.Clear();}finally{Changing=false;}}
 public void Dispose(){Stop();}
}
}
