using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace EvaClipper2 {
static class Program {
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int RegisterWindowMessage(string name);
 [DllImport("user32.dll")]static extern bool SendNotifyMessage(IntPtr hwnd,int msg,IntPtr w,IntPtr l);
 public static readonly int ShowMessage=RegisterWindowMessage("EVAClipper2.ShowPanel");
 [STAThread] static void Main(){bool first;using(var mutex=new System.Threading.Mutex(true,"Local\\EVAClipper2",out first)){if(!first){SendNotifyMessage(new IntPtr(0xffff),ShowMessage,IntPtr.Zero,IntPtr.Zero);return;}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Overlay());}}
}
public enum IconKind {None,Settings,Close,Back,Play,Stop,Camera}
public class FlatButton:Button {
 public IconKind Icon;public bool RightArrow;
 protected override void OnPaint(PaintEventArgs e){
  if(Enabled)base.OnPaint(e);else{e.Graphics.Clear(Color.FromArgb(39,39,39));TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Color.FromArgb(145,145,145),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
  e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  var color=Enabled?ForeColor:Color.FromArgb(145,145,145);float x=Width/2f,y=Height/2f;
  using(var pen=new Pen(color,1.8f))using(var brush=new SolidBrush(color)){
   pen.StartCap=pen.EndCap=System.Drawing.Drawing2D.LineCap.Round;
   if(Icon==IconKind.Settings){var points=new PointF[32];for(int i=0;i<32;i++){double a=i*Math.PI/16;float radius=(i%4==0||i%4==3)?9:6.8f;points[i]=new PointF(x+(float)Math.Cos(a)*radius,y+(float)Math.Sin(a)*radius);}e.Graphics.DrawPolygon(pen,points);e.Graphics.DrawEllipse(pen,x-2.5f,y-2.5f,5,5);}
   if(Icon==IconKind.Close){e.Graphics.DrawLine(pen,x-6,y-6,x+6,y+6);e.Graphics.DrawLine(pen,x+6,y-6,x-6,y+6);}
   if(Icon==IconKind.Back){e.Graphics.DrawLines(pen,new[]{new PointF(x+3,y-6),new PointF(x-3,y),new PointF(x+3,y+6)});}
   if(Icon==IconKind.Play)e.Graphics.FillPolygon(brush,new[]{new PointF(x-5,y-8),new PointF(x+7,y),new PointF(x-5,y+8)});
   if(Icon==IconKind.Stop)e.Graphics.FillRectangle(brush,x-6,y-6,12,12);
   if(Icon==IconKind.Camera){e.Graphics.DrawRectangle(pen,x-9,y-5,18,12);e.Graphics.DrawLines(pen,new[]{new PointF(x-5,y-5),new PointF(x-3,y-8),new PointF(x+3,y-8),new PointF(x+5,y-5)});e.Graphics.DrawEllipse(pen,x-3.5f,y-2.5f,7,7);}
   if(RightArrow){float r=Width-21;e.Graphics.DrawLines(pen,new[]{new PointF(r-2,y-5),new PointF(r+3,y),new PointF(r-2,y+5)});}
  }
 }
}
public class Overlay:Form {
 static readonly Color Bg=Color.FromArgb(25,25,25),Surface=Color.FromArgb(39,39,39),Muted=Color.FromArgb(158,158,158),Accent=Color.FromArgb(184,233,108);
 readonly Library library=new Library(); readonly Engine engine; Config config,draft;
 Panel body,header,footer;Label status,title,memoryEstimate;Button back,gear,record,replay,save,mic;NotifyIcon tray;string keyConflict="";
 readonly Timer poll=new Timer{Interval=1500},animation=new Timer{Interval=15}; bool busy,closing,exiting,polling,slidingOut;int targetX;DateTime lastCleanup=DateTime.UtcNow;string page="home",section="Видео";List<Choice> windows,monitors,outputs,inputs,apps;
 [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
 [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
 public Overlay(){config=Config.Load();engine=new Engine(AppDomain.CurrentDomain.BaseDirectory,library);Text="EVA Clipper";BackColor=Bg;ForeColor=Color.White;Font=new Font("Segoe UI",11);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=true;TopMost=true;AutoScaleMode=AutoScaleMode.None;KeyPreview=true;StartPosition=FormStartPosition.Manual;
  var area=Screen.PrimaryScreen.WorkingArea;Bounds=new Rectangle(area.Right-510,area.Top,510,area.Height);MinimumSize=new Size(470,600);
  header=new Panel{Dock=DockStyle.Top,Height=62,BackColor=Color.FromArgb(52,52,52)};Controls.Add(header);
  title=Label("EVA CLIPPER",26,17,300,30,13,FontStyle.Bold);header.Controls.Add(title);
  back=IconButton(IconKind.Back,"Назад",12,12,34,36,()=>{Home();});back.Visible=false;header.Controls.Add(back);
  gear=IconButton(IconKind.Settings,"Настройки",405,12,40,36,async()=>{await Settings();});gear.AccessibleName="Настройки";header.Controls.Add(gear);
  header.Controls.Add(IconButton(IconKind.Close,"Скрыть панель",457,12,36,36,()=>Slide(false)));
  footer=new Panel{Dock=DockStyle.Bottom,Height=77,BackColor=Bg};Controls.Add(footer);status=Label("Запускаем ядро записи…",24,12,455,54,9);status.ForeColor=Muted;footer.Controls.Add(status);
  body=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(0)};Controls.Add(body);body.BringToFront();
  tray=new NotifyIcon{Icon=SystemIcons.Application,Text="EVA Clipper",Visible=true};var menu=new ContextMenuStrip();menu.Items.Add("Открыть панель",null,(s,e)=>Slide(true));menu.Items.Add("Папка записей",null,(s,e)=>Open(config.Folder));menu.Items.Add("Выход",null,async(s,e)=>await Exit());tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>Slide(true);
  Shown+=async(s,e)=>{BindKeys();Home();await Run(async()=>{await engine.Initialize(config);config.Save();Status("Готово · "+config.Width+" × "+config.Height+" · "+config.Fps+" FPS");Home();});poll.Start();};
  poll.Tick+=async(s,e)=>{if(busy||polling||closing||!engine.Ready)return;polling=true;try{await engine.Poll();RefreshState();if(config.AutoDelete&&DateTime.UtcNow-lastCleanup>TimeSpan.FromMinutes(5)){lastCleanup=DateTime.UtcNow;var snapshot=config.Clone();await Task.Run(()=>library.Retain(snapshot));}}catch(Exception ex){Status("Связь с ядром записи: "+ex.Message,true);}finally{polling=false;}};
  animation.Tick+=(s,e)=>{int dx=targetX-Left;if(Math.Abs(dx)<3){Left=targetX;animation.Stop();if(slidingOut)Hide();}else Left+=Math.Sign(dx)*Math.Max(3,Math.Abs(dx)/3);};
  KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){e.Handled=true;Slide(false);}};FormClosing+=(s,e)=>{if(!exiting){e.Cancel=true;Slide(false);}};
 }
 Label Label(string text,int x,int y,int w,int h,float size=10,FontStyle style=FontStyle.Regular){return new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),ForeColor=Color.White,Font=new Font("Segoe UI",size,style)};}
 Button Button(string text,int x,int y,int w,int h,Action action,bool primary=false,bool navigation=false){var b=new FlatButton{Text=text,Location=new Point(x,y),Size=new Size(w,h),FlatStyle=FlatStyle.Flat,BackColor=primary?Accent:Surface,ForeColor=primary?Color.Black:Color.White,Cursor=Cursors.Hand,UseVisualStyleBackColor=false,TabStop=true};b.RightArrow=navigation;if(navigation){b.TextAlign=ContentAlignment.MiddleLeft;b.Padding=new Padding(14,0,0,0);}b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(204,249,133):Color.FromArgb(60,60,60);b.Click+=(s,e)=>BeginInvoke(new Action(()=>{if(!closing)action();}));return b;}

 Button IconButton(IconKind icon,string accessible,int x,int y,int w,int h,Action action,bool primary=false){var b=(FlatButton)Button("",x,y,w,h,action,primary);b.Icon=icon;b.AccessibleName=accessible;return b;}
 void Clear(){foreach(Control c in body.Controls.Cast<Control>().ToArray())c.Dispose();body.Controls.Clear();body.AutoScrollPosition=Point.Empty;}
 void Line(int y){body.Controls.Add(new Panel{BackColor=Color.FromArgb(64,64,64),Location=new Point(24,y),Size=new Size(456,1)});}
 string Key(int i){return new KeysConverter().ConvertToString((Keys)config.Hotkeys[i]);}
 void Home(){page="home";draft=null;Clear();back.Visible=false;title.Text="EVA CLIPPER";title.Left=26;gear.Visible=true;
  body.Controls.Add(Button("Галерея",24,20,456,38,()=>Gallery(),false,true));
  int x=24;var clips=library.Existing().Take(4).ToList();foreach(var clip in clips){var p=new PictureBox{Location=new Point(x,72),Size=new Size(108,66),BackColor=Surface,SizeMode=PictureBoxSizeMode.Zoom,Cursor=Cursors.Hand};p.Click+=(s,e)=>Open(clip.Path);body.Controls.Add(p);var text=Label(clip.Kind,x,142,108,20,8);text.ForeColor=Muted;body.Controls.Add(text);LoadThumb(p,clip);x+=116;}
  if(clips.Count==0){var empty=Label("Здесь появятся записи, повторы и снимки",30,85,445,54,10);empty.ForeColor=Muted;body.Controls.Add(empty);}Line(178);
  body.Controls.Add(Label("Запись",55,201,305,27,12,FontStyle.Bold));body.Controls.Add(Label(Key(1)+" · начать / остановить",55,231,340,24,9));record=IconButton(IconKind.Play,"Начать запись",414,201,66,50,async()=>await Record(),true);body.Controls.Add(record);
  body.Controls.Add(Label("Мгновенный повтор",55,281,335,27,12,FontStyle.Bold));body.Controls.Add(Label(Key(2)+" · вкл. / выкл.",55,313,335,24,9));replay=Button("Вкл.",394,283,86,40,async()=>await Run(async()=>{await engine.Replay();RefreshState();}));body.Controls.Add(replay);
  save=Button("Сохранить последние "+config.Minutes+" мин    ·    "+Key(3),55,349,425,42,async()=>await Save());body.Controls.Add(save);
  body.Controls.Add(Label("Снимок экрана",55,422,325,27,12,FontStyle.Bold));body.Controls.Add(Label(Key(4)+" · сохранить PNG",55,453,340,24,9));body.Controls.Add(IconButton(IconKind.Camera,"Сохранить снимок",414,422,66,48,async()=>await Shot()));Line(501);
  body.Controls.Add(Label("Микрофон",55,527,275,26,12,FontStyle.Bold));body.Controls.Add(Label(Key(5)+" · вкл. / выкл.",55,559,330,25,9));mic=Button("",350,527,130,40,async()=>await ToggleMic());body.Controls.Add(mic);
  var source=Label((config.Capture=="desktop"?"Рабочий стол":"Игра / окно")+"   ·   "+config.Fps+" FPS   ·   "+(config.Codec=="hevc"?"HEVC":"H.264"),55,611,420,30,9);source.ForeColor=Muted;body.Controls.Add(source);
  body.Controls.Add(Button("Настройки записи",55,660,425,42,async()=>await Settings(),false,true));RefreshState();
 }
 static string FindMediaTool(string name){var paths=new List<string>{Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tools",name)};paths.AddRange((Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator).Where(p=>!string.IsNullOrWhiteSpace(p)).Select(p=>Path.Combine(p.Trim('"'),name)));return paths.FirstOrDefault(File.Exists)??"";}
 async void LoadThumb(PictureBox box,Clip clip){try{string path=clip.Path;bool video=!path.EndsWith(".png",StringComparison.OrdinalIgnoreCase);if(video){string key;using(var sha=System.Security.Cryptography.SHA256.Create())key=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(clip.Path))).Replace("-","").Substring(0,24);path=Path.Combine(Config.Data,"thumbnails",key+".jpg");Directory.CreateDirectory(Path.GetDirectoryName(path));await Task.Run(()=>{string ff=FindMediaTool("ffmpeg.exe"),fp=FindMediaTool("ffprobe.exe");if(!File.Exists(path)&&File.Exists(ff))using(var p=Process.Start(new ProcessStartInfo(ff,"-hide_banner -loglevel error -y -ss 0.5 -i \""+clip.Path+"\" -frames:v 1 -vf scale=320:-1 \""+path+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden})){if(!p.WaitForExit(12000))p.Kill();}if(clip.Duration<=0&&File.Exists(fp))using(var p=Process.Start(new ProcessStartInfo(fp,"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \""+clip.Path+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true})){string result=p.StandardOutput.ReadToEnd();double duration;if(double.TryParse(result.Trim(),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out duration))clip.Duration=duration;p.WaitForExit();library.Save();}});}
  if(box.IsDisposed)return;if(File.Exists(path)){using(var im=Image.FromFile(path))box.Image=new Bitmap(im);box.Disposed+=(s,e)=>{if(box.Image!=null)box.Image.Dispose();};}if(video&&clip.Duration>0){var t=TimeSpan.FromSeconds(clip.Duration);var stamp=new Label{Text=t.TotalHours>=1?t.ToString(@"hh\:mm\:ss"):t.ToString(@"mm\:ss"),AutoSize=true,BackColor=Color.FromArgb(35,35,35),ForeColor=Color.White,Font=new Font("Segoe UI",9),Location=new Point(3,3)};box.Controls.Add(stamp);stamp.Click+=(s,e)=>Open(clip.Path);}}catch{}}

 void Gallery(){page="gallery";Clear();back.Visible=true;title.Text="Галерея";title.Left=55;gear.Visible=false;body.Controls.Add(Button("Открыть папку записей",24,20,456,40,()=>Open(config.Folder)));int y=80;foreach(var clip in library.Existing()){var p=new PictureBox{Location=new Point(24,y),Size=new Size(142,86),BackColor=Surface,SizeMode=PictureBoxSizeMode.Zoom,Cursor=Cursors.Hand};p.Click+=(s,e)=>Open(clip.Path);body.Controls.Add(p);LoadThumb(p,clip);body.Controls.Add(Label(clip.Kind+" · "+new FileInfo(clip.Path).Length/1048576+" МБ",180,y,292,24,11,FontStyle.Bold));body.Controls.Add(Label(Path.GetFileName(clip.Path),180,y+29,292,43,9));var keep=new CheckBox{Text="Сохранить от автоудаления",Checked=clip.Keep,ForeColor=Muted,Location=new Point(180,y+69),Size=new Size(293,27)};keep.CheckedChanged+=(s,e)=>{clip.Keep=keep.Checked;library.Save();};body.Controls.Add(keep);y+=116;}if(y==80)body.Controls.Add(Label("Пока нет записей",24,y,440,30));}
 async Task Settings(){await Run(async()=>{if(!engine.Ready)throw new Exception("Ядро ещё запускается.");windows=await engine.Choices("Window","window");monitors=await engine.Choices("Screen","monitor_id");outputs=await engine.Choices("System","device_id");inputs=await engine.Choices("Mic","device_id");apps=await engine.Choices("App1","window");draft=config.Clone();SettingsPage();});}
 void SettingsPage(){page="settings";Clear();back.Visible=true;title.Text="Настройки";title.Left=55;gear.Visible=false;int bx=24;foreach(string name in new[]{"Видео","Аудио","Файлы","Клавиши"}){string n=name;body.Controls.Add(Button(n,bx,18,108,38,()=>{section=n;SettingsPage();},section==name));bx+=116;}int y=80;
  if(section=="Видео"){
   Select("Источник",ref y,new[]{new Choice{Name="Рабочий стол",Value="desktop"},new Choice{Name="Игра / окно",Value="window"}},draft.Capture,v=>{draft.Capture=v;SettingsPage();});
   Select(draft.Capture=="desktop"?"Монитор":"Игра / окно",ref y,draft.Capture=="desktop"?monitors:windows,draft.Capture=="desktop"?draft.Monitor:draft.Window,v=>{if(draft.Capture=="desktop")draft.Monitor=v;else draft.Window=v;});
   Select("Разрешение",ref y,new[]{new Choice{Name="1280 × 720",Value="1280x720"},new Choice{Name="1920 × 1080",Value="1920x1080"},new Choice{Name="2560 × 1440",Value="2560x1440"},new Choice{Name="3840 × 2160",Value="3840x2160"}},draft.Width+"x"+draft.Height,v=>{var a=v.Split('x');draft.Width=int.Parse(a[0]);draft.Height=int.Parse(a[1]);});
   Slider("Частота кадров",ref y,0,2,Array.IndexOf(new[]{30,60,120},draft.Fps),v=>new[]{30,60,120}[v]+" FPS",v=>draft.Fps=new[]{30,60,120}[v]);
   Select("Кодек",ref y,new[]{new Choice{Name="H.264 — широкая совместимость",Value="h264"},new Choice{Name="HEVC (H.265) — компактнее",Value="hevc"}},draft.Codec,v=>draft.Codec=v);
   Slider("Битрейт",ref y,5,100,draft.Bitrate,v=>v+" Мбит/с",v=>{draft.Bitrate=v;UpdateMemory();});
   Slider("Длительность повтора",ref y,1,60,draft.Minutes,v=>v+" мин",v=>{draft.Minutes=v;UpdateMemory();});
   memoryEstimate=Label("",24,y,450,70,9);memoryEstimate.ForeColor=Muted;body.Controls.Add(memoryEstimate);UpdateMemory();y+=82;
  }else if(section=="Аудио"){
   Select("Аудиодорожки",ref y,new[]{new Choice{Name="Одна — система и микрофон вместе",Value="mix"},new Choice{Name="Две — система / микрофон",Value="split"},new Choice{Name="Несколько — отдельные приложения",Value="multi"}},draft.AudioMode,v=>{draft.AudioMode=v;SettingsPage();});
   Select("Системный звук · устройство",ref y,outputs,draft.SystemDevice,v=>draft.SystemDevice=v);
   Slider("Громкость системы",ref y,0,200,draft.SystemVolume,v=>v+" %",v=>draft.SystemVolume=v);
   Select("Микрофон · устройство",ref y,inputs,draft.MicDevice,v=>draft.MicDevice=v);
   Slider("Громкость микрофона",ref y,0,200,draft.MicVolume,v=>v+" %",v=>draft.MicVolume=v);
   Check("Микрофон включён",ref y,!draft.MicMuted,v=>draft.MicMuted=!v);
   if(draft.AudioMode=="multi"){
    Check("Дорожка 1: система целиком (общий микс)",ref y,draft.IncludeSystem,v=>draft.IncludeSystem=v);
    Hint("Дорожка 2 — микрофон. Дорожки 3–6 — приложения. Общий микс содержит их звук тоже: отключите его, если нужны только отдельные источники.",ref y, 70);
    for(int i=0;i<4;i++){int index=i;var choices=new List<Choice>{new Choice{Name="Не записывать",Value=""}};choices.AddRange(apps);Select("Дорожка "+(i+3)+" · приложение",ref y,choices,draft.Apps[i].Window,v=>{draft.Apps[index].Window=v;draft.Apps[index].Name=choices.First(a=>a.Value==v).Name;});Slider("Громкость дорожки "+(i+3),ref y,0,200,draft.Apps[i].Volume,v=>v+" %",v=>draft.Apps[index].Volume=v);}
   }
  }else if(section=="Файлы"){
   body.Controls.Add(Label("Папка сохранения",24,y,440,26,11,FontStyle.Bold));y+=32;var path=new TextBox{Text=draft.Folder,Location=new Point(24,y),Size=new Size(370,30),BackColor=Surface,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};path.TextChanged+=(s,e)=>draft.Folder=path.Text;body.Controls.Add(path);body.Controls.Add(Button("…",404,y-2,76,32,()=>{using(var d=new FolderBrowserDialog{SelectedPath=draft.Folder})if(d.ShowDialog(this)==DialogResult.OK)path.Text=d.SelectedPath;}));y+= 60;
   Check("Автоматически удалять старые записи",ref y,draft.AutoDelete,v=>draft.AutoDelete=v);
   Slider("Лимит библиотеки",ref y,1,500,draft.DiskLimitGB,v=>v+" ГБ",v=>draft.DiskLimitGB=v);
   Slider("Хранить не дольше",ref y,0,90,draft.MaxAgeDays,v=>v==0?"Без срока":v+" дней",v=>draft.MaxAgeDays=v);
   Hint("Удаляются только файлы, созданные EVA Clipper и внесённые в его галерею. Закреплённые файлы и самый новый клип сохраняются. Поэтому лимит может быть превышен.",ref y,90);
  }else{
   string[] names={"Показать / скрыть панель","Начать / остановить запись","Включить / выключить повтор","Сохранить повтор","Снимок экрана","Включить / выключить микрофон"};
   for(int i=0;i<6;i++){int k=i;body.Controls.Add(Label(names[i],24,y,440,25,10,FontStyle.Bold));y+= 30;var b=new TextBox{ReadOnly=true,Text=new KeysConverter().ConvertToString((Keys)draft.Hotkeys[i]),Location=new Point(24,y),Size=new Size(456,30),BackColor=Surface,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};b.KeyDown+=(s,e)=>{e.SuppressKeyPress=true;if(e.KeyCode==Keys.ControlKey||e.KeyCode==Keys.Menu||e.KeyCode==Keys.ShiftKey)return;if(e.Modifiers==Keys.None){Status("Используйте Alt, Ctrl или Shift вместе с клавишей.",true);return;}draft.Hotkeys[k]=(int)e.KeyData;b.Text=new KeysConverter().ConvertToString(e.KeyData);};body.Controls.Add(b);y+=55;}
   Hint("Нажмите сочетание в поле. Если его заняла другая программа, здесь появится предупреждение; кнопки панели продолжат работать.",ref y,70);
  }
  body.Controls.Add(Button("Применить настройки",24,y+8,456,44,async()=>await Apply(),true));body.Controls.Add(new Label{Location=new Point(24,y+ 60),Size=new Size(450,25)});
 }

 void Select(string label,ref int y,IEnumerable<Choice> items,string value,Action<string> changed){body.Controls.Add(Label(label,24,y,456,26,11,FontStyle.Bold));y+=32;var choices=(items??Enumerable.Empty<Choice>()).ToList();if(!choices.Any(x=>x.Value==value)&&!string.IsNullOrEmpty(value))choices.Insert(0,new Choice{Name="Недоступно: "+value,Value=value});var box=new ComboBox{Location=new Point(24,y),Size=new Size(456,32),DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Surface,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,DropDownWidth=650};box.Items.AddRange(choices.Cast<object>().ToArray());box.SelectedItem=choices.FirstOrDefault(x=>x.Value==value);box.SelectedIndexChanged+=(s,e)=>{if(box.SelectedItem!=null){string selected=((Choice)box.SelectedItem).Value;BeginInvoke(new Action(()=>{if(!box.IsDisposed)changed(selected);}));}};body.Controls.Add(box);y+=55;}
 void Slider(string label,ref int y,int min,int max,int value,Func<int,string> format,Action<int> changed){body.Controls.Add(Label(label,24,y,325,26,11,FontStyle.Bold));var output=Label(format(Math.Max(min,Math.Min(max,value))),350,y,130,26,10);output.TextAlign=ContentAlignment.TopRight;output.ForeColor=Accent;body.Controls.Add(output);y+=30;var bar=new TrackBar{Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),TickStyle=TickStyle.None,Location=new Point(18,y),Size=new Size(467,35),SmallChange=1,LargeChange=1,BackColor=Bg};bar.ValueChanged+=(s,e)=>{output.Text=format(bar.Value);changed(bar.Value);};body.Controls.Add(bar);y+=55;}
 void Check(string text,ref int y,bool value,Action<bool> changed){var box=new CheckBox{Text=text,Checked=value,Location=new Point(24,y),Size=new Size(456,38),ForeColor=Color.White};box.CheckedChanged+=(s,e)=>changed(box.Checked);body.Controls.Add(box);y+=50;}
 void Hint(string text,ref int y,int h){var l=Label(text,24,y,450,h,9);l.ForeColor=Muted;body.Controls.Add(l);y+=h+12;}
 async Task Apply(){await Run(async()=>{draft.Validate();if(draft.Hotkeys.Distinct().Count()!=6)throw new Exception("Для каждого действия нужно своё сочетание клавиш.");if(engine.Recording||engine.Replaying)throw new Exception("Остановите запись и мгновенный повтор перед применением настроек.");var next=draft.Clone();Status("Применяем настройки…");await engine.Initialize(next);config=next;config.Save();BindKeys();library.Retain(config);Home();Status("Настройки сохранены");});}
 async Task Record(){await Run(async()=>{var path=await engine.Record();if(path!=null)Saved(path);else Status("Идёт запись · "+Key(1)+" — остановить");RefreshState();});}
 async Task Save(){await Run(async()=>{var path=await engine.SaveReplay();Saved(path);});}
 async Task Shot(){await Run(async()=>{Saved(await engine.Screenshot());});}
 async Task ToggleMic(){await Run(async()=>{config.MicMuted=!config.MicMuted;try{await engine.Mic(config.MicMuted);}catch{config.MicMuted=!config.MicMuted;throw;}config.Save();RefreshState();});}
 void Saved(string path){library.Retain(config);Status("Сохранено: "+Path.GetFileName(path));tray.ShowBalloonTip(2500,"EVA Clipper","Сохранено: "+Path.GetFileName(path),ToolTipIcon.Info);if(page=="home")Home();}
 void RefreshState(){if(page!="home")return;if(record!=null){((FlatButton)record).Icon=engine.Recording?IconKind.Stop:IconKind.Play;record.Invalidate();record.AccessibleName=engine.Recording?"Остановить запись":"Начать запись";record.BackColor=engine.Recording?Color.FromArgb(244,109,109):Accent;record.Enabled=engine.Ready&&!busy;replay.Text=engine.Replaying?"Выкл.":"Вкл.";replay.Enabled=engine.Ready&&!busy;save.Enabled=engine.Ready&&engine.Replaying&&!busy;mic.Text=config.MicMuted?"Выключен":"Включён";mic.ForeColor=config.MicMuted?Muted:Accent;}tray.Text="EVA Clipper"+(engine.Recording?" · запись":"")+(engine.Replaying?" · повтор":"");if(engine.Recording)Status("● Запись  "+TimeSpan.FromSeconds(engine.RecordSeconds).ToString(@"hh\:mm\:ss")+(engine.Replaying?"   ·   Повтор включён":""));}
 async Task Run(Func<Task> action){if(busy||closing)return;busy=true;RefreshState();try{while(polling)await Task.Delay(50);await action();}catch(Exception ex){Directory.CreateDirectory(Config.Data);File.AppendAllText(Path.Combine(Config.Data,"clipper.log"),DateTime.Now+" "+ex+Environment.NewLine);Status(ex.Message,true);tray.ShowBalloonTip(3500,"EVA Clipper",ex.Message,ToolTipIcon.Warning);}finally{busy=false;RefreshState();}}
 void UpdateMemory(){if(memoryEstimate!=null&&!memoryEstimate.IsDisposed&&draft!=null)memoryEstimate.Text="Повтор хранится в RAM: около "+(draft.MemoryMB/1024.0).ToString("0.0")+" ГБ. Параметры применяются после остановки записи и буфера.";}
 void Status(string text,bool error=false){status.Text=string.IsNullOrEmpty(keyConflict)||error?text:keyConflict;status.ForeColor=error||!string.IsNullOrEmpty(keyConflict)?Color.FromArgb(255,154,142):Muted;}
 void BindKeys(){var failed=new List<string>();for(int i=0;i<6;i++){UnregisterHotKey(Handle,i+1);var key=(Keys)config.Hotkeys[i];uint mod=0x4000;if((key&Keys.Alt)!=0)mod|=1;if((key&Keys.Control)!=0)mod|=2;if((key&Keys.Shift)!=0)mod|=4;if(!RegisterHotKey(Handle,i+1,mod,(uint)(key&Keys.KeyCode)))failed.Add(Key(i));}keyConflict=failed.Count>0?"Заняты клавиши: "+string.Join(", ",failed)+". Измените их в разделе «Клавиши».":"";if(failed.Count>0){Status(keyConflict,true);tray.ShowBalloonTip(7000,"EVA Clipper",keyConflict,ToolTipIcon.Warning);File.AppendAllText(Path.Combine(Config.Data,"clipper.log"),DateTime.Now+" "+keyConflict+Environment.NewLine);}}
 protected override void WndProc(ref Message m){if(m.Msg==Program.ShowMessage){Slide(true);return;}if(m.Msg==0x312){Hotkey(m.WParam.ToInt32());return;}base.WndProc(ref m);}
 async void Hotkey(int id){if(id==1)Slide(!Visible||slidingOut);else if(id==2)await Record();else if(id==3)await Run(async()=>{await engine.Replay();RefreshState();});else if(id==4)await Save();else if(id==5)await Shot();else if(id==6)await ToggleMic();}
 public void Slide(bool show){var area=Screen.PrimaryScreen.WorkingArea;Top=area.Top;Height=area.Height;targetX=show?area.Right-Width:area.Right;slidingOut=!show;if(show){if(!Visible){Left=area.Right;Show();}Activate();}animation.Start();}
 static void Open(string path){try{Process.Start(new ProcessStartInfo(path){UseShellExecute=true});}catch{}}
 async Task Exit(){if(closing)return;if(busy){Status("Дождитесь завершения текущего действия.",true);return;}closing=true;poll.Stop();animation.Stop();Status("Сохраняем запись и завершаем работу…");try{await engine.Shutdown();}catch(Exception ex){MessageBox.Show(ex.Message,"Ошибка завершения");}finally{for(int i=1;i<=6;i++)UnregisterHotKey(Handle,i);tray.Visible=false;tray.Dispose();exiting=true;Close();}}
}
}
