using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EvaClipper2 {
public static class EvaTheme {
 public static readonly Color Ink=Color.FromArgb(13,13,13),Surface=Color.FromArgb(31,31,30),Hover=Color.FromArgb(45,45,43),Cream=Color.FromArgb(216,205,184),Muted=Color.FromArgb(167,164,154),Blue=Color.FromArgb(26,61,204),BlueHover=Color.FromArgb(36,80,232);
 public const TextFormatFlags TextContext=TextFormatFlags.PreserveGraphicsClipping|TextFormatFlags.PreserveGraphicsTranslateTransform;
}
public static class EvaFonts {
 static readonly System.Drawing.Text.PrivateFontCollection collection=new System.Drawing.Text.PrivateFontCollection();
 static EvaFonts(){using(var stream=typeof(EvaFonts).Assembly.GetManifestResourceStream("EvaBrand.ttf")){if(stream==null)return;byte[] bytes=new byte[stream.Length];stream.Read(bytes,0,bytes.Length);IntPtr data=Marshal.AllocCoTaskMem(bytes.Length);try{Marshal.Copy(bytes,0,data,bytes.Length);collection.AddMemoryFont(data,bytes.Length);}finally{Marshal.FreeCoTaskMem(data);}}}
 public static Font Brand(){return new Font(collection.Families.Length>0?collection.Families[0]:FontFamily.GenericSansSerif,22,FontStyle.Regular);}
}
public sealed class WheelPanel:Panel {
 int remainder;
 public WheelPanel(){DoubleBuffered=true;ResizeRedraw=true;}
 public void ScrollWheel(int delta){
  remainder+=delta;int notches=remainder/120;remainder%=120;if(notches==0)return;
  int lines=SystemInformation.MouseWheelScrollLines;if(lines==0)return;
  int distance=lines<0?ClientSize.Height:Math.Max(1,lines)*22;
  int next=Math.Max(0,Math.Min(Math.Max(0,DisplayRectangle.Height-ClientSize.Height),-AutoScrollPosition.Y-notches*distance));
  AutoScrollPosition=new Point(0,next);
 }
 protected override void OnMouseWheel(MouseEventArgs e){ScrollWheel(e.Delta);var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;}
 public static bool Route(Control child,ref Message m){if(m.Msg!=0x20A&&m.Msg!=0x20E)return false;for(Control p=child.Parent;p!=null;p=p.Parent){var scroll=p as WheelPanel;if(scroll!=null){if(m.Msg==0x20A)scroll.ScrollWheel(unchecked((short)((long)m.WParam>>16)));return true;}}return false;}
}
// Native selectors process the wheel before MouseWheel fires. Filter the message,
// including the selector's separate popup HWND, rather than undoing a changed value.
public sealed class SettingsWheelFilter:IMessageFilter {
 readonly WheelPanel panel;
 public SettingsWheelFilter(WheelPanel panel){this.panel=panel;}
 public bool PreFilterMessage(ref Message m){
  if(m.Msg!=0x20A&&m.Msg!=0x20E||panel.IsDisposed||!panel.Visible)return false;
  Control target=Control.FromChildHandle(m.HWnd);bool inside=target==panel||target!=null&&panel.Contains(target);
  foreach(var box in panel.Controls.OfType<EvaSelect>())if(box.PopupHandle==m.HWnd){inside=true;break;}
  if(!inside)return false;
  foreach(var box in panel.Controls.OfType<EvaSelect>())if(box.DroppedDown)box.DroppedDown=false;
  if(m.Msg==0x20A)panel.ScrollWheel(unchecked((short)((long)m.WParam>>16)));return true;
 }
}
public sealed class EvaSelect:ComboBox {
 [StructLayout(LayoutKind.Sequential)]struct ComboInfo {public int size;public Rectangle item,button;public int state;public IntPtr combo,edit,list;}
 [DllImport("user32.dll")]static extern bool GetComboBoxInfo(IntPtr h,ref ComboInfo info);
 public IntPtr PopupHandle {get{if(!IsHandleCreated||!DroppedDown)return IntPtr.Zero;var i=new ComboInfo{size=Marshal.SizeOf(typeof(ComboInfo))};return GetComboBoxInfo(Handle,ref i)?i.list:IntPtr.Zero;}}
 public EvaSelect(){DropDownStyle=ComboBoxStyle.DropDownList;DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=23;FlatStyle=FlatStyle.Flat;BackColor=EvaTheme.Surface;ForeColor=EvaTheme.Cream;IntegralHeight=false;DropDownHeight=320;}
 protected override void OnDrawItem(DrawItemEventArgs e){if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;using(var b=new SolidBrush(selected?EvaTheme.Blue:EvaTheme.Surface))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,GetItemText(Items[e.Index]),Font,new Rectangle(e.Bounds.X+9,e.Bounds.Y,e.Bounds.Width-12,e.Bounds.Height),selected?Color.White:ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|EvaTheme.TextContext);}
 protected override void WndProc(ref Message m){
  if(WheelPanel.Route(this,ref m)){if(DroppedDown)DroppedDown=false;return;}
  base.WndProc(ref m);
  if(IsHandleCreated&&(m.Msg==0xF||m.Msg==0x317||m.Msg==0x318)){using(var g=m.Msg==0xF?Graphics.FromHwnd(Handle):Graphics.FromHdc(m.WParam)){g.SetClip(ClientRectangle,CombineMode.Intersect);using(var background=new SolidBrush(BackColor))g.FillRectangle(background,ClientRectangle);TextRenderer.DrawText(g,Text,Font,new Rectangle(10,0,Width-42,Height),ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|EvaTheme.TextContext);using(var pen=new Pen(ForeColor,1.5f)){g.SmoothingMode=SmoothingMode.AntiAlias;int x=Width-19,y=Height/2;g.DrawLines(pen,new[]{new Point(x-4,y-2),new Point(x,y+2),new Point(x+4,y-2)});}if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(3,3,Width-7,Height-7),ForeColor,BackColor);}}
 }
}
public sealed class EvaSlider:Control {
 public int Minimum=0,Maximum=100;int value;bool dragging;
 public event EventHandler ValueChanged;
 public int Value {get{return value;}set{int next=Math.Max(Minimum,Math.Min(Maximum,value));if(this.value==next)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);AccessibilityNotifyClients(AccessibleEvents.ValueChange,-1);}}
 public EvaSlider(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Slider;BackColor=EvaTheme.Ink;Height=28;}
 void Pick(int x){Value=Minimum+(int)Math.Round(Math.Max(0,Math.Min(1,(x-8.0)/Math.Max(1,Width-16)))*(Maximum-Minimum));}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;dragging=true;Pick(e.X);}}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)Pick(e.X);}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
 protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture)dragging=false;base.OnMouseCaptureChanged(e);}
 protected override bool IsInputKey(Keys keyData){var k=keyData&Keys.KeyCode;return k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down||base.IsInputKey(keyData);}
 protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down)Value--;else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up)Value++;else if(e.KeyCode==Keys.Home)Value=Minimum;else if(e.KeyCode==Keys.End)Value=Maximum;else return;e.Handled=true;}
 protected override void WndProc(ref Message m){if(WheelPanel.Route(this,ref m))return;base.WndProc(ref m);}
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float x=8+(Width-16)*(Value-Minimum)/(float)Math.Max(1,Maximum-Minimum),y=Height/2f;using(var rail=new Pen(EvaTheme.Hover,4))using(var fill=new Pen(EvaTheme.BlueHover,4))using(var thumb=new SolidBrush(EvaTheme.Cream)){rail.StartCap=rail.EndCap=fill.StartCap=fill.EndCap=LineCap.Round;g.DrawLine(rail,8,y,Width-8,y);if(x>8)g.DrawLine(fill,8,y,x,y);g.FillEllipse(thumb,x-6,y-6,12,12);}if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(1,1,Width-2,Height-2),EvaTheme.Cream,BackColor);}
 protected override AccessibleObject CreateAccessibilityInstance(){return new SliderAccessible(this);}
 sealed class SliderAccessible:ControlAccessibleObject {readonly EvaSlider slider;public SliderAccessible(EvaSlider c):base(c){slider=c;}public override string Value{get{return slider.Value.ToString();}set{int n;if(int.TryParse(value,out n))slider.Value=n;}}}
}
public sealed class EvaToggle:CheckBox {
 public EvaToggle(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque,true);BackColor=EvaTheme.Ink;UseVisualStyleBackColor=false;ForeColor=EvaTheme.Cream;Cursor=Cursors.Hand;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;var state=g.Save();try{g.SetClip(ClientRectangle,CombineMode.Intersect);using(var background=new SolidBrush(BackColor))g.FillRectangle(background,ClientRectangle);g.SmoothingMode=SmoothingMode.AntiAlias;TextRenderer.DrawText(g,Text,Font,new Rectangle(0,0,Width-62,Height),Enabled?ForeColor:EvaTheme.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|EvaTheme.TextContext);float x=Width-44,y=(Height-22)/2f;using(var path=new GraphicsPath()){path.AddArc(x,y,22,22,90,180);path.AddArc(x+20,y,22,22,270,180);path.CloseFigure();using(var b=new SolidBrush(Checked?EvaTheme.Blue:Color.FromArgb(70,69,64)))g.FillPath(b,path);}using(var b=new SolidBrush(Checked?Color.White:EvaTheme.Cream))g.FillEllipse(b,x+(Checked?23:3),y+3,16,16);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(1,1,Width-2,Height-2),ForeColor,BackColor);}finally{g.Restore(state);}}
 protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
}
public sealed class EvaHeart:Control {
 public EvaHeart(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(Width/32f,Height/32f);using(var p=new GraphicsPath()){p.AddBezier(16,27,16,27,3,19,3,11);p.AddBezier(3,11,3,7,6,4,10,4);p.AddBezier(10,4,12.5f,4,14.5f,5.5f,16,7);p.AddBezier(16,7,17.5f,5.5f,19.5f,4,22,4);p.AddBezier(22,4,26,4,29,7,29,11);p.AddBezier(29,11,29,19,16,27,16,27);p.CloseFigure();using(var b=new SolidBrush(Color.FromArgb(237,232,223)))g.FillPath(b,p);var state=g.Save();g.SetClip(p);using(var stripe=new Pen(Color.FromArgb(130,130,130),1.1f))for(int i=-10;i<=26;i+=6)g.DrawLine(stripe,i,32,i+27,0);g.Restore(state);using(var pen=new Pen(EvaTheme.Blue,2.5f)){pen.LineJoin=LineJoin.Round;g.DrawPath(pen,p);}}}
}
}
