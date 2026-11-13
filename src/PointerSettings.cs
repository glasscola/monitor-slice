using System;using System.IO;using System.Runtime.InteropServices;
namespace MonitorSlice {
static class PointerSettings {
 [DllImport("user32.dll")]static extern bool SystemParametersInfo(uint action,uint value,out int data,uint flags);
 static readonly string PathName=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","pointer-speed.txt");
 static int percent=Read();static double windowsGain=1;
 static int Read(){try{int n;return File.Exists(PathName)&&int.TryParse(File.ReadAllText(PathName),out n)?Math.Max(50,Math.Min(400,n)):100;}catch{return 100;}}
 public static int Percent{get{return percent;}set{percent=Math.Max(50,Math.Min(400,value));Directory.CreateDirectory(Path.GetDirectoryName(PathName));File.WriteAllText(PathName,percent.ToString());}}
 public static double Scale{get{return windowsGain*percent/100.0;}}
 public static void RefreshWindowsSpeed(){int speed;if(SystemParametersInfo(0x70,0,out speed,0)){double[] gains={0.03125,0.0625,0.125,0.25,0.375,0.5,0.625,0.75,0.875,1,1.25,1.5,1.75,2,2.25,2.5,2.75,3,3.25,3.5};windowsGain=gains[Math.Max(1,Math.Min(20,speed))-1];}}
}
}
