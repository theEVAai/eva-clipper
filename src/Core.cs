using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;

namespace EvaClipper2 {
public static class J {
 public static JavaScriptSerializer Serializer {get{return new JavaScriptSerializer{MaxJsonLength=16000000};}}
 public static string Encode(object x){return Serializer.Serialize(x);}
 public static Dictionary<string,object> Parse(string x){return Serializer.Deserialize<Dictionary<string,object>>(x);}
 public static Dictionary<string,object> Obj(params object[] kv){var d=new Dictionary<string,object>();for(int i=0;i<kv.Length;i+=2)d[(string)kv[i]]=kv[i+1];return d;}
 public static Dictionary<string,object> D(object x){return (Dictionary<string,object>)x;}
 public static IEnumerable<Dictionary<string,object>> Rows(object x){return ((System.Collections.IEnumerable)x).Cast<object>().Select(D);}
 public static string S(Dictionary<string,object> d,string k,string fallback=""){return d.ContainsKey(k)&&d[k]!=null?Convert.ToString(d[k],CultureInfo.InvariantCulture):fallback;}
 public static bool B(Dictionary<string,object>d,string k){return d.ContainsKey(k)&&Convert.ToBoolean(d[k]);}
 public static double N(Dictionary<string,object>d,string k){return d.ContainsKey(k)?Convert.ToDouble(d[k],CultureInfo.InvariantCulture):0;}
 public static void Write(string path,object x){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,Encode(x),new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
}
public class AppTrack {public string Window="";public string Name="";public int Volume=100;}
public class Config {
 public int Schema=3,SecondaryFps=30,SecondaryBitrate=8,SecondaryHeight=1080;
 public bool MultiCapture=true,ReplayOnLaunch=false,PanelAnimation=true,BorderlessWindows=true;
 public CaptureSlot[] Sources=CaptureSlot.Defaults();
 public int Minutes=3,Fps=60,Width=1920,Height=1080,Bitrate=24,SystemVolume=100,MicVolume=100;
 public string Capture="desktop",Window="",Monitor="",Codec="h264",AudioMode="split",SystemDevice="default",MicDevice="default";
 public bool MicMuted=true,IncludeSystem=true,AutoDelete=false;
 public int DiskLimitGB=50,MaxAgeDays=0;
 public int[] Hotkeys={262232,262264,327801,262265,262256,393293};
 public string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),"EVA Clips");
 public AppTrack[] Apps={new AppTrack(),new AppTrack(),new AppTrack(),new AppTrack()};
 public static readonly string Data=Environment.GetEnvironmentVariable("EVA_CLIPPER_DATA")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EVA Clipper 2");
 public static readonly string SettingsPath=Path.Combine(Data,"settings.json");
 public static Config Load(){Directory.CreateDirectory(Data);try{string raw=File.ReadAllText(SettingsPath);var c=J.Serializer.Deserialize<Config>(raw)??new Config();if(!J.Parse(raw).ContainsKey("Schema")){c.Schema=3;c.MultiCapture=true;c.AudioMode="multi";}if(c.Sources==null||c.Sources.Length!=4||c.Sources.Any(x=>x==null))c.Sources=CaptureSlot.Defaults();if(c.Apps==null||c.Apps.Length!=4||c.Apps.Any(x=>x==null))c.Apps=new[]{new AppTrack(),new AppTrack(),new AppTrack(),new AppTrack()};if(c.Hotkeys==null||c.Hotkeys.Length!=6)c.Hotkeys=new Config().Hotkeys;return c;}catch{return new Config();}}
 public void Save(){J.Write(SettingsPath,this);}
 public Config Clone(){return J.Serializer.Deserialize<Config>(J.Encode(this));}
 public int Tracks {get{if(MultiCapture)return 63;if(AudioMode=="mix")return 1;if(AudioMode=="split")return 3;int m=IncludeSystem?3:2;for(int i=0;i<4;i++)if(!string.IsNullOrEmpty(Apps[i].Window))m|=1<<(i+2);return m;}}
 public int TotalMemoryMB {get{return MemoryMB+(MultiCapture?Sources.Where(x=>x.Enabled).Sum(x=>(int)Math.Ceiling(Minutes*60.0*((x.Quality=="main"?Bitrate:SecondaryBitrate)*1000+192*6)/8000*1.1+64)):0);}}
 public int MemoryMB {get{return (int)Math.Ceiling(Minutes*60.0*(Bitrate*1000+192*6)/8000*1.2+128);}}
 public void Validate(){
  if(Minutes<1||Minutes>60||!(new[]{30,60,120}).Contains(Fps)||Bitrate<5||Bitrate>100||Width<640||Width>3840||Height<360||Height>2160)throw new Exception("Проверьте настройки видео.");
  if(Codec!="h264"&&Codec!="hevc")throw new Exception("Неизвестный кодек.");
  if(!MultiCapture&&Capture!="desktop"&&string.IsNullOrEmpty(Window))throw new Exception("Выберите игру или окно для захвата.");
  if(Apps==null||Apps.Length!=4||Apps.Any(a=>a==null))throw new Exception("Проверьте аудиоисточники.");
  if(!MultiCapture&&AudioMode=="multi"&&Apps.Where(a=>!string.IsNullOrEmpty(a.Window)).GroupBy(a=>a.Window).Any(g=>g.Count()>1))throw new Exception("Одно приложение выбрано несколько раз.");
  var keys=new[]{"game","discord","browser","extra"};
  if(Sources==null||Sources.Length!=4||Sources.Where((x,i)=>x==null||x.Key!=keys[i]||(x.Quality!="main"&&x.Quality!="secondary")).Any())throw new Exception("Проверьте список источников.");
  if(SystemVolume<0||SystemVolume>200||MicVolume<0||MicVolume>200||Sources.Any(x=>x.Volume<0||x.Volume>200)||Apps.Any(x=>x.Volume<0||x.Volume>200))throw new Exception("Громкость должна быть от 0 до 200%.");
  if(!new[]{30,60}.Contains(SecondaryFps)||!new[]{720,1080}.Contains(SecondaryHeight)||SecondaryBitrate<3||SecondaryBitrate>30)throw new Exception("Проверьте качество дополнительных окон.");
  if(MultiCapture&&Sources.Where(x=>x.Enabled).Any(x=>Minutes*60.0*((x.Quality=="main"?Bitrate:SecondaryBitrate)+1.152)/8>9500))throw new Exception("Этот повтор превышает 9,5 ГБ на источник. Уменьшите длительность или битрейт.");
  if(string.IsNullOrWhiteSpace(Folder)||Folder.Contains('\n')||Folder.Contains('\r'))throw new Exception("Неверный путь.");
  Folder=Path.GetFullPath(Folder);Directory.CreateDirectory(Folder);
  if(AutoDelete&&(DiskLimitGB<1||MaxAgeDays<0))throw new Exception("Проверьте лимит хранения.");
 }
}
public class Choice {public string Name,Value;public override string ToString(){return Name;}}
public class Clip {
 public string Path,Kind,CreatedUtc,Thumbnail;public string GroupId="",SourceLabel="";public bool Keep;public double Duration;public long Bytes;
}
public class Library {
 public List<Clip> Clips=new List<Clip>();
 readonly string file=System.IO.Path.Combine(Config.Data,"library.json");
 readonly object gate=new object();
 public Library(){try{Clips=J.Serializer.Deserialize<List<Clip>>(File.ReadAllText(file))??new List<Clip>();}catch{}}
 public void Register(string path,string kind,string group="",string source=""){path=System.IO.Path.GetFullPath(path);if(!File.Exists(path))return;lock(gate){if(!Clips.Any(x=>string.Equals(x.Path,path,StringComparison.OrdinalIgnoreCase)))Clips.Insert(0,new Clip{Path=path,Kind=kind,GroupId=group,SourceLabel=source,CreatedUtc=DateTime.UtcNow.ToString("o"),Bytes=new FileInfo(path).Length});Save();}}
 public void Save(){lock(gate)J.Write(file,Clips);}
 public List<Clip> Snapshot(){lock(gate)return Clips.ToList();}
 public List<Clip> Existing(){lock(gate)return Clips.Where(c=>File.Exists(c.Path)).OrderByDescending(c=>c.CreatedUtc).ToList();}
 public List<string> Retain(Config c){
  var removed=new List<string>();if(!c.AutoDelete)return removed;
  lock(gate){
   var groups=Clips.Where(x=>File.Exists(x.Path)).OrderBy(x=>x.CreatedUtc).GroupBy(x=>string.IsNullOrEmpty(x.GroupId)?x.Path:x.GroupId).Select(g=>g.ToList()).ToList();
   long total=groups.SelectMany(g=>g).Sum(x=>new FileInfo(x.Path).Length);var newest=groups.LastOrDefault();
   foreach(var group in groups){
    if(group==newest||group.Any(x=>x.Keep))continue;
    DateTime created;bool aged=c.MaxAgeDays>0&&DateTime.TryParse(group.Last().CreatedUtc,null,DateTimeStyles.RoundtripKind,out created)&&created<DateTime.UtcNow.AddDays(-c.MaxAgeDays);
    if(total<=c.DiskLimitGB*1024L*1024*1024&&!aged)continue;
    foreach(var x in group)try{var fi=new FileInfo(x.Path);if((fi.Attributes&FileAttributes.ReparsePoint)!=0)continue;long bytes=fi.Length;File.Delete(x.Path);total-=bytes;removed.Add(x.Path);Clips.Remove(x);}catch(IOException){}catch(UnauthorizedAccessException){}
   }
   Save();
  }return removed;
 }
}

