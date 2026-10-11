using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EvaClipper2 {
// A reversal changes only the target. Position and velocity remain continuous.
public sealed class PanelMotion {
 const double Omega=48;
 public double Position {get;private set;}
 public double Velocity {get;private set;}
 public double Target {get;private set;}
 public bool Settled {get{return Math.Abs(Position-Target)<.0008&&Math.Abs(Velocity)<.04;}}
 public void Reset(double position){Position=Target=Math.Max(0,Math.Min(1,position));Velocity=0;}
 public void Aim(bool visible){Target=visible?1:0;}
 public void Step(double seconds){
  if(seconds<=0)return;
  double change=Position-Target,term=Velocity+Omega*change,decay=Math.Exp(-Omega*seconds);
  Position=Target+(change+term*seconds)*decay;
  Velocity=(Velocity-Omega*term*seconds)*decay;
  if(Position<0||Position>1){Position=Math.Max(0,Math.Min(1,Position));Velocity=0;}
  if(Settled){Position=Target;Velocity=0;}
 }
}

// Wait for the compositor away from the UI thread. At most one frame is queued.
public sealed class CompositionClock:IDisposable {
 [DllImport("dwmapi.dll")]static extern int DwmFlush();
 readonly Control owner;
 readonly AutoResetEvent wake=new AutoResetEvent(false);
 readonly Thread worker;
 volatile bool running,disposed;
 int queued;
 public event EventHandler Tick;
 public CompositionClock(Control owner){this.owner=owner;worker=new Thread(Loop){IsBackground=true,Name="EVA panel frames"};worker.Start();}
 public void Start(){if(disposed)return;running=true;wake.Set();}
 public void Stop(){running=false;}
 void Loop(){
  try{while(!disposed){wake.WaitOne();while(running&&!disposed){
   var wait=Stopwatch.StartNew();int result=DwmFlush();
   if(result!=0||wait.Elapsed.TotalMilliseconds<2)Thread.Sleep(8);
   if(!running||disposed||Interlocked.CompareExchange(ref queued,1,0)!=0)continue;
   try{owner.BeginInvoke(new Action(()=>{Interlocked.Exchange(ref queued,0);if(running&&!disposed&&Tick!=null)Tick(this,EventArgs.Empty);}));}
   catch(InvalidOperationException){Interlocked.Exchange(ref queued,0);running=false;}
  }}}finally{wake.Dispose();}
 }
 public void Dispose(){if(disposed)return;running=false;disposed=true;wake.Set();}
}

// Animate one immutable, opaque image instead of repainting every child HWND.
// The real controls return as soon as the transition finishes.
public sealed class SlidePreview:Form {
 [StructLayout(LayoutKind.Sequential)]struct PointI {public int X,Y;public PointI(int x,int y){X=x;Y=y;}}
 [StructLayout(LayoutKind.Sequential)]struct SizeI {public int Width,Height;public SizeI(int w,int h){Width=w;Height=h;}}
 [StructLayout(LayoutKind.Sequential,Pack=1)]struct Blend {public byte Op,Flags,Alpha,Format;}
 [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr hwnd);
 [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
 [DllImport("gdi32.dll")]static extern IntPtr CreateRectRgn(int left,int top,int right,int bottom);
 [DllImport("user32.dll")]static extern int SetWindowRgn(IntPtr hwnd,IntPtr region,bool redraw);
 [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dstDc,ref PointI dst,ref SizeI size,IntPtr srcDc,ref PointI src,uint key,ref Blend blend,uint flags);
 int lastWidth=-1;
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80000|0x80|0x08000000;return p;}}
 public SlidePreview(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;Text="EVA Clipper animation";}
 public void Prepare(Bitmap bitmap,Rectangle bounds,double progress){
  Bounds=bounds;IntPtr screen=GetDC(IntPtr.Zero),memory=IntPtr.Zero,image=IntPtr.Zero,old=IntPtr.Zero;
  try{
   memory=CreateCompatibleDC(screen);image=bitmap.GetHbitmap();old=SelectObject(memory,image);
   var dst=new PointI(bounds.X,bounds.Y);var src=new PointI(0,0);var size=new SizeI(bounds.Width,bounds.Height);var blend=new Blend{Alpha=255};
   if(!UpdateLayeredWindow(Handle,screen,ref dst,ref size,memory,ref src,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
   Reveal(progress);Show();
  }finally{if(old!=IntPtr.Zero)SelectObject(memory,old);if(image!=IntPtr.Zero)DeleteObject(image);if(memory!=IntPtr.Zero)DeleteDC(memory);if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen);}
 }
 public void Reveal(double progress){
  int width=(int)Math.Round(Width*Math.Max(0,Math.Min(1,progress)));if(width==lastWidth)return;lastWidth=width;
  IntPtr region=CreateRectRgn(Width-width,0,Width,Height);
  if(SetWindowRgn(Handle,region,true)==0){DeleteObject(region);throw new System.ComponentModel.Win32Exception();}
 }
 protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=(IntPtr)3;return;}base.WndProc(ref m);}
}
}
