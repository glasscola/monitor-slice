using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace MonitorSlice {
public class Diagram:Control {
 public List<Region> Regions=Layouts.Create("grid");public HashSet<int> Selected=new HashSet<int>();public bool Editing;public string[] Titles={"창을 연결하세요","창을 연결하세요","창을 연결하세요","창을 연결하세요"};
 public Action<int> RegionClick;
 public Diagram(){DoubleBuffered=true;BackColor=Color.FromArgb(237,243,252);Cursor=Cursors.Hand;}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);foreach(var r in Regions){var b=Layouts.Bounds(r,ClientRectangle,4);bool chosen=Selected.Contains(r.Slot);using(var brush=new SolidBrush(chosen?Color.FromArgb(45,102,218):Color.FromArgb(218,231,253)))e.Graphics.FillRectangle(brush,b);using(var pen=new Pen(chosen?Color.FromArgb(25,70,170):Color.FromArgb(115,151,207),2))e.Graphics.DrawRectangle(pen,b);
 if(Width>240){var color=chosen?Color.White:Color.FromArgb(30,66,119);TextRenderer.DrawText(e.Graphics,(Editing?(chosen?"✓ 선택됨 · ":"＋ 선택 · "):"")+"영역 "+(char)('A'+r.Slot),Font,new Rectangle(b.X+8,b.Y+b.Height/2-24,b.Width-16,24),color,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);TextRenderer.DrawText(e.Graphics,Titles[r.Slot],Font,new Rectangle(b.X+8,b.Y+b.Height/2+3,b.Width-16,32),color,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);}}}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);foreach(var r in Regions)if(Layouts.Bounds(r,ClientRectangle,4).Contains(e.Location)){if(RegionClick!=null)RegionClick(r.Slot);break;}}
}
public class LayoutButton:Button {
 public string Mode;public bool Chosen;
 public LayoutButton(string mode,string name){Mode=mode;Text=name;Size=new Size(116,77);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderColor=Color.LightSteelBlue;BackColor=Color.White;TextAlign=ContentAlignment.BottomCenter;Padding=new Padding(0,0,0,5);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.DrawRectangle(Pens.LightSteelBlue,0,0,Width-1,Height-1);foreach(var r in Layouts.Create(Mode)){var b=Layouts.Bounds(r,new Rectangle(25,7,64,38),2);using(var brush=new SolidBrush(Chosen?Color.FromArgb(130,173,249):Color.FromArgb(214,229,252)))e.Graphics.FillRectangle(brush,b);e.Graphics.DrawRectangle(Pens.CornflowerBlue,b);}using(var labelFont=new Font("맑은 고딕",8))TextRenderer.DrawText(e.Graphics,Text,labelFont,new Rectangle(3,47,Width-6,25),Color.FromArgb(25,55,95),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);if(Chosen){using(var p=new Pen(Color.RoyalBlue,2))e.Graphics.DrawRectangle(p,1,1,Width-3,Height-3);}}
}
}
