using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EvaClipper2 {
public sealed class SourceState {
 public string Name,Detail,Key;public bool Available,Warning,Buffering;
}
public sealed partial class Engine {
 sealed class Companion {
  public CaptureSlot Slot;public string Scene,Input,Filter,Value="",Kind;
  public IntPtr Window;public int SceneItem,AudioItem;
  public bool Available,Minimized,ReplayStarted,RecordStarted,ReplayActive,RecordActive;
  public string Problem="";public DateTime FirstFrameUtc,RecordJoinedUtc;
  public string ReplayOutput {get{return Scene+" - "+Filter;}}
 }
 readonly List<Companion> companions=new List<Companion>();
 IntPtr singleWindowHandle;
 DateTime lastSourcesPoll=DateTime.MinValue;
 string recordFolder="";DateTime recordStartedUtc;
 public string LastNotice="";
 public double EncoderMemoryMB {get{try{if(backend!=null&&!backend.HasExited){backend.Refresh();return backend.WorkingSet64/1048576.0;}}catch{}return 0;}}
 public List<SourceState> SourceStates {get{
  var list=new List<SourceState>{new SourceState{Key="screen",Name=Applied!=null&&(!Applied.MultiCapture&&Applied.Capture!="desktop")?"Основное окно":"Весь экран",Available=Ready,Buffering=Replaying,Detail=Ready?(Recording?(Replaying?"Идёт запись · в буфере":"Идёт запись"):Replaying?"В буфере":"Готов к захвату"):"Запускается"}};
  foreach(var c in companions)list.Add(new SourceState{Key=c.Slot.Key,Name=c.Slot.Name,Available=c.Available,Buffering=c.ReplayActive,Warning=c.Minimized||(c.Available&&!string.IsNullOrEmpty(c.Problem)),Detail=c.Minimized?"Свёрнуто · видео приостановлено":!c.Available?"Ожидание окна":!string.IsNullOrEmpty(c.Problem)?c.Problem:c.ReplayActive?"В буфере · "+(c.Slot.Quality=="main"?Applied.Fps:Math.Min(Applied.Fps,Applied.SecondaryFps))+" FPS":c.RecordActive?"Идёт запись":"Готов к захвату"});return list;
 }}
 async Task SetupCompanions(Config c){
  companions.Clear();lastSourcesPoll=DateTime.MinValue;
  if(!File.Exists(Path.Combine(root,"engine","obs-plugins","64bit","source-record.dll")))throw new IOException("Для записи нескольких окон нужен полный архив EVA Clipper 3 с ядром engine.");
  var mainItems=J.Rows((await Rpc.Call("GetSceneItemList",J.Obj("sceneName","EVA")))["sceneItems"]).ToList();
  for(int i=0;i<c.Sources.Length;i++){
   var slot=c.Sources[i];if(!slot.Enabled)continue;
   var item=new Companion{Slot=slot,Scene="EVA "+slot.Key,Input="Capture "+slot.Key,Filter="EVA Recorder "+slot.Key};
   item.Kind=c.BorderlessWindows&&slot.Key=="game"&&slot.Selector=="auto:game"?"game_capture":WindowCapture.Kind(c.BorderlessWindows);
   item.AudioItem=(int)J.N(mainItems.First(x=>J.S(x,"sourceName")=="App"+(i+1)),"sceneItemId");
   await Rpc.Call("CreateScene",J.Obj("sceneName",item.Scene));
   var captureSettings=item.Kind=="game_capture"?J.Obj("capture_mode","window","window","","capture_cursor",true):WindowCapture.Settings(null,slot.Quality=="main"?c.Fps:Math.Min(c.Fps,c.SecondaryFps),c.BorderlessWindows);
   var created=await Rpc.Call("CreateInput",J.Obj("sceneName",item.Scene,"inputName",item.Input,"inputKind",item.Kind,"inputSettings",captureSettings,"sceneItemEnabled",false));
   item.SceneItem=(int)J.N(created,"sceneItemId");
   // The scene fixes encoder dimensions even if a Discord pop-out or browser window resizes.
   await Rpc.Call("SetSceneItemTransform",J.Obj("sceneName",item.Scene,"sceneItemId",item.SceneItem,"sceneItemTransform",J.Obj("boundsType","OBS_BOUNDS_SCALE_INNER","boundsWidth",c.Width,"boundsHeight",c.Height,"positionX",0,"positionY",0)));
   bool secondary=slot.Quality!="main";int height=secondary?c.SecondaryHeight:c.Height,width=secondary?height*16/9:c.Width;
   await Rpc.Call("CreateSourceFilter",J.Obj("sourceName",item.Scene,"filterName",item.Filter,"filterKind","source_record_filter","filterSettings",J.Obj(
    "record_mode",0,"stream_mode",0,"replay_buffer",false,"replay_duration",c.Minutes*60,"path",c.Folder,"rec_format","hybrid_mp4",
    "filename_formatting",slot.Key,"replay_filename_formatting",slot.Key,"encoder","obs_nvenc_"+c.Codec+"_tex","rate_control","CBR",
    "bitrate",(secondary?c.SecondaryBitrate:c.Bitrate)*1000,"preset","p4","tune","hq","multipass","disabled","profile",c.Codec=="h264"?"high":"main","keyint_sec",2,"bf",2,
    "different_audio",true,"audio_track",-1,"audio_encoder","ffmpeg_aac","audio_bitrate",192,"scale",secondary,"width",width,"height",height,"resolution",width+"x"+height,"frame_rate_divisor",secondary?Math.Max(1,c.Fps/c.SecondaryFps):1)));
   companions.Add(item);
  }
  await RefreshTargets();
 }
 Dictionary<string,object> FilterRequest(Companion c,object settings){return ObsRpc.Request("SetSourceFilterSettings",J.Obj("sourceName",c.Scene,"filterName",c.Filter,"filterSettings",settings));}
 Dictionary<string,object> VendorRequest(Companion c,string action){return ObsRpc.Request("CallVendorRequest",J.Obj("vendorName","source-record","requestType",action,"requestData",J.Obj("source",c.Scene,"filter",c.Filter)));}
 async Task RefreshTargets(){
  var windows=await Task.Run(()=>WindowCatalog.Read());var requests=new List<Dictionary<string,object>>();
  foreach(var c in companions){
   var window=WindowCatalog.Resolve(c.Slot.Selector,windows,c.Window);string value=window==null?"":window.ObsValue;bool available=window!=null&&!window.Minimized;
   if(c.Value!=value||c.Window!=(window==null?IntPtr.Zero:window.Handle)||c.Available!=available||c.Minimized!=(window!=null&&window.Minimized)){
    int index=Array.IndexOf(Applied.Sources,c.Slot);if(index<0)index=Array.FindIndex(Applied.Sources,s=>s.Key==c.Slot.Key);
    if(window!=null&&(value!=c.Value||window.Handle!=c.Window)){
     var settings=c.Kind=="game_capture"?J.Obj("window",value):WindowCapture.Settings(window,c.Slot.Quality=="main"?Applied.Fps:Math.Min(Applied.Fps,Applied.SecondaryFps),Applied.BorderlessWindows);
     requests.Add(ObsRpc.Request("SetInputSettings",J.Obj("inputName",c.Input,"inputSettings",settings)));requests.Add(ObsRpc.Request("SetInputSettings",J.Obj("inputName","App"+(index+1),"inputSettings",J.Obj("window",value,"priority",2))));
    }
    requests.Add(ObsRpc.Request("SetSceneItemEnabled",J.Obj("sceneName",c.Scene,"sceneItemId",c.SceneItem,"sceneItemEnabled",available)));
    requests.Add(ObsRpc.Request("SetSceneItemEnabled",J.Obj("sceneName","EVA","sceneItemId",c.AudioItem,"sceneItemEnabled",window!=null)));
    requests.Add(ObsRpc.Request("SetInputVolume",J.Obj("inputName","App"+(index+1),"inputVolumeMul",c.Slot.Volume/100.0)));
    c.Value=value;c.Window=window==null?IntPtr.Zero:window.Handle;c.Available=available;c.Minimized=window!=null&&window.Minimized;
    if(available&&c.FirstFrameUtc==default(DateTime))c.FirstFrameUtc=DateTime.UtcNow;
   }
  }
  if(requests.Count>0)await Rpc.Batch(requests);
  foreach(var c in companions.Where(c=>c.Available)){
   var settings=J.Obj();
   if(Replaying&&!c.ReplayStarted){settings["replay_buffer"]=true;c.ReplayStarted=true;}
   if(Recording&&!c.RecordStarted&&!string.IsNullOrEmpty(recordFolder)){settings["record_mode"]=1;settings["path"]=recordFolder;settings["filename_formatting"]=c.Slot.Key;c.RecordStarted=true;c.RecordJoinedUtc=DateTime.UtcNow;}
   if(settings.Count>0){c.Problem="";await Rpc.Batch(new[]{FilterRequest(c,settings)});}
  }
 }
 async Task PollCompanions(){
  if(!Applied.MultiCapture&&Applied.Capture=="window"&&WindowCapture.Kind(Applied.BorderlessWindows)=="eva_print_capture"&&DateTime.UtcNow-lastSourcesPoll>=TimeSpan.FromSeconds(3)){
   lastSourcesPoll=DateTime.UtcNow;var target=WindowCatalog.Resolve(Applied.Window,await Task.Run(()=>WindowCatalog.Read()),singleWindowHandle);var handle=target==null?IntPtr.Zero:target.Handle;
   if(handle!=singleWindowHandle){await Rpc.Call("SetInputSettings",J.Obj("inputName","Window","inputSettings",WindowCapture.Settings(target,Applied.Fps,Applied.BorderlessWindows)));singleWindowHandle=handle;}
  }
  if(companions.Count==0||DateTime.UtcNow-lastSourcesPoll<TimeSpan.FromSeconds(3))return;
  lastSourcesPoll=DateTime.UtcNow;await RefreshTargets();
  var outputs=J.Rows((await Rpc.Call("GetOutputList"))["outputs"]).ToList();
  UpdateCompanionOutputs(outputs);
 }
 void UpdateCompanionOutputs(List<Dictionary<string,object>> outputs){
  foreach(var c in companions){
   c.ReplayActive=outputs.Any(o=>J.S(o,"outputName")==c.ReplayOutput&&J.B(o,"outputActive"));
   c.RecordActive=outputs.Any(o=>J.S(o,"outputName")==c.Filter&&J.B(o,"outputActive"));
   bool failed=c.Available&&((c.ReplayStarted&&Replaying&&!c.ReplayActive)||(c.RecordStarted&&Recording&&!c.RecordActive));
   c.Problem=failed?"Выход не активен · проверьте ядро":"";
  }
 }
 async Task StartCompanionReplay(){
  if(companions.Count==0)return;await RefreshTargets();
  var started=companions.Where(c=>c.ReplayStarted).ToList();await WaitCompanions(started,true,true);
 }
 async Task StopCompanionReplay(){
  var started=companions.Where(c=>c.ReplayStarted).ToList();if(started.Count>0){await Rpc.Batch(started.Select(c=>FilterRequest(c,J.Obj("replay_buffer",false))));await WaitCompanions(started,true,false);}
  foreach(var c in companions){c.ReplayStarted=c.ReplayActive=false;c.Problem="";}
 }
 async Task WaitCompanions(List<Companion> items,bool replay,bool active){
  if(items.Count==0)return;
  for(int attempt=0;attempt<60;attempt++){
   var outputs=J.Rows((await Rpc.Call("GetOutputList"))["outputs"]).ToList();bool confirmed=true;
   foreach(var c in items){bool actual=outputs.Any(o=>J.S(o,"outputName")== (replay?c.ReplayOutput:c.Filter)&&J.B(o,"outputActive"));if(replay)c.ReplayActive=actual;else c.RecordActive=actual;if(actual!=active)confirmed=false;}
   if(confirmed)return;await Task.Delay(100);
  }
  throw new IOException("Не все источники подтвердили "+(active?"запуск":"остановку")+". Проверьте состояние источников и журнал ядра.");
 }
 string NewMomentFolder(string kind){string folder=Path.Combine(Applied.Folder,"Моменты",kind+"_"+DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fff")+"_"+Guid.NewGuid().ToString("N").Substring(0,6));Directory.CreateDirectory(folder);return folder;}
 static bool CompleteFile(string path){try{using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){return file.Length>0;}}catch(IOException){return false;}}
 async Task WaitFiles(IEnumerable<string> files){var list=files.ToList();for(int i=0;i<150;i++){if(list.All(CompleteFile))return;await Task.Delay(200);}throw new IOException("Сохранение части источников ещё идёт. Файлы оставлены в папке момента.");}
 async Task<string> RegisterMoment(string folder,string kind,string main,List<Companion> saved,DateTime trigger,bool replay){
  main=Path.GetFullPath(main);string group=Path.GetFileName(folder);var files=new List<string>{main};files.AddRange(saved.Select(c=>Path.Combine(folder,c.Slot.Key+".mp4")));
  await WaitFiles(files);var durations=await Task.WhenAll(files.Select(MediaTools.Duration));double longest=durations.DefaultIfEmpty(0).Max();
  var rows=new List<object>();for(int i=0;i<files.Count;i++){
   string label=i==0?"Весь экран":saved[i-1].Slot.Name;Library.Register(files[i],kind,group,label);
   var clip=Library.Clips.First(c=>string.Equals(c.Path,files[i],StringComparison.OrdinalIgnoreCase));clip.Duration=durations[i];
   // Every output shares a save/stop batch. Align ends approximately; encoder
   // warm-up means request timestamps alone cannot locate a file's first frame.
   object offset=durations.All(d=>d>0)?(object)Math.Max(0,longest-durations[i]):null;
   rows.Add(J.Obj("file",Path.GetFileName(files[i]),"source",label,"durationSeconds",durations[i]>0?(object)durations[i]:null,"timelineOffsetSeconds",offset));
  }
  Library.Save();
  var missing=companions.Where(c=>!saved.Contains(c)||!c.Available).Select(c=>c.Slot.Name+(!c.Available?" — окно недоступно или свёрнуто":" — буфер ещё не начался")).ToArray();
  J.Write(Path.Combine(folder,"moment.json"),J.Obj("schema",1,"kind",kind,"triggerUtc",trigger.ToString("o"),"files",rows,"audioTracks",new[]{"System","Microphone","Game","Discord","Browser","Extra"},"warnings",missing,"alignment","Shared save/stop batch. timelineOffsetSeconds aligns clip ends approximately, not sample-accurately; null means duration metadata was unavailable."));
  LastNotice="Сохранено видео: "+files.Count+(missing.Length>0?" · Не все окна доступны: "+string.Join(", ",missing):" · один момент");return main;
 }
 async Task<string> SaveMoment(){
  if(!Replaying)throw new InvalidOperationException("Сначала включите мгновенный повтор.");CheckDisk();
  var outputs=J.Rows((await Rpc.Call("GetOutputList"))["outputs"]).ToList();
  var native=outputs.FirstOrDefault(o=>J.S(o,"outputKind")=="replay_buffer"&&J.B(o,"outputActive")&&!companions.Any(c=>c.ReplayOutput==J.S(o,"outputName")));
  if(native==null)throw new IOException("Основной буфер не активен.");
  var saved=companions.Where(c=>c.ReplayStarted&&outputs.Any(o=>J.S(o,"outputName")==c.ReplayOutput&&J.B(o,"outputActive"))).ToList();
  string folder=NewMomentFolder("Replay");var prepare=new List<Dictionary<string,object>>{ObsRpc.Request("SetOutputSettings",J.Obj("outputName",J.S(native,"outputName"),"outputSettings",J.Obj("directory",folder,"format","Screen","extension","mp4")))};
  prepare.AddRange(saved.Select(c=>ObsRpc.Request("SetOutputSettings",J.Obj("outputName",c.ReplayOutput,"outputSettings",J.Obj("directory",folder,"format",c.Slot.Key,"extension","mp4")))));
  await Rpc.Batch(prepare);DateTime trigger=DateTime.UtcNow;var save=saved.Select(c=>VendorRequest(c,"replay_buffer_save")).ToList();save.Add(ObsRpc.Request("SaveReplayBuffer"));
  var results=await Rpc.Batch(save);for(int i=0;i<saved.Count;i++){if(!J.B(J.D(results[i]["responseData"]),"success"))throw new IOException("Не удалось сохранить "+saved[i].Slot.Name+". Остальные файлы оставлены в папке момента.");}
  return await RegisterMoment(folder,"Повтор",Path.Combine(folder,"Screen.mp4"),saved,trigger,true);
 }
 async Task<string> RecordMoment(){
  if(!Recording){
   CheckDisk();recordFolder=NewMomentFolder("Record");recordStartedUtc=DateTime.UtcNow;LastNotice="";
   await Rpc.Call("SetRecordDirectory",J.Obj("recordDirectory",recordFolder));
   await Rpc.Call("StartRecord");await WaitOutput("GetRecordStatus",true);Recording=true;
   try{await RefreshTargets();await WaitCompanions(companions.Where(c=>c.RecordStarted).ToList(),false,true);}catch{await RecordMoment();throw;}
   return null;
  }
  var saved=companions.Where(c=>c.RecordStarted).ToList();var stop=saved.Select(c=>FilterRequest(c,J.Obj("record_mode",0))).ToList();stop.Add(ObsRpc.Request("StopRecord"));
  var replies=await Rpc.Batch(stop);string main=J.S(replies.Last(),"outputPath");Recording=false;
  await WaitCompanions(saved,false,false);foreach(var c in saved)c.RecordStarted=c.RecordActive=false;
  await WaitOutput("GetRecordStatus",false);string folder=recordFolder;recordFolder="";
  return await RegisterMoment(folder,"Запись",main,saved,DateTime.UtcNow,false);
 }
 async Task ShutdownCompanions(){
  if(Rpc==null||(companions.Count==0&&string.IsNullOrEmpty(recordFolder)))return;
  try{if(Recording&&!string.IsNullOrEmpty(recordFolder))await RecordMoment();await StopCompanionReplay();}catch(Exception e){File.AppendAllText(Path.Combine(Config.Data,"clipper.log"),DateTime.Now+" Shutdown: "+e.Message+Environment.NewLine);}
  companions.Clear();
 }
}
}
