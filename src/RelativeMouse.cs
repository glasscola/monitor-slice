using System;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace MonitorSlice {
// Raw movement remains available at native monitor edges, where GetCursorPos
// deltas can be clamped. Legacy button delivery stays enabled.
sealed class RelativeMouse:NativeWindow,IDisposable {
 [StructLayout(LayoutKind.Sequential)]struct Device{public ushort Page,Usage;public uint Flags;public IntPtr Target;}
 [DllImport("user32.dll",SetLastError=true)]static extern bool RegisterRawInputDevices(Device[] devices,uint count,uint size);
 [DllImport("user32.dll")]static extern uint GetRawInputData(IntPtr handle,uint command,IntPtr data,ref uint size,uint header);
 readonly Action<int,int> move;
 public RelativeMouse(Action<int,int> callback){move=callback;CreateHandle(new CreateParams{Parent=new IntPtr(-3),Caption="Monitor Slice mouse"});if(!RegisterRawInputDevices(new[]{new Device{Page=1,Usage=2,Flags=0x100,Target=Handle}},1,(uint)Marshal.SizeOf(typeof(Device)))){DestroyHandle();throw new System.ComponentModel.Win32Exception();}}
 protected override void WndProc(ref Message m){if(m.Msg==0xFF){uint bytes=0;uint header=(uint)(8+IntPtr.Size*2);if(GetRawInputData(m.LParam,0x10000003,IntPtr.Zero,ref bytes,header)==0&&bytes>=header+24){var data=Marshal.AllocHGlobal((int)bytes);try{if(GetRawInputData(m.LParam,0x10000003,data,ref bytes,header)==bytes&&Marshal.ReadInt32(data)==0){int offset=(int)header;if((Marshal.ReadInt16(data,offset)&1)==0)move(Marshal.ReadInt32(data,offset+12),Marshal.ReadInt32(data,offset+16));}}finally{Marshal.FreeHGlobal(data);}}}base.WndProc(ref m);}
 public void Dispose(){RegisterRawInputDevices(new[]{new Device{Page=1,Usage=2,Flags=1,Target=IntPtr.Zero}},1,(uint)Marshal.SizeOf(typeof(Device)));DestroyHandle();}
}
}
