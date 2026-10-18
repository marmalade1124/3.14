using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;

namespace PiStudio {
public static class Native {
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    public static void RoundWindow(IntPtr hwnd){
        // Windows 11 owns corner clipping; older Windows versions keep their native window shape.
        try{int rounded=2;DwmSetWindowAttribute(hwnd,33,ref rounded,sizeof(int));}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
    }

    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; public Rectangle Bounds {get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}} }
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
    public delegate bool MonitorCallback(IntPtr monitor,IntPtr dc,ref Rect rect,IntPtr data);
    [DllImport("user32.dll")] public static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,MonitorCallback callback,IntPtr data);
    [DllImport("user32.dll",SetLastError=true)] public static extern bool RegisterHotKey(IntPtr handle,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr handle,int id);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd,System.Text.StringBuilder name,int size);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,IntPtr process);
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint from,uint to,bool attach);
    public static void TestActivate(IntPtr handle) {
        uint current=GetCurrentThreadId(),foreground=GetWindowThreadProcessId(GetForegroundWindow(),IntPtr.Zero);
        bool attached=current!=foreground&&AttachThreadInput(current,foreground,true);
        try{SetForegroundWindow(handle);}finally{if(attached)AttachThreadInput(current,foreground,false);}
        if(GetForegroundWindow()!=handle)throw new InvalidOperationException("Desktop foreground activation was blocked; input test stopped.");
    }
    [StructLayout(LayoutKind.Explicit,Size=40)] public struct Input { [FieldOffset(0)] public uint Type; [FieldOffset(8)]public ushort Key;[FieldOffset(12)]public uint Flags;[FieldOffset(20)]public uint MouseFlags; }
    [DllImport("user32.dll",SetLastError=true)] public static extern uint SendInput(uint count,Input[] inputs,int size);
    public static void TestKey(ushort key,bool up) {var input=new Input{Type=1,Key=key,Flags=up?2u:0u};if(SendInput(1,new[]{input},40)!=1)throw new InvalidOperationException("Input injection unavailable: "+Marshal.GetLastWin32Error());}
    public static void TestMouse(bool down) {var input=new Input {Type=0,MouseFlags=down?2u:4u};if(SendInput(1,new[]{input},40)!=1)throw new InvalidOperationException("Mouse injection unavailable: "+Marshal.GetLastWin32Error());}
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr handle,IntPtr after,int x,int y,int w,int h,uint flags);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint x,uint y,uint data,UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
    [DllImport("dwmapi.dll")] public static extern int DwmFlush();
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll",SetLastError=true)] static extern bool BitBlt(IntPtr dest,int x,int y,int w,int h,IntPtr source,int sx,int sy,uint operation);
    public static void Capture(Bitmap image,Rectangle bounds) {
        IntPtr source=GetDC(IntPtr.Zero);if(source==IntPtr.Zero)throw new InvalidOperationException("Desktop device context is unavailable.");
        try {using(var graphics=Graphics.FromImage(image)) {IntPtr dest=graphics.GetHdc();try{if(!BitBlt(dest,0,0,bounds.Width,bounds.Height,source,bounds.X,bounds.Y,0x40CC0020))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}finally{graphics.ReleaseHdc(dest);}}}
        finally {ReleaseDC(IntPtr.Zero,source);}
    }
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr w,IntPtr l);
    public static List<Rectangle> Monitors() {
        var result=new List<Rectangle>();
        MonitorCallback callback=delegate(IntPtr m,IntPtr d,ref Rect r,IntPtr x) { result.Add(r.Bounds); return true; };
        if(!EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,callback,IntPtr.Zero)||result.Count==0) throw new InvalidOperationException("No desktop monitors available.");
        return result;
    }
    public static bool Hotkey(string text,out uint modifiers,out uint key) {
        modifiers=0x4000; key=0; string[] parts=(text??"").ToUpperInvariant().Split('+');
        if(parts.Length<2) return false;
        for(int i=0;i<parts.Length-1;i++) { switch(parts[i].Trim()) { case "CTRL": modifiers|=2; break; case "SHIFT": modifiers|=4; break; case "ALT": modifiers|=1; break; case "WIN": modifiers|=8; break; default:return false; } }
        string last=parts[parts.Length-1].Trim();
        if(last.Length==1&&last[0]>='A'&&last[0]<='Z') key=last[0];
        else { int n; if(last.StartsWith("F")&&int.TryParse(last.Substring(1),out n)&&n>=1&&n<=24) key=(uint)(111+n); }
        return key!=0;
    }
}
}
