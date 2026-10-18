using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PiStudio {
public sealed class Picker : IDisposable {
    readonly List<Overlay> overlays=new List<Overlay>();
    readonly Action<string> completed;
    readonly string target;
    bool finished;
    public static int LiveCaptures;
    public Picker(Action<string> completed,string target="Base color") { this.completed=completed;this.target=target; }
    public void Start() {
        try {
            // Capture every display before creating any visible picker window.
            foreach(Rectangle bounds in Native.Monitors()) {
                Bitmap image=new Bitmap(bounds.Width,bounds.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try { Native.Capture(image,bounds); }
                catch {image.Dispose();throw;}
                overlays.Add(new Overlay(this,bounds,image)); LiveCaptures++;
            }
            foreach(var overlay in overlays) overlay.Show();
            Native.Point cursor; Native.GetCursorPos(out cursor);
            foreach(var overlay in overlays) if(overlay.ScreenBounds.Contains(cursor.X,cursor.Y)) { overlay.Activate(); overlay.Focus(); }
        } catch { Dispose(); throw; }
    }
    public void Finish(string value) {
        if(finished)return; finished=true;
        Dispose(); completed(value);
    }
    void SelectAtCursor() {Native.Point p;Native.GetCursorPos(out p);foreach(var overlay in overlays)if(overlay.ScreenBounds.Contains(p.X,p.Y)){Finish(overlay.Sample(p.X,p.Y));return;}}
    public void Dispose() { foreach(var overlay in overlays.ToArray()) overlay.Dispose(); overlays.Clear(); }
    sealed class Overlay : Form {
        readonly Picker owner;
        Bitmap capture;
        readonly Timer timer=new Timer();
        public Rectangle ScreenBounds {get;private set;}
        System.Drawing.Point pixel;
        public Overlay(Picker owner,Rectangle bounds,Bitmap image) {
            this.owner=owner; ScreenBounds=bounds; capture=image;
            AutoScaleMode=AutoScaleMode.None; FormBorderStyle=FormBorderStyle.None; StartPosition=FormStartPosition.Manual; Bounds=bounds;
            TopMost=true; ShowInTaskbar=false; KeyPreview=true; Cursor=Cursors.Cross;
            DoubleBuffered=true;
            timer.Interval=16; timer.Tick+=delegate {Native.Point p;Native.GetCursorPos(out p); if(ScreenBounds.Contains(p.X,p.Y)){pixel=new System.Drawing.Point(p.X-ScreenBounds.X,p.Y-ScreenBounds.Y);Invalidate();}}; timer.Start();
            Shown+=delegate {Native.SetWindowPos(Handle,new IntPtr(-1),bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0040);};
            MouseDown+=delegate(object s,MouseEventArgs e) {if(e.Button==MouseButtons.Left)owner.SelectAtCursor();};
            KeyDown+=delegate(object s,KeyEventArgs e) { if(e.KeyCode==Keys.Escape){e.Handled=true;owner.Finish(null);} else if(e.KeyCode==Keys.Enter){owner.SelectAtCursor();} else {int dx=e.KeyCode==Keys.Left?-1:e.KeyCode==Keys.Right?1:0,dy=e.KeyCode==Keys.Up?-1:e.KeyCode==Keys.Down?1:0; if(dx!=0||dy!=0){Native.Point p;Native.GetCursorPos(out p);Native.SetCursorPos(p.X+dx,p.Y+dy);e.Handled=true;}} };
            DisplaySettingsChangedAttach();
            FormClosing+=delegate {if(!owner.finished)owner.Finish(null);};
        }
        void DisplaySettingsChangedAttach() { Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged; }
        public string Sample(int x,int y) {return ColorLogic.Hex(capture.GetPixel(x-ScreenBounds.X,y-ScreenBounds.Y));}
        void DisplayChanged(object sender,EventArgs e) { if(!IsDisposed) BeginInvoke((Action)delegate {owner.Finish(null);}); }
        protected override void OnPaint(PaintEventArgs e) {
            if(capture==null)return;
            var g=e.Graphics; g.DrawImageUnscaled(capture,0,0);
            Native.Point p;Native.GetCursorPos(out p); if(!ScreenBounds.Contains(p.X,p.Y))return;
            int x=Math.Max(0,Math.Min(capture.Width-1,p.X-ScreenBounds.X)), y=Math.Max(0,Math.Min(capture.Height-1,p.Y-ScreenBounds.Y));
            pixel=new System.Drawing.Point(x,y);
            int panelX=Math.Max(0,Math.Min(ClientSize.Width-232,x+26)),panelY=Math.Max(0,Math.Min(ClientSize.Height-236,y+28));
            using(var bg=new SolidBrush(Color.FromArgb(25,27,35))) g.FillRectangle(bg,panelX,panelY,232,236);
            using(var font=new Font("Segoe UI",10,FontStyle.Bold))g.DrawString("PICKING: "+owner.target.ToUpperInvariant(),font,Brushes.White,panelX+12,panelY+208);
            int startX=Math.Max(0,Math.Min(capture.Width-11,x-5)),startY=Math.Max(0,Math.Min(capture.Height-11,y-5));
            g.InterpolationMode=InterpolationMode.NearestNeighbor; g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(capture,new Rectangle(panelX+12,panelY+12,110,110),new Rectangle(startX,startY,11,11),GraphicsUnit.Pixel);
            g.PixelOffsetMode=PixelOffsetMode.Default;
            g.DrawRectangle(Pens.White,panelX+12+(x-startX)*10,panelY+12+(y-startY)*10,10,10);
            using(var swatch=new SolidBrush(capture.GetPixel(x,y)))g.FillRectangle(swatch,panelX+138,panelY+12,80,110);
            using(var font=new Font("Segoe UI",15,FontStyle.Bold))g.DrawString(ColorLogic.Hex(capture.GetPixel(x,y)),font,Brushes.White,panelX+12,panelY+132);
            using(var font=new Font("Segoe UI",9))g.DrawString("Click / Enter to copy · Esc to cancel\nArrow keys move one pixel",font,Brushes.White,panelX+12,panelY+166);
        }
        protected override void Dispose(bool disposing) {
            if(disposing) { timer.Stop();timer.Dispose();Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;if(capture!=null){capture.Dispose();capture=null;LiveCaptures--;} }
            base.Dispose(disposing);
        }
    }
}
}