public sealed class EncoderJob : IDisposable {
    [StructLayout(LayoutKind.Sequential)] struct BasicLimits { public long ProcessTime,JobTime; public uint Flags; public UIntPtr MinWorkingSet,MaxWorkingSet; public uint ActiveProcessLimit; public UIntPtr Affinity; public uint Priority,Scheduling; }
    [StructLayout(LayoutKind.Sequential)] struct IoCounters { public ulong ReadOps,WriteOps,OtherOps,ReadBytes,WriteBytes,OtherBytes; }
    [StructLayout(LayoutKind.Sequential)] struct ExtendedLimits { public BasicLimits Basic; public IoCounters Io; public UIntPtr ProcessMemory,JobMemory,PeakProcessMemory,PeakJobMemory; }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr CreateJobObject(IntPtr attr,string name);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetInformationJobObject(IntPtr job,int cls,ref ExtendedLimits value,uint size);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    IntPtr handle;
    public EncoderJob(){handle=CreateJobObject(IntPtr.Zero,null);var limits=new ExtendedLimits();limits.Basic.Flags=0x2000;if(handle==IntPtr.Zero||!SetInformationJobObject(handle,9,ref limits,(uint)Marshal.SizeOf(limits))){Dispose();throw new System.ComponentModel.Win32Exception();}}
    public void Attach(Process p){if(!AssignProcessToJobObject(handle,p.Handle))throw new System.ComponentModel.Win32Exception();}
    public void Dispose(){if(handle!=IntPtr.Zero){CloseHandle(handle);handle=IntPtr.Zero;}}
}


