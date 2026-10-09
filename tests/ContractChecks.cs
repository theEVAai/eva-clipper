using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EvaClipper2;

// No recorder, device, network or production library is opened by these checks.
class ContractChecks {
 static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Main(){
  string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"eva-clipper-check-"+Guid.NewGuid().ToString("N")));
  Environment.SetEnvironmentVariable("EVA_CLIPPER_DATA",root);Directory.CreateDirectory(root);
  try{Migration(root);WindowSelection();Retention(root);SourceRecovery(root);Console.WriteLine("PASS: migration, source validation, window reconnection, grouped retention, source warning recovery");}
  finally{if(root.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(root).StartsWith("eva-clipper-check-"))Directory.Delete(root,true);}
 }
 static void Migration(string root){
  J.Write(Config.SettingsPath,J.Obj("Minutes",13,"MicMuted",true,"AudioMode","mix","Folder",root));
  var migrated=Config.Load();Assert(migrated.MultiCapture&&migrated.Tracks==63&&migrated.Minutes==13&&migrated.MicMuted,"Migration lost user preferences");migrated.Validate();
  migrated.Sources[0].Key="../escape";bool refused=false;try{migrated.Validate();}catch{refused=true;}Assert(refused,"Invalid source filename accepted");
 }
 static void SourceRecovery(string root){
  var engine=new Engine(root,new Library()){Applied=new Config(),Replaying=true};
  var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  var type=typeof(Engine).GetNestedType("Companion",System.Reflection.BindingFlags.NonPublic);
  var item=Activator.CreateInstance(type);Action<string,object> set=(name,value)=>type.GetField(name).SetValue(item,value);
  set("Slot",new CaptureSlot{Key="discord",Name="Discord"});set("Scene","EVA discord");set("Filter","EVA Recorder discord");set("Available",true);set("ReplayStarted",true);
  var list=(System.Collections.IList)typeof(Engine).GetField("companions",flags).GetValue(engine);list.Add(item);
  var update=typeof(Engine).GetMethod("UpdateCompanionOutputs",flags);
  update.Invoke(engine,new object[]{new List<Dictionary<string,object>>()});Assert(engine.SourceStates.Last().Warning,"Inactive expected replay did not warn");
  update.Invoke(engine,new object[]{new List<Dictionary<string,object>>{J.Obj("outputName","EVA discord - EVA Recorder discord","outputActive",true)}});
  Assert(!engine.SourceStates.Last().Warning&&engine.SourceStates.Last().Buffering,"Recovered replay retained stale warning");
  set("Available",false);set("Minimized",true);update.Invoke(engine,new object[]{new List<Dictionary<string,object>>()});
  Assert(engine.SourceStates.Last().Detail.StartsWith("Свёрнуто"),"Minimized window reported an encoder failure");
 }
 static void WindowSelection(){
  var first=new CaptureWindow{Handle=(IntPtr)1,Title="Before: #title",Exe="sample.exe",ClassName="Sample",Width=1280,Height=720};
  string selector=first.ObsValue;Assert(selector.Split(':').Length==3,"OBS selector escaping failed");
  first.Title="New title";var windows=new List<CaptureWindow>{first};Assert(WindowCatalog.Resolve(selector,windows,IntPtr.Zero)==first,"Unique process did not reconnect");
  windows.Add(new CaptureWindow{Handle=(IntPtr)2,Title="Another",Exe="sample.exe",ClassName="Sample"});Assert(WindowCatalog.Resolve(selector,windows,IntPtr.Zero)==null,"Ambiguous manual target silently switched");
  var browser=new CaptureWindow{Handle=(IntPtr)3,Title="Browser",Exe="chrome.exe",Minimized=true};var visible=new CaptureWindow{Handle=(IntPtr)4,Title="Browser",Exe="zen.exe"};windows=new List<CaptureWindow>{browser,visible};Assert(WindowCatalog.Resolve("auto:browser",windows,browser.Handle)==visible,"Minimized browser was preferred");
 }
 static void Retention(string root){
  var library=new Library();library.Clips.Clear();
  Action<string,string,int,bool> add=(name,group,days,keep)=>{string path=Path.Combine(root,name);File.WriteAllBytes(path,new byte[]{1});library.Register(path,"Test",group);var c=library.Clips.First(x=>x.Path==path);c.CreatedUtc=DateTime.UtcNow.AddDays(-days).ToString("o");c.Keep=keep;};
  add("old-a.mp4","old",10,false);add("old-b.mp4","old",10,false);add("pin-a.mp4","pin",9,true);add("pin-b.mp4","pin",9,false);add("new-a.mp4","new",8,false);add("new-b.mp4","new",8,false);
  string unrelated=Path.Combine(root,"untracked.mp4");File.WriteAllText(unrelated,"keep");
  var removed=library.Retain(new Config{AutoDelete=true,MaxAgeDays=1,DiskLimitGB=1});Assert(removed.Count==2&&removed.All(x=>Path.GetFileName(x).StartsWith("old-")),"Retention split or removed a protected group");Assert(File.Exists(unrelated),"Retention touched an untracked file");
 }
}
