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
 public static readonly string Instance=Environment.GetEnvironmentVariable("EVA_CLIPPER_INSTANCE")??"main";
 public static readonly int ShowMessage=RegisterWindowMessage("EVAClipper2.ShowPanel."+Instance);
 [STAThread] static void Main(){bool first;using(var mutex=new System.Threading.Mutex(true,"Local\\EVAClipper2."+Instance,out first)){if(!first){SendNotifyMessage(new IntPtr(0xffff),ShowMessage,IntPtr.Zero,IntPtr.Zero);return;}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Overlay());}}
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
}
