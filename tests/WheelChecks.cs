using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EvaClipper2;

class WheelChecks {
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 class TestForm:Form {protected override bool ShowWithoutActivation{get{return true;}}}
 [STAThread]static void Main(){Application.EnableVisualStyles();using(var form=new TestForm{Size=new Size(440,500),ShowInTaskbar=false})using(var panel=new WheelPanel{Dock=DockStyle.Fill,AutoScroll=true}){
  form.Controls.Add(panel);var combo=new EvaSelect{Location=new Point(20,30),Width=350};combo.Items.AddRange(new object[]{"One","Two","Three"});combo.SelectedIndex=1;
  var slider=new EvaSlider{Location=new Point(20,100),Width=350,Minimum=0,Maximum=100,Value=45};panel.Controls.Add(combo);panel.Controls.Add(slider);panel.AutoScrollMinSize=new Size(0,1800);
  form.Show();Application.DoEvents();var filter=new SettingsWheelFilter(panel);
  foreach(var control in new Control[]{combo,slider}){
   panel.AutoScrollPosition=Point.Empty;control.Focus();SendMessage(control.Handle,0x20A,new IntPtr(-120L<<16),IntPtr.Zero);
   Assert(combo.SelectedIndex==1&&slider.Value==45,"Direct wheel changed a value");Assert(panel.AutoScrollPosition.Y<0,"Direct wheel did not scroll parent");
   panel.AutoScrollPosition=Point.Empty;var m=Message.Create(control.Handle,0x20A,new IntPtr(-120L<<16),IntPtr.Zero);
   Assert(filter.PreFilterMessage(ref m),"Native wheel message not consumed");Assert(panel.AutoScrollPosition.Y<0,"Filtered wheel did not scroll");
  }
  var foreign=Message.Create(new IntPtr(123),0x20A,new IntPtr(-120L<<16),IntPtr.Zero);Assert(!filter.PreFilterMessage(ref foreign),"Wheel intercepted another window");
  panel.AutoScrollPosition=Point.Empty;panel.ScrollWheel(-60);Assert(panel.AutoScrollPosition.Y==0,"Partial wheel step was not accumulated");panel.ScrollWheel(-60);Assert(panel.AutoScrollPosition.Y<0,"High-resolution wheel lost delta");
  SendMessage(slider.Handle,0x100,(IntPtr)Keys.Right,IntPtr.Zero);Assert(slider.Value==46,"Keyboard adjustment stopped working");
  SendMessage(combo.Handle,0x100,(IntPtr)Keys.Down,IntPtr.Zero);Assert(combo.SelectedIndex==2,"Selector keyboard adjustment stopped working");
  Console.WriteLine("PASS: wheel scroll, value preservation, high-resolution delta, keyboard, foreign-window boundary");
 }}
}