public sealed partial class Engine : IDisposable {
 public ObsRpc Rpc;public Config Applied;public bool Ready;public bool Recording,Replaying;public double RecordSeconds;public DateTime ReplaySince;
 Process backend;EncoderJob job;int port;string password;readonly string root;
 public readonly Library Library;
 public Engine(string path,Library library){root=path;Library=library;}
 public string ObsExe {get{return Path.Combine(root,"engine","bin","64bit","obs64.exe");}}
 string ObsConfig {get{return Path.Combine(root,"engine","config","obs-studio");}}
 static string Q(string x){return "\""+x.Replace("\"","")+"\"";}
 public async Task Initialize(Config c){if(Recording||Replaying)throw new Exception("Перед изменением качества остановите запись и буфер.");c.Validate();await StopBackend();Ready=false;LastNotice="";Applied=c.Clone();Prepare(c);backend=Process.Start(new ProcessStartInfo(ObsExe,"--portable --multi --minimize-to-tray --disable-updater --disable-shutdown-check --profile EVA --collection EVA --websocket_ipv4_only"){WorkingDirectory=Path.GetDirectoryName(ObsExe),UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});job=new EncoderJob();job.Attach(backend);Rpc=new ObsRpc(port,password);Exception last=null;for(int i=0;i<35;i++){if(backend.HasExited)throw new Exception("Ядро записи не запустилось. Проверьте журнал в папке engine.");try{await Rpc.Call("GetVersion");last=null;break;}catch(Exception ex){last=ex;await Task.Delay(500);}}if(last!=null)throw new Exception("Не удалось подключиться к ядру записи.",last);await SetupSources(c);if(c.MultiCapture)await SetupCompanions(c);Ready=true;}
 void Prepare(Config c){
  string profile=Path.Combine(ObsConfig,"basic","profiles","EVA");Directory.CreateDirectory(profile);Directory.CreateDirectory(Path.Combine(ObsConfig,"basic","scenes"));Directory.CreateDirectory(Path.Combine(ObsConfig,"plugin_config","obs-websocket"));
  var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();byte[] secret=new byte[36];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(secret);password=Convert.ToBase64String(secret);
  J.Write(Path.Combine(ObsConfig,"plugin_config","obs-websocket","config.json"),J.Obj("server_enabled",true,"server_port",port,"server_password",password,"auth_required",true,"alerts_enabled",false,"first_load",false));
  string global="[General]\nFirstRun=true\nEnableAutoUpdates=false\n[Basic]\nProfile=EVA\nProfileDir=EVA\nSceneCollection=EVA\nSceneCollectionFile=EVA\n[BasicWindow]\nPreviewEnabled=false\nSysTrayEnabled=true\nSysTrayWhenStarted=true\n";File.WriteAllText(Path.Combine(ObsConfig,"global.ini"),global);File.WriteAllText(Path.Combine(ObsConfig,"user.ini"),global);
  string ini="[General]\nName=EVA\n[Video]\nBaseCX="+c.Width+"\nBaseCY="+c.Height+"\nOutputCX="+c.Width+"\nOutputCY="+c.Height+"\nFPSType=1\nFPSInt="+c.Fps+"\n[Audio]\nSampleRate=48000\nChannelSetup=Stereo\n[Output]\nMode=Advanced\nFilenameFormatting=EVA_%CCYY-%MM-%DD_%hh-%mm-%ss\n[AdvOut]\nRecType=Standard\nRecFilePath="+c.Folder.Replace('\\','/')+"\nRecFormat2=hybrid_mp4\nRecTracks="+c.Tracks+"\nRecEncoder=obs_nvenc_"+c.Codec+"_tex\nRecUseRescale=false\nRecRB=true\nRecRBTime="+(c.Minutes*60)+"\nRecRBSize="+c.MemoryMB+"\n";
  for(int i=1;i<=6;i++){string title=i==1?(!c.MultiCapture&&c.AudioMode=="mix"?"Mixed":"System"):i==2?"Microphone":(c.MultiCapture?new[]{"Game","Discord","Browser","Extra"}[i-3]:(!string.IsNullOrEmpty(c.Apps[i-3].Name)?c.Apps[i-3].Name:"Application "+(i-2)));title=title.Replace('\n',' ').Replace('\r',' ');ini+="Track"+i+"Bitrate=192\nTrack"+i+"Name="+title+"\n";}
  File.WriteAllText(Path.Combine(profile,"basic.ini"),ini,new UTF8Encoding(false));J.Write(Path.Combine(profile,"recordEncoder.json"),J.Obj("rate_control","CBR","bitrate",c.Bitrate*1000,"preset","p5","tune","hq","multipass","qres","profile",c.Codec=="h264"?"high":"main","keyint_sec",2,"bf",2));
  J.Write(Path.Combine(ObsConfig,"basic","scenes","EVA.json"),J.Obj("name","EVA","current_scene","EVA","current_program_scene","EVA","scene_order",new[]{J.Obj("name","EVA")},"sources",new[]{J.Obj("name","EVA","id","scene","versioned_id","scene","settings",J.Obj("items",new object[0]))}));
 }
 async Task Add(string name,string kind,object settings,bool enabled=true){await Rpc.Call("CreateInput",J.Obj("sceneName","EVA","inputName",name,"inputKind",kind,"inputSettings",settings,"sceneItemEnabled",enabled));}
 async Task Tracks(string name,int track){var map=J.Obj();for(int i=1;i<=6;i++)map[i.ToString()]=i==track;await Rpc.Call("SetInputAudioTracks",J.Obj("inputName",name,"inputAudioTracks",map));}
 public async Task<List<Choice>> Choices(string input,string property){var d=await Rpc.Call("GetInputPropertiesListPropertyItems",J.Obj("inputName",input,"propertyName",property));return J.Rows(d["propertyItems"]).Where(x=>J.B(x,"itemEnabled")).Select(x=>new Choice{Name=J.S(x,"itemName"),Value=J.S(x,"itemValue")}).ToList();}
 async Task SetupSources(Config c){
  await Add("Screen","monitor_capture",J.Obj("monitor_id",c.Monitor,"capture_cursor",true,"method",1),c.MultiCapture||c.Capture=="desktop");
  if(string.IsNullOrEmpty(c.Monitor)){var displays=await Choices("Screen","monitor_id");var first=displays.FirstOrDefault();if(first!=null){c.Monitor=Applied.Monitor=first.Value;await Rpc.Call("SetInputSettings",J.Obj("inputName","Screen","inputSettings",J.Obj("monitor_id",first.Value)));}}
  bool singleWindow=!c.MultiCapture&&c.Capture=="window";
  var singleTarget=singleWindow?WindowCatalog.Resolve(c.Window,await Task.Run(()=>WindowCatalog.Read()),IntPtr.Zero):null;
  singleWindowHandle=singleTarget==null?IntPtr.Zero:singleTarget.Handle;lastSourcesPoll=DateTime.MinValue;
  await Add("Window",WindowCapture.Kind(c.BorderlessWindows),WindowCapture.Settings(singleTarget,c.Fps,c.BorderlessWindows),singleWindow);
  await Add("Game","game_capture",J.Obj("capture_mode","window","window",c.Window,"capture_cursor",true),!c.MultiCapture&&c.Capture=="game");
  await Add("System","wasapi_output_capture",J.Obj("device_id",c.SystemDevice));await Add("Mic","wasapi_input_capture",J.Obj("device_id",c.MicDevice));
  await Tracks("System",1);await Tracks("Mic",!c.MultiCapture&&c.AudioMode=="mix"?1:2);
  await Rpc.Call("SetInputMute",J.Obj("inputName","System","inputMuted",(c.MultiCapture||c.AudioMode=="multi")&&!c.IncludeSystem));await Rpc.Call("SetInputMute",J.Obj("inputName","Mic","inputMuted",c.MicMuted));
  await Volume("System",c.SystemVolume);await Volume("Mic",c.MicVolume);
  for(int i=0;i<4;i++){var a=c.Apps[i];await Add("App"+(i+1),"wasapi_process_output_capture",J.Obj("window",a.Window,"priority",2),!c.MultiCapture&&c.AudioMode=="multi"&&!string.IsNullOrEmpty(a.Window));await Tracks("App"+(i+1),i+3);await Volume("App"+(i+1),a.Volume);}
  foreach(var item in J.Rows((await Rpc.Call("GetSceneItemList",J.Obj("sceneName","EVA")))["sceneItems"]))if(new[]{"Screen","Window","Game"}.Contains(J.S(item,"sourceName")))await Rpc.Call("SetSceneItemTransform",J.Obj("sceneName","EVA","sceneItemId",item["sceneItemId"],"sceneItemTransform",J.Obj("boundsType","OBS_BOUNDS_SCALE_INNER","boundsWidth",c.Width,"boundsHeight",c.Height,"positionX",0,"positionY",0)));
 }
 public async Task Volume(string input,int value){await Rpc.Call("SetInputVolume",J.Obj("inputName",input,"inputVolumeMul",value/100.0));}
 public async Task Mic(bool muted){await Rpc.Call("SetInputMute",J.Obj("inputName","Mic","inputMuted",muted));Applied.MicMuted=muted;}
 public async Task Poll(){if(!Ready)return;await PollCompanions();bool wasRecording=Recording,wasReplaying=Replaying;var r=await Rpc.Call("GetRecordStatus");Recording=J.B(r,"outputActive");RecordSeconds=J.N(r,"outputDuration")/1000;Replaying=J.B(await Rpc.Call("GetReplayBufferStatus"),"outputActive");if(wasRecording&&!Recording)throw new Exception("Запись остановилась неожиданно. Проверьте место на диске и журнал ядра; уже записанный MP4 находится в папке сохранения.");if(wasReplaying&&!Replaying)throw new Exception("Мгновенный повтор остановился неожиданно. Проверьте журнал ядра.");}
 public async Task<string> Record(){await Poll();if(Applied.MultiCapture)return await RecordMoment();if(!Recording){CheckDisk();await Rpc.Call("StartRecord");await WaitOutput("GetRecordStatus",true);Recording=true;return null;}var d=await Rpc.Call("StopRecord");Recording=false;string path=J.S(d,"outputPath");Library.Register(path,"Запись");return path;}
 public async Task Replay(){await Poll();if(Replaying){await Rpc.Call("StopReplayBuffer");await WaitOutput("GetReplayBufferStatus",false);Replaying=false;await StopCompanionReplay();}else{CheckDisk();if(Applied.MemoryMB>16000)throw new Exception("Буфер требует больше 16 ГБ RAM. Уменьшите длительность или битрейт.");await Rpc.Call("StartReplayBuffer");await WaitOutput("GetReplayBufferStatus",true);Replaying=true;ReplaySince=DateTime.UtcNow;await StartCompanionReplay();}}
 public async Task<string> SaveReplay(){await Poll();if(Applied.MultiCapture)return await SaveMoment();if(!Replaying)throw new Exception("Сначала включите мгновенный повтор.");string prior="";try{prior=J.S(await Rpc.Call("GetLastReplayBufferReplay"),"savedReplayPath");}catch{}await Rpc.Call("SaveReplayBuffer");for(int i=0;i<150;i++){await Task.Delay(200);string p="";try{p=J.S(await Rpc.Call("GetLastReplayBufferReplay"),"savedReplayPath");}catch{}if(p!=prior&&File.Exists(p)){Library.Register(p,"Повтор");return p;}}throw new Exception("Сохранение повтора ещё не завершено. Проверьте папку записей.");}
 public async Task<string> Screenshot(){string path=Path.Combine(Applied.Folder,"EVA_Screenshot_"+DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fff")+".png");await Rpc.Call("SaveSourceScreenshot",J.Obj("sourceName","EVA","imageFormat","png","imageFilePath",path));Library.Register(path,"Снимок");return path;}
 public async Task Preview(string path){await Rpc.Call("SaveSourceScreenshot",J.Obj("sourceName","EVA","imageFormat","png","imageFilePath",path,"imageWidth",640));}
 async Task WaitOutput(string request,bool active){for(int i=0;i<60;i++){if(J.B(await Rpc.Call(request),"outputActive")==active)return;await Task.Delay(100);}throw new Exception("Ядро не подтвердило "+(active?"начало":"остановку")+" записи. Проверьте журнал engine/config/obs-studio/logs.");}
 void CheckDisk(){if(new DriveInfo(Path.GetPathRoot(Applied.Folder)).AvailableFreeSpace<1024L*1024*1024)throw new Exception("Для записи нужен минимум 1 ГБ свободного места.");}
 async Task StopBackend(){Ready=false;await ShutdownCompanions();if(backend!=null&&!backend.HasExited){if(Recording){try{var d=await Rpc.Call("StopRecord");Library.Register(J.S(d,"outputPath"),"Запись");}catch{}}if(Replaying){try{await Rpc.Call("StopReplayBuffer");}catch{}}backend.CloseMainWindow();if(!await Task.Run(()=>backend.WaitForExit(3000)))backend.Kill();await Task.Run(()=>backend.WaitForExit(3000));}if(backend!=null)backend.Dispose();backend=null;if(job!=null){job.Dispose();job=null;}Recording=Replaying=false;if(Rpc!=null)Rpc.Dispose();Rpc=null;}
 public async Task Shutdown(){await StopBackend();}
 public void Dispose(){Shutdown().GetAwaiter().GetResult();}
}
}
