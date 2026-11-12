using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
namespace MonitorSlice {
public class Region {public int Slot,X,Y,W,H;public Region(int s,int x,int y,int w,int h){Slot=s;X=x;Y=y;W=w;H=h;}}
public static class Layouts {
 public static readonly string[] Keys={"grid","cols","rows","top","bottom","full"};
 public static readonly string[] Names={"4칸","좌우 2칸","상하 2칸","위쪽 넓게","아래쪽 넓게","전체 1칸"};
 public static List<Region> Create(string mode){switch(mode){
  case "cols":return new List<Region>{new Region(0,0,0,1,2),new Region(1,1,0,1,2)};
  case "rows":return new List<Region>{new Region(0,0,0,2,1),new Region(2,0,1,2,1)};
  case "top":return new List<Region>{new Region(0,0,0,2,1),new Region(2,0,1,1,1),new Region(3,1,1,1,1)};
  case "bottom":return new List<Region>{new Region(0,0,0,1,1),new Region(1,1,0,1,1),new Region(2,0,1,2,1)};
  case "full":return new List<Region>{new Region(0,0,0,2,2)};
  default:return new List<Region>{new Region(0,0,0,1,1),new Region(1,1,0,1,1),new Region(2,0,1,1,1),new Region(3,1,1,1,1)};
 }}
 public static Rectangle Bounds(Region r,Rectangle area,int gap){int x=area.X+area.Width*r.X/2,y=area.Y+area.Height*r.Y/2;int right=area.X+area.Width*(r.X+r.W)/2,bottom=area.Y+area.Height*(r.Y+r.H)/2;return new Rectangle(x+gap,y+gap,right-x-gap*2,bottom-y-gap*2);}
 public static List<Region> Merge(List<Region> source,IEnumerable<int> selected,int keep){var ids=new HashSet<int>(selected);var rs=source.Where(r=>ids.Contains(r.Slot)).ToList();if(rs.Count<2)throw new Exception("이웃한 영역을 두 개 이상 선택하세요.");int x=rs.Min(r=>r.X),y=rs.Min(r=>r.Y),w=rs.Max(r=>r.X+r.W)-x,h=rs.Max(r=>r.Y+r.H)-y;if(rs.Sum(r=>r.W*r.H)!=w*h)throw new Exception("직사각형을 이루는 영역만 합칠 수 있습니다.");if(!ids.Contains(keep))throw new Exception("선택한 영역 중 표시할 창을 골라 주세요.");var result=source.Where(r=>!ids.Contains(r.Slot)).ToList();result.Add(new Region(keep,x,y,w,h));return result;}
}
}
