using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace EvaClipper2 {
public sealed class CaptureSlot {
 public string Key="extra", Name="Окно / видео", Selector="", Quality="secondary";
 public bool Enabled;
 public int Volume=100;
 public static CaptureSlot[] Defaults(){return new[]{
  new CaptureSlot{Key="game",Name="Игра",Selector="auto:game",Quality="main",Enabled=true},
  new CaptureSlot{Key="discord",Name="Discord",Selector="auto:discord",Enabled=true},
  new CaptureSlot{Key="browser",Name="Браузер",Selector="auto:browser",Enabled=true},
  new CaptureSlot{Key="extra",Name="Окно / видео"}};}
}

public sealed class CaptureWindow {
 public IntPtr Handle;
 public uint ProcessId;
 public string Title, Exe, ClassName;
 public bool Minimized, Foreground;
 public int Width,Height;
 static string Escape(string s){return s.Replace("#","#22").Replace(":","#3A");}
 public string ObsValue {get{return Escape(Title)+":"+Escape(ClassName)+":"+Escape(Exe);}}
 public string Display {get{return Exe+" · "+Title;}}
}

public static class WindowCatalog {
 delegate bool EnumCallback(IntPtr h,IntPtr p);
 [StructLayout(LayoutKind.Sequential)] struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumCallback callback,IntPtr p);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out Rect rect);
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint id);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder text,int count);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder text,int count);
 static readonly string[] browsers={"zen.exe","chrome.exe","msedge.exe","firefox.exe","brave.exe","opera.exe","vivaldi.exe"};
 static readonly string[] games={"robloxplayerbeta.exe","javaw.exe","java.exe","minecraft.windows.exe","cs2.exe","dota2.exe","valorant-win64-shipping.exe","fortniteclient-win64-shipping.exe","overwatch.exe","r5apex.exe","league of legends.exe","gta5.exe","eldenring.exe"};
 static readonly string[] excluded={"explorer.exe","chatgpt.exe","codex.exe","eva clipper.exe","obs64.exe","devenv.exe","code.exe","rider64.exe","blender.exe","robloxstudiobeta.exe","discord.exe","discordptb.exe","discordcanary.exe","spotify.exe","steam.exe","taskmgr.exe","msmpeng.exe","applicationframehost.exe"};
 static IntPtr lastGame;
 public static List<CaptureWindow> Read(){
  var result=new List<CaptureWindow>();var foreground=GetForegroundWindow();
  EnumWindows((h,p)=>{
   if(!IsWindowVisible(h))return true;
   var title=new StringBuilder(1024);GetWindowText(h,title,title.Capacity);if(title.Length==0)return true;
   uint id;GetWindowThreadProcessId(h,out id);string exe;
   try{using(var process=Process.GetProcessById((int)id))exe=process.ProcessName+".exe";}catch{return true;}
   Rect r;if(!GetWindowRect(h,out r))return true;bool minimized=IsIconic(h);
   if(!minimized&&(r.Right-r.Left<280||r.Bottom-r.Top<160))return true;
   var cls=new StringBuilder(256);GetClassName(h,cls,cls.Capacity);
   result.Add(new CaptureWindow{Handle=h,ProcessId=id,Title=title.ToString(),Exe=exe,ClassName=cls.ToString(),Minimized=minimized,Foreground=h==foreground,Width=r.Right-r.Left,Height=r.Bottom-r.Top});return true;
  },IntPtr.Zero);
  var active=result.FirstOrDefault(w=>w.Foreground);
  if(active!=null&&(games.Contains(active.Exe.ToLowerInvariant())||IsLikelyGame(active)))lastGame=active.Handle;
  return result;
 }
 static bool IsLikelyGame(CaptureWindow w){
  string exe=w.Exe.ToLowerInvariant();
  return !excluded.Contains(exe)&&!browsers.Contains(exe)&&!exe.Contains("launcher")&&!exe.Contains("terminal")&&!exe.Contains("powershell")&&!exe.Contains("cmd.exe")&&!w.ClassName.StartsWith("Chrome_",StringComparison.Ordinal)&&w.Width>=1000&&w.Height>=650;
 }
 public static CaptureWindow Resolve(string selector,List<CaptureWindow> windows,IntPtr prior){
  if(string.IsNullOrEmpty(selector))return null;
  IEnumerable<CaptureWindow> candidates=windows;
  if(selector=="auto:discord")candidates=candidates.Where(w=>w.Exe.StartsWith("Discord",StringComparison.OrdinalIgnoreCase));
  else if(selector=="auto:browser")candidates=candidates.Where(w=>browsers.Contains(w.Exe.ToLowerInvariant()));
  else if(selector=="auto:game")candidates=candidates.Where(w=>games.Contains(w.Exe.ToLowerInvariant())||w.Handle==lastGame);
  else {
   var exact=windows.FirstOrDefault(w=>w.ObsValue==selector);if(exact!=null)return exact;
   var parts=selector.Split(':');if(parts.Length!=3)return null;
   string exe=parts[2].Replace("#3A",":").Replace("#22","#");
   candidates=candidates.Where(w=>w.Exe.Equals(exe,StringComparison.OrdinalIgnoreCase));
   // A changed title may reconnect only when there is a unique window in that process.
   var reconnect=candidates.ToList();return reconnect.Count==1?reconnect[0]:null;
  }
  return candidates.OrderBy(w=>w.Minimized?1:0)
   .ThenByDescending(w=>selector=="auto:discord"&&(w.Title.IndexOf("popout",StringComparison.OrdinalIgnoreCase)>=0||w.Title.IndexOf("stream",StringComparison.OrdinalIgnoreCase)>=0))
   .ThenByDescending(w=>w.Foreground).ThenByDescending(w=>w.Handle==prior).FirstOrDefault();
 }
}
public static class WindowCapture {
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct VersionInfo{public int Size,Major,Minor,Build,Platform;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string ServicePack;}
 [DllImport("ntdll.dll",CharSet=CharSet.Unicode)]static extern int RtlGetVersion(ref VersionInfo version);
 static int Build(){var v=new VersionInfo{Size=Marshal.SizeOf(typeof(VersionInfo))};return RtlGetVersion(ref v)==0?v.Build:0;}
 public static readonly bool NeedsCompatibility=Build()<20348;
 public static string Kind(bool borderless){return borderless&&NeedsCompatibility?"eva_print_capture":"window_capture";}
 public static Dictionary<string,object> Settings(CaptureWindow window,int fps,bool borderless){
  if(Kind(borderless)=="eva_print_capture")return J.Obj("hwnd",window==null?0L:window.Handle.ToInt64(),"process_id",window==null?0:window.ProcessId,"fps",fps);
  return J.Obj("window",window==null?"":window.ObsValue,"method",2,"priority",0,"client_area",true,"cursor",true);
 }
}
}
