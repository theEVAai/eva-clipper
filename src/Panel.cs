using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EvaClipper2 {
public sealed class BufferedPanel:Panel {public BufferedPanel(){DoubleBuffered=true;ResizeRedraw=true;}}
public sealed class Overlay:Form {
 const int W=440,L=20,CW=384;
 static readonly Color Bg=EvaTheme.Ink,Surface=EvaTheme.Surface,Muted=EvaTheme.Muted,Accent=EvaTheme.Blue,TextColor=EvaTheme.Cream,Red=Color.FromArgb(209,72,72);
 readonly Library library=new Library();readonly Engine engine;Config config,draft;
 readonly Dictionary<string,Font> fonts=new Dictionary<string,Font>();
 readonly Dictionary<string,Label> sourceLabels=new Dictionary<string,Label>();
 readonly Timer poll=new Timer{Interval=1500},animation=new Timer{Interval=15};readonly Stopwatch motion=new Stopwatch();
 WheelPanel body;Panel header,footer,tabs;SettingsWheelFilter wheelFilter;EvaHeart heart;Label title,status,resources,replayLabel,memory;Button back,gear,record,replay,save,mic,apply;NotifyIcon tray;
 string page="home",section="Источники",keyConflict="";int viewVersion,galleryPage;bool busy,closing,exiting,polling,requestedVisible=true,animating;double reveal=1,startReveal=1;
 List<Choice> windows=new List<Choice>(),monitors=new List<Choice>(),outputs=new List<Choice>(),inputs=new List<Choice>(),apps=new List<Choice>();
 List<List<Clip>> gallery=new List<List<Clip>>();
 [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
 [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
 protected override bool ShowWithoutActivation{get{return Program.Instance!="main";}}
 public Overlay(bool startEngine=true){
  config=Config.Load();engine=new Engine(AppDomain.CurrentDomain.BaseDirectory,library);
  Text="EVA Clipper";BackColor=Bg;ForeColor=Color.White;Font=Face(10);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=true;TopMost=true;AutoScaleMode=AutoScaleMode.None;KeyPreview=true;DoubleBuffered=true;StartPosition=FormStartPosition.Manual;
  var area=Screen.PrimaryScreen.WorkingArea;Bounds=new Rectangle(area.Right-W,area.Top,W,area.Height);MinimumSize=new Size(W,580);
  header=new BufferedPanel{Dock=DockStyle.Top,Height=64,BackColor=TextColor};Controls.Add(header);
  heart=new EvaHeart{Location=new Point(18,17),Size=new Size(30,30),AccessibleName="EVA"};header.Controls.Add(heart);
  title=TextLabel("EVA CLIPPER",60,13,260,38,16,true);title.ForeColor=Bg;header.Controls.Add(title);
  back=IconButton(IconKind.Back,"Назад",12,14,36,36,()=>Home());back.Visible=false;header.Controls.Add(back);
  gear=IconButton(IconKind.Settings,"Настройки",350,14,36,36,async()=>await Settings());header.Controls.Add(gear);
  header.Controls.Add(IconButton(IconKind.Close,"Скрыть панель",394,14,30,36,()=>Slide(false)));
  foreach(Button b in header.Controls.OfType<Button>()){b.BackColor=TextColor;b.ForeColor=Bg;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(201,189,166);}
  footer=new BufferedPanel{Dock=DockStyle.Bottom,Height=80,BackColor=Bg};Controls.Add(footer);
  status=TextLabel("Запускаем ядро записи…",L,12,CW,40,9);status.ForeColor=Muted;footer.Controls.Add(status);
  resources=TextLabel("",L,55,CW,20,8.5f);resources.ForeColor=Muted;footer.Controls.Add(resources);
  apply=Button("Применить настройки",L,10,CW,42,async()=>await Apply(),true);apply.Visible=false;footer.Controls.Add(apply);
  tabs=new BufferedPanel{Dock=DockStyle.Top,Height=56,BackColor=Bg,Visible=false};Controls.Add(tabs);
  body=new WheelPanel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Bg};Controls.Add(body);body.BringToFront();wheelFilter=new SettingsWheelFilter(body);Application.AddMessageFilter(wheelFilter);
  Controls.SetChildIndex(body,0);Controls.SetChildIndex(tabs,1);Controls.SetChildIndex(footer,2);Controls.SetChildIndex(header,3);
  tray=new NotifyIcon{Icon=SystemIcons.Application,Text="EVA Clipper",Visible=true};var menu=new ContextMenuStrip();menu.Items.Add("Открыть панель",null,(s,e)=>Slide(true));menu.Items.Add("Папка записей",null,(s,e)=>Open(config.Folder));menu.Items.Add("Выход",null,async(s,e)=>await Exit());tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>Slide(true);
  Shown+=async(s,e)=>{BindKeys();Home();if(!startEngine)return;await Run(async()=>{await engine.Initialize(config);config.Save();if(config.ReplayOnLaunch)await engine.Replay();Status(engine.Replaying?"Повтор включён · сохраняй момент по "+Key(3):"Готово · включи повтор по "+Key(2));Home();});poll.Start();};
  poll.Tick+=async(s,e)=>{
   if(busy||polling||closing||!engine.Ready)return;polling=true;
   try{await engine.Poll();if(Visible&&!animating)RefreshState();if(config.AutoDelete&&DateTime.UtcNow-lastCleanup>TimeSpan.FromMinutes(5)){lastCleanup=DateTime.UtcNow;var c=config.Clone();await Task.Run(()=>library.Retain(c));}}
   catch(Exception ex){Status(ex.Message,true);}finally{polling=false;}
  };
  animation.Tick+=(s,e)=>AnimateStep();
  KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){e.Handled=true;if(page=="settings")Home();else Slide(false);}};
  FormClosing+=(s,e)=>{if(!exiting){e.Cancel=true;Slide(false);}};
 }
 DateTime lastCleanup=DateTime.UtcNow;
 Font Face(float size,bool bold=false){string key=size+":"+bold;Font font;if(!fonts.TryGetValue(key,out font)){font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular);fonts[key]=font;}return font;}
 Label TextLabel(string text,int x,int y,int width,int height,float size=10,bool bold=false){return new Label{Text=text,Location=new Point(x,y),Size=new Size(width,height),Font=Face(size,bold),ForeColor=TextColor,AutoEllipsis=true};}
 Button Button(string text,int x,int y,int width,int height,Action action,bool primary=false,bool navigation=false){
  var b=new FlatButton{Text=text,Location=new Point(x,y),Size=new Size(width,height),FlatStyle=FlatStyle.Flat,BackColor=primary?Accent:Surface,ForeColor=primary?Color.White:TextColor,Font=Face(10),Cursor=Cursors.Hand,UseVisualStyleBackColor=false,TabStop=true,RightArrow=navigation};
  if(navigation){b.TextAlign=ContentAlignment.MiddleLeft;b.Padding=new Padding(12,0,0,0);}b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=primary?EvaTheme.BlueHover:EvaTheme.Hover;b.FlatAppearance.MouseDownBackColor=primary?Color.FromArgb(15,36,144):Color.FromArgb(59,59,55);
  b.Click+=(s,e)=>BeginInvoke(new Action(()=>{if(!closing)action();}));return b;
 }
 Button IconButton(IconKind icon,string name,int x,int y,int width,int height,Action action){var b=(FlatButton)Button("",x,y,width,height,action);b.Icon=icon;b.AccessibleName=name;return b;}
 void Clear(){viewVersion++;sourceLabels.Clear();body.SuspendLayout();foreach(Control c in body.Controls.Cast<Control>().ToArray())c.Dispose();body.Controls.Clear();body.AutoScrollPosition=Point.Empty;body.ResumeLayout(false);}
 void Line(int y){body.Controls.Add(new Panel{Location=new Point(L,y),Size=new Size(CW,1),BackColor=Color.FromArgb(59,59,59)});}
 string Key(int i){return new KeysConverter().ConvertToString((Keys)config.Hotkeys[i]);}
 void Home(){
  page="home";draft=null;Clear();PageChrome(false);back.Visible=false;heart.Visible=true;gear.Visible=true;title.Text="EVA CLIPPER";title.Left=60;
  body.Controls.Add(TextLabel("Моменты",L,20,230,26,12,true));body.Controls.Add(Button("Галерея",302,14,102,32,async()=>await Gallery(),false,true));
  var clips=library.Snapshot().Where(c=>File.Exists(c.Path)).GroupBy(c=>string.IsNullOrEmpty(c.GroupId)?c.Path:c.GroupId).Take(3).ToList();
  int x=L;foreach(var group in clips){var clip=group.First();AddThumb(clip,x,56,120,68);var label=TextLabel(group.Count()>1?group.Count()+" видео":clip.Kind,x,130,120,22,9);label.ForeColor=Muted;body.Controls.Add(label);x+=132;}
  if(clips.Count==0){var empty=TextLabel("Сохранённые моменты появятся здесь.\n"+Key(3)+" сохраняет момент, когда повтор включён.",L,65,CW,76,10);empty.ForeColor=Muted;body.Controls.Add(empty);}
  replayLabel=TextLabel("Мгновенный повтор",L,170,CW,27,12,true);body.Controls.Add(replayLabel);
  save=Button("Сохранить момент     ·     "+Key(3),L,205,CW,48,async()=>await Save(),true);save.Font=Face(10,true);body.Controls.Add(save);
  replay=Button("Включить повтор",L,262,CW,38,async()=>await Run(async()=>{await engine.Replay();Status(engine.Replaying?"Повтор включён · "+Key(3)+" сохраняет все источники":"Повтор выключен");}));body.Controls.Add(replay);
  record=Button("Начать запись",L,310,CW,46,async()=>await Record());body.Controls.Add(record);
  body.Controls.Add(TextLabel("Источники",L,382,240,26,12,true));body.Controls.Add(Button("Настроить",296,376,108,32,async()=>{section="Источники";await Settings();}));
  int y=424;var states=engine.SourceStates;if(!engine.Ready&&config.MultiCapture)foreach(var source in config.Sources.Where(c=>c.Enabled))states.Add(new SourceState{Key=source.Key,Name=source.Name,Detail="Ожидание ядра"});
  foreach(var source in states){body.Controls.Add(TextLabel(source.Name,L,y,110,28,10,true));var detail=TextLabel(source.Detail,132,y,272,30,9);detail.TextAlign=ContentAlignment.TopRight;body.Controls.Add(detail);sourceLabels[source.Key]=detail;y+=37;}
  y+=18;body.Controls.Add(TextLabel("Микрофон",L,y,220,26,11,true));var micHint=TextLabel(config.MultiCapture?"Отдельная дорожка 2":"Управление: "+Key(5),L,y+27,248,22,9);micHint.ForeColor=Muted;body.Controls.Add(micHint);mic=Button("",282,y,122,40,async()=>await ToggleMic());body.Controls.Add(mic);y+=78;
  body.Controls.Add(Button("Снимок · "+Key(4),L,y,186,40,async()=>await Shot()));body.Controls.Add(Button("Видео и звук",218,y,186,40,async()=>{section="Видео";await Settings();},false,true));
  body.AutoScrollMinSize=new Size(0,y+62);RefreshState();
 }
 void AddThumb(Clip clip,int x,int y,int width,int height){var box=new PictureBox{Location=new Point(x,y),Size=new Size(width,height),BackColor=Surface,SizeMode=PictureBoxSizeMode.Zoom,Cursor=Cursors.Hand};box.Click+=(s,e)=>Open(clip.Path);box.Disposed+=(s,e)=>{if(box.Image!=null){box.Image.Dispose();box.Image=null;}};body.Controls.Add(box);LoadThumb(box,clip,viewVersion);}
 async void LoadThumb(PictureBox box,Clip clip,int version){try{var path=await MediaTools.Thumbnail(clip,()=>version==viewVersion&&!closing);if(version!=viewVersion||box.IsDisposed||string.IsNullOrEmpty(path))return;using(var image=Image.FromFile(path))box.Image=new Bitmap(image);if(clip.Duration>0){var duration=TimeSpan.FromSeconds(clip.Duration);var stamp=TextLabel(duration.ToString(duration.TotalHours>=1?@"hh\:mm\:ss":@"mm\:ss"),4,4,54,20,9);stamp.BackColor=Surface;box.Controls.Add(stamp);stamp.Click+=(s,e)=>Open(clip.Path);}}catch{}}
 async Task Gallery(){await Run(async()=>{var all=await Task.Run(()=>library.Existing());gallery=all.GroupBy(c=>string.IsNullOrEmpty(c.GroupId)?c.Path:c.GroupId).Select(g=>g.ToList()).ToList();galleryPage=0;GalleryPage();});}
 void GalleryPage(){
  page="gallery";Clear();PageChrome(false);back.Visible=true;heart.Visible=false;gear.Visible=false;title.Text="Галерея";title.Left=58;
  body.Controls.Add(Button("Открыть папку записей",L,18,CW,42,()=>Open(config.Folder),false,true));int y=82;
  foreach(var group in gallery.Skip(galleryPage*8).Take(8)){
   var clip=group.First();AddThumb(clip,L,y,120,76);body.Controls.Add(TextLabel(clip.Kind+(group.Count>1?" · "+group.Count+" видео":""),156,y,248,25,11,true));
   DateTime when;string date=DateTime.TryParse(clip.CreatedUtc,out when)?when.ToLocalTime().ToString("dd MMM · HH:mm"):Path.GetFileName(clip.Path);var meta=TextLabel(date+" · "+(group.Sum(c=>c.Bytes)/1048576)+" МБ",156,y+27,248,24,9);meta.ForeColor=Muted;body.Controls.Add(meta);
   body.Controls.Add(Button(group.Count>1?"Открыть момент":"Открыть запись",156,y+53,248,30,()=>Open(group.Count>1?Path.GetDirectoryName(clip.Path):clip.Path)));
   var keep=new EvaToggle{Text="Защитить от автоудаления",Checked=group.Any(c=>c.Keep),ForeColor=Muted,Location=new Point(L,y+90),Size=new Size(CW,34),Font=Face(9)};keep.CheckedChanged+=(s,e)=>{foreach(var c in group)c.Keep=keep.Checked;library.Save();};body.Controls.Add(keep);y+=146;
  }
  if(gallery.Count==0)body.Controls.Add(TextLabel("Пока нет сохранённых моментов",L,y,CW,48,11));
  int pages=Math.Max(1,(gallery.Count+7)/8);body.Controls.Add(TextLabel((galleryPage+1)+" / "+pages,L,y+4,90,32,10));var previous=Button("Назад",124,y,132,36,()=>{galleryPage--;GalleryPage();});previous.Enabled=galleryPage>0;body.Controls.Add(previous);var next=Button("Дальше",268,y,136,36,()=>{galleryPage++;GalleryPage();});next.Enabled=galleryPage+1<pages;body.Controls.Add(next);body.AutoScrollMinSize=new Size(0,y+64);
 }
 async Task Settings(){await Run(async()=>{
  windows=WindowCatalog.Read().Where(w=>!new[]{"eva clipper.exe","obs64.exe"}.Contains(w.Exe.ToLowerInvariant())).Select(w=>new Choice{Name=w.Display,Value=w.ObsValue}).ToList();
  if(engine.Ready){monitors=await engine.Choices("Screen","monitor_id");outputs=await engine.Choices("System","device_id");inputs=await engine.Choices("Mic","device_id");apps=await engine.Choices("App1","window");}
  else{outputs=inputs=new List<Choice>{new Choice{Name="По умолчанию",Value="default"}};apps=windows;}
  draft=config.Clone();SettingsPage();
 });}
 void SettingsPage(bool preservePosition=false){
  Point priorScroll=new Point(-body.AutoScrollPosition.X,-body.AutoScrollPosition.Y);var priorFocus=body.Controls.Cast<Control>().FirstOrDefault(c=>c.ContainsFocus);string focusName=priorFocus==null?"":priorFocus.AccessibleName;
  page="settings";Clear();PageChrome(true);back.Visible=true;heart.Visible=false;gear.Visible=false;title.Text="Настройки";title.Left=58;int x=12;
  foreach(Control c in tabs.Controls.Cast<Control>().ToArray())c.Dispose();
  foreach(string name in new[]{"Источники","Видео","Аудио","Файлы","Клавиши"}){string tab=name;int width=name=="Источники"?96:name=="Клавиши"?84:72;var b=Button(name,x,12,width,32,()=>{section=tab;SettingsPage();},section==name);b.Font=Face(9);if(section!=name)b.BackColor=Bg;tabs.Controls.Add(b);x+=width+2;}
  int y=16;
  if(section=="Источники"){
   Check("Сохранять окна отдельными видео",ref y,draft.MultiCapture,v=>{draft.MultiCapture=v;SettingsPage(true);});
   Hint("Одна кнопка сохраняет экран и выбранные окна в общую папку момента. Звук приложений и микрофон — на отдельных дорожках.",ref y,65);
   Select("Монитор · весь экран",ref y,monitors,draft.Monitor,v=>draft.Monitor=v);
   Check("Захват окон без жёлтой рамки",ref y,draft.BorderlessWindows,v=>draft.BorderlessWindows=v);
   Hint(WindowCapture.NeedsCompatibility?"Для Discord и браузера используется совместимый захват Windows 10. Если конкретное окно даёт чёрный кадр, выключи этот режим. Игра захватывается отдельным игровым методом.":"Рамка отключается средствами Windows. Другие программы захвата могут включить её снова.",ref y,86);
   if(draft.MultiCapture){
    for(int i=0;i<4;i++){int n=i;var slot=draft.Sources[i];Check(slot.Name+" · отдельное видео",ref y,slot.Enabled,v=>{draft.Sources[n].Enabled=v;SettingsPage(true);});if(!slot.Enabled)continue;
     var list=new List<Choice>();if(i<3)list.Add(new Choice{Name=i==0?"Авто · последняя активная игра":i==1?"Авто · окно Discord / демонстрация":"Авто · активный браузер",Value="auto:"+slot.Key});list.Add(new Choice{Name="Выбрать окно…",Value=""});list.AddRange(windows);
     Select("Окно для захвата",ref y,list,slot.Selector,v=>draft.Sources[n].Selector=v);
     Select("Качество этого источника",ref y,new[]{new Choice{Name="Основное · как у экрана",Value="main"},new Choice{Name="Дополнительное · меньше нагрузка",Value="secondary"}},slot.Quality,v=>draft.Sources[n].Quality=v);
    }
    Hint("Для демки открой её в Discord или вынеси в отдельное окно. Перекрытое окно записывается; свёрнутое — отмечается как недоступное. Браузер записывает видимую вкладку выбранного окна.",ref y,94);
   }else{Select("Основной источник",ref y,new[]{new Choice{Name="Весь экран",Value="desktop"},new Choice{Name="Игра / окно",Value="window"}},draft.Capture,v=>{draft.Capture=v;SettingsPage(true);});if(draft.Capture!="desktop")Select("Окно",ref y,windows,draft.Window,v=>draft.Window=v);}
   Check("Включать повтор при запуске EVA Clipper",ref y,draft.ReplayOnLaunch,v=>draft.ReplayOnLaunch=v);
   Check("Плавное открытие панели",ref y,draft.PanelAnimation,v=>draft.PanelAnimation=v);
  }else if(section=="Видео"){
   Heading("Основное качество",ref y);
   Select("Разрешение",ref y,new[]{new Choice{Name="1280 × 720",Value="1280x720"},new Choice{Name="1920 × 1080",Value="1920x1080"},new Choice{Name="2560 × 1440",Value="2560x1440"},new Choice{Name="3840 × 2160",Value="3840x2160"}},draft.Width+"x"+draft.Height,v=>{var a=v.Split('x');draft.Width=int.Parse(a[0]);draft.Height=int.Parse(a[1]);});
   Slider("Частота кадров",ref y,0,2,Array.IndexOf(new[]{30,60,120},draft.Fps),v=>new[]{30,60,120}[v]+" FPS",v=>draft.Fps=new[]{30,60,120}[v]);
   Select("Кодек",ref y,new[]{new Choice{Name="H.264 · совместимость с монтажом",Value="h264"},new Choice{Name="HEVC (H.265)",Value="hevc"}},draft.Codec,v=>draft.Codec=v);
   Slider("Битрейт",ref y,5,100,draft.Bitrate,v=>v+" Мбит/с",v=>{draft.Bitrate=v;UpdateMemory();});
   if(draft.MultiCapture){Heading("Дополнительное качество",ref y);Select("Разрешение окон",ref y,new[]{new Choice{Name="1920 × 1080",Value="1080"},new Choice{Name="1280 × 720",Value="720"}},draft.SecondaryHeight.ToString(),v=>draft.SecondaryHeight=int.Parse(v));Slider("Частота кадров окон",ref y,0,1,draft.SecondaryFps==60?1:0,v=>new[]{30,60}[v]+" FPS",v=>draft.SecondaryFps=new[]{30,60}[v]);Slider("Битрейт окон",ref y,3,30,draft.SecondaryBitrate,v=>v+" Мбит/с",v=>{draft.SecondaryBitrate=v;UpdateMemory();});Hint("Применяется к источникам с дополнительным качеством. FPS окна не превышает основную частоту кадров.",ref y,55);}
   Heading("Мгновенный повтор",ref y);Slider("Сколько хранить",ref y,1,60,draft.Minutes,v=>v+" мин",v=>{draft.Minutes=v;UpdateMemory();});memory=TextLabel("",L,y,CW,72,9);memory.ForeColor=Muted;body.Controls.Add(memory);UpdateMemory();y+=90;
  }else if(section=="Аудио"){
   if(draft.MultiCapture)Hint("В каждом видео 6 дорожек: 1 — система, 2 — микрофон, 3 — игра, 4 — Discord, 5 — браузер, 6 — дополнительное окно. Пустые источники дают тишину.",ref y,82);
   else Select("Режим дорожек",ref y,new[]{new Choice{Name="Одна · общий микс",Value="mix"},new Choice{Name="Две · система и микрофон",Value="split"},new Choice{Name="Несколько · по приложениям",Value="multi"}},draft.AudioMode,v=>{draft.AudioMode=v;SettingsPage(true);});
   Select("Системный звук · устройство",ref y,outputs,draft.SystemDevice,v=>draft.SystemDevice=v);Slider("Громкость системы",ref y,0,200,draft.SystemVolume,v=>v+" %",v=>draft.SystemVolume=v);
   if(draft.MultiCapture||draft.AudioMode=="multi")Check("Оставлять общий системный микс на дорожке 1",ref y,draft.IncludeSystem,v=>draft.IncludeSystem=v);
   Select("Микрофон · устройство",ref y,inputs,draft.MicDevice,v=>draft.MicDevice=v);Slider("Громкость микрофона",ref y,0,200,draft.MicVolume,v=>v+" %",v=>draft.MicVolume=v);Check("Записывать микрофон",ref y,!draft.MicMuted,v=>draft.MicMuted=!v);
   if(draft.MultiCapture){for(int i=0;i<4;i++){int n=i;if(draft.Sources[i].Enabled)Slider("Дорожка "+(i+3)+" · "+draft.Sources[i].Name,ref y,0,200,draft.Sources[i].Volume,v=>v+" %",v=>draft.Sources[n].Volume=v);}Hint("Система уже содержит звук приложений. При монтаже используй либо общий микс, либо отдельные дорожки, чтобы звук не удваивался.",ref y,74);}
   else if(draft.AudioMode=="multi")for(int i=0;i<4;i++){int n=i;var choices=new List<Choice>{new Choice{Name="Не записывать",Value=""}};choices.AddRange(apps);Select("Дорожка "+(i+3),ref y,choices,draft.Apps[i].Window,v=>{draft.Apps[n].Window=v;draft.Apps[n].Name=choices.First(a=>a.Value==v).Name;});Slider("Громкость",ref y,0,200,draft.Apps[i].Volume,v=>v+" %",v=>draft.Apps[n].Volume=v);}
  }else if(section=="Файлы"){
   Heading("Папка сохранения",ref y);var path=new TextBox{Text=draft.Folder,Location=new Point(L,y+6),Size=new Size(288,30),BackColor=Surface,ForeColor=TextColor,BorderStyle=BorderStyle.None,Font=Face(10)};path.TextChanged+=(s,e)=>draft.Folder=path.Text;body.Controls.Add(path);body.Controls.Add(Button("Выбрать",318,y-2,86,34,()=>{using(var d=new FolderBrowserDialog{SelectedPath=draft.Folder})if(d.ShowDialog(this)==DialogResult.OK)path.Text=d.SelectedPath;}));y+=50;
   Hint("Папка «Моменты» объединяет видео одного сохранения. moment.json содержит список источников и смещения для совмещения записей в монтаже.",ref y,74);
   Check("Автоматически удалять старые записи",ref y,draft.AutoDelete,v=>draft.AutoDelete=v);Slider("Лимит библиотеки",ref y,1,500,draft.DiskLimitGB,v=>v+" ГБ",v=>draft.DiskLimitGB=v);Slider("Срок хранения",ref y,0,90,draft.MaxAgeDays,v=>v==0?"Без срока":v+" дней",v=>draft.MaxAgeDays=v);
   Hint("Очистка затрагивает только собственную библиотеку EVA. Закреплённый момент и самый новый момент защищены целиком. Поэтому лимит может быть превышен.",ref y,82);
  }else{
   string[] labels={"Показать / скрыть панель","Начать / остановить запись","Включить / выключить повтор","Сохранить весь момент","Снимок экрана","Включить / выключить микрофон"};for(int i=0;i<6;i++){int n=i;Heading(labels[i],ref y);var field=new TextBox{ReadOnly=true,Text=new KeysConverter().ConvertToString((Keys)draft.Hotkeys[i]),Location=new Point(L,y),Size=new Size(CW,30),BackColor=Surface,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};field.KeyDown+=(s,e)=>{e.SuppressKeyPress=true;if(e.KeyCode==Keys.ControlKey||e.KeyCode==Keys.Menu||e.KeyCode==Keys.ShiftKey)return;if(e.Modifiers==Keys.None){Status("Добавьте Alt, Ctrl или Shift.",true);return;}draft.Hotkeys[n]=(int)e.KeyData;field.Text=new KeysConverter().ConvertToString(e.KeyData);};body.Controls.Add(field);y+=54;}Hint("Нажми сочетание в поле. Если клавиши заняты другой программой, EVA покажет предупреждение.",ref y,54);
  }
  body.AutoScrollMinSize=new Size(0,y+18);
  if(preservePosition){int version=viewVersion;BeginInvoke(new Action(()=>{if(page!="settings"||version!=viewVersion)return;var next=body.Controls.Cast<Control>().FirstOrDefault(c=>!string.IsNullOrEmpty(focusName)&&c.AccessibleName==focusName);if(next!=null)next.Focus();body.AutoScrollPosition=priorScroll;}));}
 }
 void PageChrome(bool settings){tabs.Visible=settings;apply.Visible=settings;footer.Height=settings?132:80;status.Top=settings?62:12;resources.Top=settings?109:55;if(page=="home"){if(!fonts.ContainsKey("brand"))fonts["brand"]=EvaFonts.Brand();title.Font=fonts["brand"];title.UseCompatibleTextRendering=true;title.Top=12;}else{title.Font=Face(13,true);title.UseCompatibleTextRendering=false;title.Top=20;}}
 void Heading(string text,ref int y){body.Controls.Add(TextLabel(text,L,y,CW,25,10,true));y+=29;}
 void Hint(string text,ref int y,int height){height=TextRenderer.MeasureText(text,Face(9),new Size(CW,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl).Height+4;var label=TextLabel(text,L,y,CW,height,9);label.ForeColor=Muted;body.Controls.Add(label);y+=height+18;}
 void Select(string label,ref int y,IEnumerable<Choice> options,string value,Action<string> changed){Heading(label,ref y);var choices=(options??Enumerable.Empty<Choice>()).ToList();if(!choices.Any(c=>c.Value==value)&&!string.IsNullOrEmpty(value))choices.Insert(0,new Choice{Name="Недоступно · выбери другое окно",Value=value});var box=new EvaSelect{AccessibleName=label,Location=new Point(L,y),Size=new Size(CW,32),DropDownWidth=620,Font=Face(10)};box.Items.AddRange(choices.Cast<object>().ToArray());box.SelectedItem=choices.FirstOrDefault(c=>c.Value==value);box.SelectedIndexChanged+=(s,e)=>{if(box.SelectedItem==null)return;string next=((Choice)box.SelectedItem).Value;BeginInvoke(new Action(()=>{if(!box.IsDisposed)changed(next);}));};body.Controls.Add(box);y+=48;}
 void Slider(string label,ref int y,int min,int max,int value,Func<int,string> format,Action<int> changed){body.Controls.Add(TextLabel(label,L,y,CW-125,24,10,true));var output=TextLabel(format(Math.Max(min,Math.Min(max,value))),L+CW-125,y,125,24,10);output.TextAlign=ContentAlignment.TopRight;body.Controls.Add(output);y+=25;var slider=new EvaSlider{AccessibleName=label,Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),Location=new Point(L,y),Size=new Size(CW,28)};slider.ValueChanged+=(s,e)=>{output.Text=format(slider.Value);changed(slider.Value);};body.Controls.Add(slider);y+=43;}
 void Check(string text,ref int y,bool value,Action<bool> changed){var check=new EvaToggle{Text=text,AccessibleName=text,Checked=value,Location=new Point(L,y),Size=new Size(CW,42),Font=Face(10)};check.CheckedChanged+=(s,e)=>BeginInvoke(new Action(()=>{if(!check.IsDisposed)changed(check.Checked);}));body.Controls.Add(check);y+=50;}
 async Task Apply(){await Run(async()=>{draft.Validate();if(draft.Hotkeys.Distinct().Count()!=6)throw new Exception("Для каждого действия нужны разные клавиши.");if(engine.Recording||engine.Replaying)throw new Exception("Останови запись и повтор перед изменением источников или качества. Сначала можно сохранить текущий момент.");var next=draft.Clone();Status("Применяем настройки…");await engine.Initialize(next);config=next;config.Save();BindKeys();await Task.Run(()=>library.Retain(config));Home();Status("Настройки сохранены");});}
 async Task Record(){await Run(async()=>{var path=await engine.Record();if(path!=null)await Saved(path);else Status("Идёт запись · "+Key(1)+" — остановить");});}
 async Task Save(){await Run(async()=>{Status("Сохраняем момент…");await Saved(await engine.SaveReplay());});}
 async Task Shot(){await Run(async()=>{engine.LastNotice="";await Saved(await engine.Screenshot());});}
 async Task ToggleMic(){await Run(async()=>{bool next=!config.MicMuted;await engine.Mic(next);config.MicMuted=next;config.Save();Status(next?"Микрофон выключен":config.MultiCapture||config.AudioMode!="mix"?"Микрофон включён · отдельная дорожка":"Микрофон включён · общий микс");});}
 async Task Saved(string path){await Task.Run(()=>library.Retain(config));string notice=string.IsNullOrEmpty(engine.LastNotice)?"Сохранено: "+Path.GetFileName(path):engine.LastNotice;Status(notice);tray.ShowBalloonTip(2500,"EVA Clipper",notice,ToolTipIcon.Info);if(page=="home")Home();}
 void RefreshState(){
  if(closing)return;
  if(page=="home"&&record!=null&&!record.IsDisposed){record.Text=engine.Recording?"Остановить запись · "+TimeSpan.FromSeconds(engine.RecordSeconds).ToString(@"mm\:ss"):"Начать запись     ·     "+Key(1);record.BackColor=engine.Recording?Red:Surface;record.ForeColor=engine.Recording?Color.White:TextColor;record.Enabled=engine.Ready&&!busy;record.FlatAppearance.MouseOverBackColor=engine.Recording?Color.FromArgb(184,51,51):EvaTheme.Hover;
   replay.Text=(engine.Replaying?"Выключить повтор":"Включить повтор")+"     ·     "+Key(2);replay.Enabled=engine.Ready&&!busy;save.Enabled=engine.Ready&&engine.Replaying&&!busy;mic.Text=config.MicMuted?"Выключен":"Включён";mic.ForeColor=config.MicMuted?Muted:Color.White;mic.BackColor=config.MicMuted?Surface:Accent;mic.Enabled=engine.Ready&&!busy;
   replayLabel.Text=engine.Replaying?"Повтор включён · "+config.Minutes+" мин":"Мгновенный повтор";
   foreach(var source in engine.SourceStates){Label label;if(sourceLabels.TryGetValue(source.Key,out label)&&!label.IsDisposed){label.Text=source.Detail;label.ForeColor=source.Warning?Color.FromArgb(255,175,147):source.Buffering?TextColor:Muted;}}
  }
  tray.Text="EVA Clipper"+(engine.Recording?" · запись":"")+(engine.Replaying?" · повтор":"");
  resources.Text="RAM ядра: "+(engine.EncoderMemoryMB/1024).ToString("0.00")+" ГБ   ·   "+config.Fps+" FPS   ·   "+(config.Codec=="hevc"?"HEVC":"H.264");
 }
 async Task Run(Func<Task> action){if(busy||closing)return;busy=true;RefreshState();try{while(polling)await Task.Delay(30);await action();}catch(Exception ex){Directory.CreateDirectory(Config.Data);File.AppendAllText(Path.Combine(Config.Data,"clipper.log"),DateTime.Now+" "+ex+Environment.NewLine);Status(ex.Message,true);tray.ShowBalloonTip(3500,"EVA Clipper",ex.Message,ToolTipIcon.Warning);}finally{busy=false;if(Visible)RefreshState();}}
 void UpdateMemory(){if(memory!=null&&!memory.IsDisposed&&draft!=null)memory.Text="Буферы всех включённых источников: примерно "+(draft.TotalMemoryMB/1024.0).ToString("0.0")+" ГБ RAM при заполнении. Закрытые окна не начинают буфер; фактическая память отображается внизу.";}
 void Status(string text,bool error=false){status.Text=string.IsNullOrEmpty(keyConflict)||error?text:keyConflict;status.ForeColor=error||!string.IsNullOrEmpty(keyConflict)?Color.FromArgb(255,175,147):Muted;}
 void BindKeys(){
  if(Program.Instance!="main")return;
  var failed=new List<string>();for(int i=0;i<6;i++){UnregisterHotKey(Handle,i+1);var key=(Keys)config.Hotkeys[i];uint mod=0x4000;if((key&Keys.Alt)!=0)mod|=1;if((key&Keys.Control)!=0)mod|=2;if((key&Keys.Shift)!=0)mod|=4;if(!RegisterHotKey(Handle,i+1,mod,(uint)(key&Keys.KeyCode)))failed.Add(Key(i));}
  keyConflict=failed.Count==0?"":"Заняты клавиши: "+string.Join(", ",failed)+". Измени их в настройках.";
 }
 protected override void WndProc(ref Message m){if(m.Msg==Program.ShowMessage){Slide(true);return;}if(m.Msg==0x312){Hotkey(m.WParam.ToInt32());return;}base.WndProc(ref m);}
 async void Hotkey(int id){if(id==1)Slide(!requestedVisible);else if(id==2)await Record();else if(id==3)await Run(async()=>{await engine.Replay();Status(engine.Replaying?"Повтор включён":"Повтор выключен");});else if(id==4)await Save();else if(id==5)await Shot();else if(id==6)await ToggleMic();}
 // The HWND stays inside the monitor. Revealing a fixed region avoids Windows snapping
 // an off-screen form back into view, and elapsed time makes reversals deterministic.
 public void Slide(bool show){
  if(closing)return;if(requestedVisible==show&&Visible==show&&!animating)return;requestedVisible=show;
  var area=Screen.PrimaryScreen.WorkingArea;SetBounds(area.Right-W,area.Top,W,area.Height);
  if(!config.PanelAnimation){animation.Stop();animating=false;Region=null;reveal=show?1:0;if(show){Show();Activate();RefreshState();}else Hide();return;}
  if(show&&!Visible){reveal=0;SetReveal(0);Show();}startReveal=reveal;motion.Restart();animating=true;animation.Start();if(show)Activate();
 }
 void SetReveal(double progress){int visible=(int)Math.Round(W*Math.Max(0,Math.Min(1,progress)));Region old=Region;Region=new Region(new Rectangle(W-visible,0,visible,Height));if(old!=null)old.Dispose();}
 void AnimateStep(){double time=Math.Min(1,motion.Elapsed.TotalMilliseconds/170.0);double ease=1-Math.Pow(1-time,3);reveal=startReveal+((requestedVisible?1:0)-startReveal)*ease;SetReveal(reveal);if(time<1)return;animation.Stop();animating=false;if(!requestedVisible)Hide();else{var old=Region;Region=null;if(old!=null)old.Dispose();RefreshState();}}
 static void Open(string path){try{Process.Start(new ProcessStartInfo(path){UseShellExecute=true});}catch{}}
 async Task Exit(){if(closing)return;if(busy){Status("Дождись завершения сохранения.",true);return;}closing=true;poll.Stop();animation.Stop();Status("Завершаем запись…");try{while(polling)await Task.Delay(30);await engine.Shutdown();}catch(Exception ex){MessageBox.Show(ex.Message,"EVA Clipper");}finally{for(int i=1;i<=6;i++)UnregisterHotKey(Handle,i);tray.Visible=false;tray.Dispose();exiting=true;Close();}}
 protected override void Dispose(bool disposing){if(disposing){viewVersion++;poll.Dispose();animation.Dispose();if(wheelFilter!=null)Application.RemoveMessageFilter(wheelFilter);}base.Dispose(disposing);if(disposing)foreach(var font in fonts.Values)font.Dispose();}
}
}
