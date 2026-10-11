using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using EvaClipper2;

class PanelChecks {
 static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
 [STAThread]static void Main(){Application.EnableVisualStyles();PaintBounds();Reversals();Console.WriteLine("PASS: translated/clipped toggle painting, opaque repaint, continuous reversals, duplicate targets, irregular frame timing");}
 static void PaintBounds(){
  using(var toggle=new EvaToggle{Size=new Size(300,42),Text="Длинная подпись переключателя",Checked=true,Font=new Font("Segoe UI",10)})
  using(var bitmap=new Bitmap(480,200))using(var graphics=Graphics.FromImage(bitmap)){
   graphics.Clear(Color.Magenta);graphics.TranslateTransform(35,85);graphics.SetClip(new Rectangle(0,0,300,42));
   typeof(EvaToggle).GetMethod("OnPaint",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(toggle,new object[]{new PaintEventArgs(graphics,toggle.ClientRectangle)});
   int changed=0;
   for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++){
    bool inside=x>=35&&x<335&&y>=85&&y<127;int pixel=bitmap.GetPixel(x,y).ToArgb();
    if(!inside)Assert(pixel==Color.Magenta.ToArgb(),"Text leaked outside its translated control");
    else if(pixel!=EvaTheme.Ink.ToArgb())changed++;
   }
   Assert(changed>100,"Translated control has no label or switch");
   Assert(bitmap.GetPixel(40,88).ToArgb()==EvaTheme.Ink.ToArgb(),"Toggle copied a stale parent background");
   graphics.ResetTransform();graphics.ResetClip();graphics.TranslateTransform(35,85);graphics.SetClip(new Rectangle(0,0,300,42));
   toggle.Text="";toggle.Checked=false;
   typeof(EvaToggle).GetMethod("OnPaint",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(toggle,new object[]{new PaintEventArgs(graphics,toggle.ClientRectangle)});
   for(int y=85;y<127;y++)for(int x=35;x<240;x++)Assert(bitmap.GetPixel(x,y).ToArgb()==EvaTheme.Ink.ToArgb(),"Old label survived a repaint");
  }
 }
 static void Reversals(){
  var m=new PanelMotion();m.Reset(0);m.Aim(true);
  for(int i=0;i<100;i++){
   m.Step(i%3==0?.031:.006);double position=m.Position,velocity=m.Velocity;
   m.Aim(i%2==0);Assert(m.Position==position&&m.Velocity==velocity,"Reversal jumped or reset velocity");
   m.Aim(i%2==0);Assert(m.Position==position&&m.Velocity==velocity,"Duplicate target reset movement");
   Assert(m.Position>=0&&m.Position<=1,"Motion left panel bounds");
  }
  m.Aim(true);for(int i=0;i<120&&!m.Settled;i++)m.Step(.008);
  Assert(m.Settled&&m.Position==1,"Opening did not finish");m.Aim(false);m.Step(2);
  Assert(m.Settled&&m.Position==0,"Delayed frame left an animation running");
 }
}
