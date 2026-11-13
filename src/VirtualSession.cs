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
 List<Displays.Snapshot> original;Ccd.State ccdOriginal;DesktopSurface surface;PointerBridge pointer;WindowSafety safety;MirrorPointer mirrorPointer;
 public bool Active {get{return original!=null;}}public bool Changing;public List<Viewport> Views=new List<Viewport>();
 public Rectangle OutputBounds;public ulong[] PerViewFrames {get{return surface==null?new ulong[0]:surface.PerViewFrames;}}public ulong Received {get{return surface==null?0:surface.Received;}}public ulong Frames {get{return surface==null?0:surface.Frames;}}public int[] RenderErrors {get{return surface==null?new int[0]:surface.Errors;}}
 public string Backend{get{return surface==null?"disabled":surface.Backend;}}
 public double[] FrameAgeMs{get{return surface==null?new double[0]:surface.FrameAgeMs;}}
 public MirrorPointer Input{get{return mirrorPointer;}}
 public void RaiseOutput(){if(surface!=null)Native.SetWindowPos(surface.Handle,new IntPtr(-1),OutputBounds.X,OutputBounds.Y,OutputBounds.Width,OutputBounds.Height,0x210);}
 public PointerBridge Bridge {get{return pointer;}}
 public static bool Ready {get{return Displays.Virtual().Count>=3;}}
 public void Recover(){if(!File.Exists(journal)){WindowCarry.Restore();DesktopIcons.Restore(true);return;}var states=new JavaScriptSerializer().Deserialize<Ccd.State>(File.ReadAllText(journal));Changing=true;try{Ccd.Set(states,false);WindowCarry.Restore();DesktopIcons.Restore(true);File.Delete(journal);}finally{Changing=false;}}
 public void Start(Rectangle target,List<Region> regions,bool input){if(Active)Stop();var adapters=Displays.Virtual();if(adapters.Count<regions.Count-1)throw new Exception("추가 가상 디스플레이가 필요합니다. 설치 · 자동 설정을 실행하세요.");
  var all=Displays.All();var physical=all.FirstOrDefault(s=>s.Attached&&!adapters.Any(a=>a.Name==s.Name)&&s.Mode.X==target.X&&s.Mode.Y==target.Y);if(physical==null)throw new Exception("실제 모니터를 확인할 수 없습니다.");
  var bounds=regions.Select(r=>Layouts.Bounds(r,target,0)).ToArray();var modes=regions.Select((r,i)=>adapters.Count>0?Displays.Resolution(adapters[0].Name,bounds[i].Width,bounds[i].Height):physical.Mode).ToArray();
  ccdOriginal=Ccd.Read(false);DesktopIcons.Save();try{WindowCarry.Save(target);}catch{DesktopIcons.Restore(true);throw;}original=all.Where(s=>s.Attached||adapters.Any(a=>a.Name==s.Name)).ToList();Directory.CreateDirectory(Path.GetDirectoryName(journal));File.WriteAllText(journal,new JavaScriptSerializer().Serialize(ccdOriginal));Changing=true;
  try{
   var planned=all.Where(d=>d.Attached&&!adapters.Any(a=>a.Name==d.Name)).ToList();
   Views.Clear();int firstIndex=Enumerable.Range(0,regions.Count).OrderBy(i=>bounds[i].Top).ThenBy(i=>bounds[i].Left).First();
   Views.Add(new Viewport{Slot=regions[firstIndex].Slot,Source=target,Destination=bounds[firstIndex]});
   int offX=all.Where(d=>d.Attached&&!adapters.Any(a=>a.Name==d.Name)).Min(d=>d.Mode.X)-target.Width*4;int next=0;
   for(int i=0;i<regions.Count;i++){if(i==firstIndex)continue;var mode=modes[i];mode.X=offX;mode.Y=target.Y;offX+=mode.Width;mode.Orientation=0;mode.Fields=0x20|0x80000|0x100000|0x40000|0x400000|0x80;planned.Add(new Displays.Snapshot{Name=adapters[next].Name,Attached=true,Primary=false,Mode=mode});Views.Add(new Viewport{Slot=regions[i].Slot,Source=new Rectangle(mode.X,mode.Y,mode.Width,mode.Height),Destination=bounds[i]});next++;}
   Ccd.Apply(planned);OutputBounds=target;var applied=Displays.All();var panel=applied.First(d=>d.Name==physical.Name);if(new Rectangle(panel.Mode.X,panel.Mode.Y,panel.Mode.Width,panel.Mode.Height)!=target)throw new Exception("원본 물리 모니터 배치가 변경되어 분할을 취소합니다.");int vi=0;foreach(var view in Views.Skip(1)){var adapterName=adapters[vi++].Name;var item=applied.First(d=>d.Name==adapterName);view.Source=new Rectangle(item.Mode.X,item.Mode.Y,item.Mode.Width,item.Mode.Height);}
   if(!Environment.CommandLine.Contains("--no-capture")){surface=new DesktopSurface(OutputBounds,Views);surface.Open();}
   DesktopIcons.Restore(false);if(input){mirrorPointer=new MirrorPointer(OutputBounds,Views);if(surface!=null)surface.ConnectPointer(()=>mirrorPointer.VisualPosition);}

  }catch{Stop();throw;}finally{Changing=false;}
 }
 public void RescueWindows(){if(safety!=null)safety.Rescue();}
 public int RescueFailures{get{return safety==null?0:safety.Failed;}}
 public void Stop(){Changing=true;try{if(safety!=null){safety.Dispose();safety=null;}if(mirrorPointer!=null){mirrorPointer.Dispose();mirrorPointer=null;}if(pointer!=null){pointer.Dispose();pointer=null;}if(surface!=null){surface.Dispose();surface=null;}if(original!=null){foreach(var window in Native.Windows()){Native.Rect wr;if(!Native.GetWindowRect(window.Handle,out wr))continue;var box=wr.Box;foreach(var view in Views.Skip(1))if(view.Source.Contains(box.X+box.Width/2,box.Y+box.Height/2)){var fit=WindowCarry.Fit(box,view.Source,OutputBounds);Native.SetWindowPos(window.Handle,IntPtr.Zero,fit.X,fit.Y,fit.Width,fit.Height,0x214);break;}}Ccd.Set(ccdOriginal,false);WindowCarry.Restore();DesktopIcons.Restore(true);original=null;if(File.Exists(journal))File.Delete(journal);}Views.Clear();}finally{Changing=false;}}
 public void Dispose(){Stop();}
}
}
