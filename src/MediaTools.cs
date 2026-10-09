using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EvaClipper2 {
public static class MediaTools {
 static readonly SemaphoreSlim workers=new SemaphoreSlim(2,2);
 static readonly Dictionary<string,string> cache=new Dictionary<string,string>();
 public static string Find(string name){lock(cache){string found;if(cache.TryGetValue(name,out found))return found;var paths=new List<string>{Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tools",name)};paths.AddRange((Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator).Where(p=>!string.IsNullOrWhiteSpace(p)).Select(p=>Path.Combine(p.Trim('"'),name)));found=paths.FirstOrDefault(File.Exists)??"";cache[name]=found;return found;}}
 public static string Quote(string value){return "\""+value.Replace("\"","")+"\"";}
 public static async Task<string> Run(string tool,string args,int timeout=12000,Func<bool> wanted=null){
  var exe=Find(tool);if(string.IsNullOrEmpty(exe))return "";
  await workers.WaitAsync().ConfigureAwait(false);
  try{
   if(wanted!=null&&!wanted())return "";
   using(var p=Process.Start(new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true})){
    var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();
    if(!await Task.Run(()=>p.WaitForExit(timeout)).ConfigureAwait(false)){try{p.Kill();}catch{}throw new IOException("Обработка медиа заняла слишком много времени.");}
    string text=await output.ConfigureAwait(false);await error.ConfigureAwait(false);return p.ExitCode==0?text:"";
   }
  }finally{workers.Release();}
 }
 public static async Task<double> Duration(string path){double seconds;string value=await Run("ffprobe.exe","-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "+Quote(path)).ConfigureAwait(false);return double.TryParse(value.Trim(),NumberStyles.Float,CultureInfo.InvariantCulture,out seconds)?seconds:0;}
 public static async Task<string> Thumbnail(Clip clip,Func<bool> wanted){
  if(clip.Path.EndsWith(".png",StringComparison.OrdinalIgnoreCase))return clip.Path;
  string key;using(var sha=System.Security.Cryptography.SHA256.Create())key=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(clip.Path))).Replace("-","").Substring(0,24);
  string path=Path.Combine(Config.Data,"thumbnails",key+".jpg");Directory.CreateDirectory(Path.GetDirectoryName(path));
  if(!File.Exists(path)&&wanted())await Run("ffmpeg.exe","-hide_banner -loglevel error -y -ss 0.5 -i "+Quote(clip.Path)+" -frames:v 1 -vf scale=320:-1 "+Quote(path),12000,wanted).ConfigureAwait(false);
  return File.Exists(path)?path:"";
 }
}
}
