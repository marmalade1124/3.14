using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PiStudio {
public sealed class ColorWheel : Control {
    const int Size=220;
    static BitmapSource image;
    double hue,saturation,value=1;
    public event Action Changed;
    public double Hue {get{return hue;}}
    public double Saturation {get{return saturation;}}
    public double Value {get{return value;} set{this.value=Math.Max(0,Math.Min(1,value));Notify();}}
    public string Hex {get{return FromHsv(hue,saturation,value);}}
    public ColorWheel() {
        Width=Size;Height=Size;Focusable=true;Cursor=Cursors.Cross;
        SetResourceReference(FocusVisualStyleProperty,"WheelFocus");
        AutomationProperties.SetName(this,"Color wheel");
        AutomationProperties.SetHelpText(this,"Left and right change hue. Up and down change saturation. Use the brightness slider for darker colors.");
        if(image==null)BuildImage();
    }
    static void BuildImage() {
        var pixels=new byte[Size*Size*4];
        for(int y=0;y<Size;y++)for(int x=0;x<Size;x++) {
            double dx=(x-Size/2.0)/(Size/2.0-4),dy=(y-Size/2.0)/(Size/2.0-4),radius=Math.Sqrt(dx*dx+dy*dy);
            if(radius>1)continue;var c=ColorLogic.Parse(FromHsv(Math.Atan2(-dy,dx)*180/Math.PI,radius,1));int offset=(y*Size+x)*4;
            pixels[offset]=c.B;pixels[offset+1]=c.G;pixels[offset+2]=c.R;pixels[offset+3]=255;
        }
        image=BitmapSource.Create(Size,Size,96,96,PixelFormats.Bgra32,null,pixels,Size*4);image.Freeze();
    }
    public static string FromHsv(double h,double s,double v) {
        h=(h%360+360)%360/60;s=Math.Max(0,Math.Min(1,s));v=Math.Max(0,Math.Min(1,v));
        double c=v*s,x=c*(1-Math.Abs(h%2-1)),m=v-c,r=0,g=0,b=0;
        if(h<1){r=c;g=x;}else if(h<2){r=x;g=c;}else if(h<3){g=c;b=x;}else if(h<4){g=x;b=c;}else if(h<5){r=x;b=c;}else{r=c;b=x;}
        return ColorLogic.Hex(System.Drawing.Color.FromArgb((int)Math.Round((r+m)*255),(int)Math.Round((g+m)*255),(int)Math.Round((b+m)*255)));
    }
    public void SetHex(string hex) {var c=ColorLogic.Parse(hex);hue=c.GetHue();double max=Math.Max(c.R,Math.Max(c.G,c.B)),min=Math.Min(c.R,Math.Min(c.G,c.B));saturation=max==0?0:(max-min)/max;value=max/255;InvalidateVisual();}
    public void SelectPoint(double x,double y) {double dx=x-Size/2.0,dy=y-Size/2.0;hue=(Math.Atan2(-dy,dx)*180/Math.PI+360)%360;saturation=Math.Min(1,Math.Sqrt(dx*dx+dy*dy)/(Size/2.0-4));Notify();}
    void Notify() {InvalidateVisual();if(Changed!=null)Changed();}
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {base.OnMouseLeftButtonDown(e);Focus();CaptureMouse();var p=e.GetPosition(this);SelectPoint(p.X,p.Y);e.Handled=true;}
    protected override void OnMouseMove(MouseEventArgs e) {base.OnMouseMove(e);if(IsMouseCaptured&&e.LeftButton==MouseButtonState.Pressed){var p=e.GetPosition(this);SelectPoint(p.X,p.Y);}}
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {base.OnMouseLeftButtonUp(e);if(IsMouseCaptured)ReleaseMouseCapture();}
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if(e.Key==Key.Left)hue=(hue+359)%360;else if(e.Key==Key.Right)hue=(hue+1)%360;
        else if(e.Key==Key.Up)saturation=Math.Min(1,saturation+.02);else if(e.Key==Key.Down)saturation=Math.Max(0,saturation-.02);else return;
        Notify();e.Handled=true;
    }
    protected override AutomationPeer OnCreateAutomationPeer(){return new FrameworkElementAutomationPeer(this);}
    protected override void OnRender(DrawingContext dc) {
        base.OnRender(dc);dc.DrawImage(image,new Rect(0,0,Size,Size));
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)Math.Round((1-value)*255),0,0,0)),null,new Point(Size/2,Size/2),Size/2-4,Size/2-4);
        double a=hue*Math.PI/180,r=saturation*(Size/2-4);var marker=new Point(Size/2+Math.Cos(a)*r,Size/2-Math.Sin(a)*r);
        dc.DrawEllipse(null,new Pen(Brushes.Black,4),marker,6,6);dc.DrawEllipse(null,new Pen(Brushes.White,2),marker,6,6);
    }
}
}
