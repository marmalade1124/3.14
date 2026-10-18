using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Web.Script.Serialization;
using Forms=System.Windows.Forms;
using Drawing=System.Drawing;

namespace PiStudio {
public static class Tests {
    public static bool IntegrationFailed;
    static int count;
    static readonly List<string> results=new List<string>();
    static void Check(bool ok,string name) {if(!ok)throw new Exception("FAIL: "+name);count++;results.Add("PASS: "+name);}
    static void Throws(Action action,string name) {bool threw=false;try{action();}catch(ArgumentException){threw=true;}Check(threw,name);}
    public static int Run() {
        string root=Path.Combine(Environment.CurrentDirectory,".test-data",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try {
            Check(ColorLogic.Normalize(" #aBc ")=="#AABBCC","short HEX normalization");
            Check(ColorLogic.Normalize("12abEF")=="#12ABEF","six-digit HEX normalization");
            var redLch=ColorLogic.ToOklch("#FF0000");Check(Math.Abs(redLch[0]-.627955)<.00001&&Math.Abs(redLch[1]-.257683)<.00001,"OKLCH known red reference");
            for(int i=0;i<60;i++){string hex="#"+((i*237187+12345)%16777216).ToString("X6");var lch=ColorLogic.ToOklch(hex);var c=ColorLogic.Parse(hex);var back=ColorLogic.Parse(ColorLogic.Oklch(lch[0],lch[1],lch[2]));Check(Math.Abs(c.R-back.R)<=1&&Math.Abs(c.G-back.G)<=1&&Math.Abs(c.B-back.B)<=1,"OKLCH round trip "+hex);}
            bool mapped;ColorLogic.Oklch(.7,.5,30,out mapped);Check(mapped,"out-of-sRGB OKLCH reduces chroma");
            Check(ColorLogic.ParseFormat("255 0 0","RGB",out mapped)=="#FF0000"&&ColorLogic.ParseFormat("120 100 50","HSL",out mapped)=="#00FF00","RGB and HSL numeric input");
            foreach(string invalid in new[]{"NaN 0 0","Infinity 0 0","256 0 0","1 2","1.5 0 0"})Throws(delegate{ColorLogic.ParseFormat(invalid,"RGB",out mapped);},"reject invalid numeric color "+invalid);
            Throws(delegate{ColorLogic.ParseFormat("2 .1 10","OKLCH",out mapped);},"reject OKLCH lightness outside range");
            Check(ColorWheel.FromHsv(0,1,1)=="#FF0000"&&ColorWheel.FromHsv(120,1,1)=="#00FF00"&&ColorWheel.FromHsv(240,1,1)=="#0000FF","wheel primary hues");
            Check(ColorWheel.FromHsv(-120,1,1)=="#0000FF"&&ColorWheel.FromHsv(720,1,1)=="#FF0000","wheel hue wraps");
            Check(ColorWheel.FromHsv(100,0,1)=="#FFFFFF"&&ColorWheel.FromHsv(100,1,0)=="#000000","wheel white and black endpoints");
            var wheel=new ColorWheel();foreach(string hex in new[]{"#123ABC","#FFFFFF","#000000","#227766","#808080"}){wheel.SetHex(hex);Check(wheel.Hex==hex,"wheel HEX round trip "+hex);}wheel.SetHex("#FFFFFF");wheel.SelectPoint(216,110);Check(wheel.Hex=="#FF0000","wheel pointer maps edge to full saturation");wheel.SelectPoint(110,110);Check(wheel.Hex=="#FFFFFF","wheel center maps to white");
            foreach(string bad in new[]{"","#12","#12345","#1234567","#gggggg","red","##fff","#１２３", "123456\nextra"})Throws(delegate{ColorLogic.Normalize(bad);},"reject HEX "+bad);
            Throws(delegate{ColorLogic.Normalize(null);},"reject null HEX");
            Check(Math.Abs(ColorLogic.Contrast("#000000","#FFFFFF")-21)<.000001,"WCAG black-white 21:1");
            Check(ColorLogic.Contrast("#123456","#123456")==1,"identical contrast 1:1");
            Check(Math.Abs(ColorLogic.Contrast("#777777","#FFFFFF")-4.478)<.002,"WCAG known borderline ratio");
            Check(ColorLogic.Contrast("#ABCDEF","#123456")==ColorLogic.Contrast("#123456","#ABCDEF"),"contrast symmetry");
            foreach(string seed in new[]{"#7160D4","#000000","#FFFFFF","#FF0000","#00FF00","#0000FF","#808080"})foreach(bool dark in new[]{false,true}) {
                var p=new Palette{Seed=seed,Dark=dark};ColorLogic.Generate(p);ColorLogic.Validate(p);
                Check(p.Colors[4]==seed,"seed is initial accent "+seed+dark);
                var first=(string[])p.Colors.Clone();ColorLogic.Generate(p);Check(first.SequenceEqual(p.Colors),"deterministic palette "+seed+dark);
                Check(new[]{2,3}.All(t=>new[]{0,1}.All(b=>ColorLogic.Contrast(p.Colors[t],p.Colors[b])>=4.5)),"generated text meets AA pairs "+seed+dark);
                Check(ColorLogic.Contrast(ColorLogic.ButtonText(p),p.Colors[4])>=4.5,"button label contrast "+seed+dark);
                for(int mask=0;mask<32;mask++) {var locked=p.Clone();for(int i=0;i<5;i++)locked.Locks[i]=(mask&(1<<i))!=0;locked.Variation++;locked.Dark=!dark;ColorLogic.Generate(locked);Check(Enumerable.Range(0,5).All(i=>!locked.Locks[i]||locked.Colors[i]==first[i]),"all lock combinations "+seed+dark+"/"+mask);}
            }
            var conflict=new Palette();ColorLogic.Generate(conflict);conflict.Colors[2]=conflict.Colors[0];conflict.Locks[2]=true;ColorLogic.Generate(conflict);Check(ColorLogic.Conflicts(conflict).Any(x=>x.Contains("locked values preserved")),"locked contrast conflict flagged");
            var palette=new Palette{Name="Test palette",Favorite=true};ColorLogic.Generate(palette);
            var imported=ColorLogic.Import(ColorLogic.Json(palette));Check(imported.Name==palette.Name&&imported.Colors.SequenceEqual(palette.Colors),"versioned palette import round trip");Check(ColorLogic.Import(ColorLogic.Json(palette).Replace("\"version\":1,","")).Colors.SequenceEqual(palette.Colors),"legacy exported palette import");Throws(delegate{ColorLogic.Import("{\"version\":99,\"colors\":{}}");},"reject future palette schema");Throws(delegate{ColorLogic.Import("{}");},"reject damaged palette import");
            string css=ColorLogic.Css(palette);Check(ColorLogic.Tokens.All(t=>css.Contains("--"+t+":")),"CSS exports every named token");
            var exported=new JavaScriptSerializer().DeserializeObject(ColorLogic.Json(palette)) as Dictionary<string,object>;
            Check(exported!=null&&((Dictionary<string,object>)exported["colors"]).Count==5&&((Dictionary<string,object>)exported["locks"]).Count==5,"JSON colors and locks parse");
            string path=Path.Combine(root,"library.json");var store=new Store(path);store.Data.Palettes.Add(palette);store.Data.Draft=palette.Clone();store.AddRecent("#fff");store.AddRecent("#FFFFFF");Check(store.Save(),"initial atomic save");
            var restored=new Store(path);Check(restored.Data.Palettes[0].Favorite&&restored.Data.Palettes[0].Name=="Test palette"&&restored.Data.Recent.Count==1&&restored.Data.Draft.Colors.SequenceEqual(palette.Colors),"palettes favorites draft and history survive restart");
            restored.AddRecent("#123456");Check(restored.Save()&&File.Exists(path+".bak"),"atomic replacement keeps backup");
            for(int i=0;i<30;i++)restored.AddRecent("#"+i.ToString("X6"));Check(restored.Data.Recent.Count==24,"recent history bounded");
            foreach(string invalid in new[]{"broken json","{}","null","{\"Version\":2,\"Palettes\":[],\"Recent\":[],\"Shortcut\":\"Ctrl+Shift+C\"}","{\"Version\":1,\"Palettes\":[{}],\"Recent\":[],\"Shortcut\":\"Ctrl+Shift+C\"}"}) {
                File.WriteAllText(path,invalid);var damaged=new Store(path);Check(damaged.ReadOnly&&damaged.Warning!=null&&!damaged.Save()&&File.ReadAllText(path)==invalid,"damaged data preserved "+invalid);
            }
            uint m,k;Check(Native.Hotkey("Ctrl+Shift+C",out m,out k)&&m==0x4006&&k==67,"default shortcut parse");Check(Native.Hotkey("Alt+F9",out m,out k)&&k==120,"function shortcut parse");Check(!Native.Hotkey("C",out m,out k)&&!Native.Hotkey("Ctrl+F25",out m,out k),"invalid shortcuts rejected");
            string legacy="{\"Version\":1,\"Palettes\":[],\"Recent\":[],\"Shortcut\":\"Ctrl+Shift+C\"}";Check(Store.ReadLibrary(legacy).Snapshots.Count==0&&Store.ReadLibrary(legacy).Theme=="Light","old library gains safe optional defaults");
            File.WriteAllText(path,"damaged original");var recovery=new Store(path);Check(recovery.Restore(legacy)&&!recovery.ReadOnly&&Directory.GetFiles(root,"*.recovery-*").Any(f=>File.ReadAllText(f)=="damaged original"),"restore archives damaged original before replacing");string validLibrary=File.ReadAllText(path);bool invalidRestore=false;try{recovery.Restore("{}");}catch{invalidRestore=true;}Check(invalidRestore&&File.ReadAllText(path)==validLibrary,"invalid restore leaves current library untouched");
            File.WriteAllLines("test-results.txt",results.Concat(new[]{count+" assertions passed"}));return 0;
        }catch(Exception e){File.WriteAllLines("test-results.txt",results.Concat(new[]{e.ToString()}));return 1;}
    }
    static async System.Threading.Tasks.Task Wait(int milliseconds) {await System.Threading.Tasks.Task.Delay(milliseconds);}
    static void Click(Button button) {button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
    public static async void Features(Studio s){try{await Wait(150);ToolChecks(s);s.Window.UpdateLayout();Snapshot(s.Window,"studio-updated.png");s.OpenColorEditor(4);await Wait(100);Snapshot(s.Window,"editor-updated.png");s.CloseEditor();Check(s.Get<Button>("Undo").IsEnabled,"undo state exposed in toolbar");Check(FindVisual<Button>(s.Get<System.Windows.Controls.Primitives.UniformGrid>("Colors")).Count>=15,"copy-only and contrast inspection actions available for every role");Click(s.Get<Button>("PreviewTab"));s.Window.UpdateLayout();Snapshot(s.Window,"preview-updated.png");Check(Fits(s,s.Get<Border>("ContrastCard")),"preview fits fixed window");results.Add(count+" feature assertions passed");File.WriteAllLines("feature-results.txt",results);s.Window.Close();}catch(Exception e){IntegrationFailed=true;File.WriteAllLines("feature-results.txt",results.Concat(new[]{e.ToString()}));s.Window.Close();}}
    public static async void Integration(Studio s) {
        Forms.Form fixture=null;Native.Point original;Native.GetCursorPos(out original);string clipboard=null;bool hadText=false;
        try {
            s.Window.Topmost=true;s.Window.Activate();Native.TestActivate(new System.Windows.Interop.WindowInteropHelper(s.Window).Handle);await Wait(250);
            hadText=System.Windows.Clipboard.ContainsText();if(hadText)clipboard=System.Windows.Clipboard.GetText();
            var monitors=Native.Monitors();results.Add("Displays: "+string.Join("; ",monitors.Select(r=>r.ToString())));

            s.Get<TextBox>("Seed").Text="#badvalue";Check(!s.ApplySeed(),"UI rejects invalid HEX");
            s.Get<TextBox>("Seed").Text="#3256AB";Check(s.ApplySeed()&&s.Current.Colors[4]=="#3256AB","UI manual seed generates palette");
            s.Current.Locks[4]=true;Click(s.Get<Button>("Generate"));Check(s.Current.Colors[4]=="#3256AB","UI regeneration preserves lock");s.Current.Locks[4]=false;
            Click(s.Get<Button>("Dark"));Check(s.Current.Dark,"dark variant control");Click(s.Get<Button>("Light"));Check(!s.Current.Dark,"light variant control");
            var staleInput=(TextBox)RoleColumn(s,4).Children[3];s.Current.Colors[4]="#354CBF";s.Render();staleInput.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice,0,staleInput,s.Get<Button>("PreviewTab")){RoutedEvent=Keyboard.LostKeyboardFocusEvent});
            Check(s.Current.Colors[4]=="#354CBF"&&((SolidColorBrush)s.Get<Button>("PreviewButton").Background).Color==Color.FromRgb(53,76,191),"detached HEX field cannot restore an old accent after render");
            Click(s.Get<Button>("PreviewTab"));await Wait(80);Check(s.Get<Button>("PreviewButton").IsVisible&&((SolidColorBrush)s.Get<Button>("PreviewButton").Background).Color==Color.FromRgb(53,76,191),"changed accent remains current on real-world screen");Click(s.Get<Button>("StudioTab"));
            var colorColumn=RoleColumn(s,4);((TextBox)colorColumn.Children[3]).Text="#227766";Check(s.Current.Colors[4]=="#227766"&&((SolidColorBrush)s.Get<Button>("PreviewButton").Background).Color==((SolidColorBrush)((Button)colorColumn.Children[1]).Background).Color,"role edits immediately update preview and swatch");
            Click((Button)colorColumn.Children[1]);await Wait(120);Check(s.IsEditorOpen&&System.Windows.Clipboard.GetText()=="#227766"&&s.SelectedRole==4,"swatch click copies HEX and opens selected-role wheel");
            var frame=(Border)s.Get<System.Windows.Controls.Primitives.UniformGrid>("Colors").Children[4];Check((((TextBlock)colorColumn.Children[0]).Text.Contains("EDITING")||((TextBlock)colorColumn.Children[0]).Text.Contains("COPIED"))&&((SolidColorBrush)frame.BorderBrush).Color==Color.FromRgb(108,89,200)&&s.Get<TextBlock>("PickTarget").Text.Contains("ACCENT"),"selected role has strong outline badge and picker target label");
            double openedBrightness=s.EditorWheel.Value;var beforeWheel=(string[])s.Current.Colors.Clone();s.EditorWheel.Value=.5;Check(s.Current.Colors[4]==s.EditorWheel.Hex&&Enumerable.Range(0,4).All(i=>s.Current.Colors[i]==beforeWheel[i]),"wheel edits only the selected role live");
            Check(FindVisual<TextBlock>(s.ColorEditor).Single(t=>t.Name=="BrightnessValue").Text=="50%","brightness displays live percentage");
            double resetHue=s.EditorWheel.Hue,resetSaturation=s.EditorWheel.Saturation;Click(FindVisual<Button>(s.ColorEditor).Single(b=>b.Name=="ResetBrightness"));Check(s.EditorWheel.Value==openedBrightness&&s.EditorWheel.Hue==resetHue&&s.EditorWheel.Saturation==resetSaturation&&s.Current.Colors[4]==beforeWheel[4]&&FindVisual<TextBlock>(s.ColorEditor).Single(t=>t.Name=="BrightnessValue").Text==Math.Round(openedBrightness*100)+"%","reset restores opening brightness and updates HEX preview and percentage without changing hue or saturation");s.EditorWheel.Value=.5;
            FindVisual<TextBox>(s.ColorEditor).Single().Text="#FFFFFF";Check(FindVisual<TextBlock>(s.ColorEditor).Single(t=>t.Name=="BrightnessValue").Text=="100%","brightness percentage tracks HEX edits");s.EditorWheel.Value=.5;
            s.Window.UpdateLayout();await Wait(120);Native.TestActivate(new System.Windows.Interop.WindowInteropHelper(s.Window).Handle);var wheelPoint=s.EditorWheel.PointToScreen(new Point(180,110));results.Add("Wheel target "+wheelPoint+" hit "+s.Window.InputHitTest(s.Window.PointFromScreen(wheelPoint)));Native.SetCursorPos((int)wheelPoint.X,(int)wheelPoint.Y);Native.Point observed;Native.GetCursorPos(out observed);results.Add("Cursor after position "+observed.X+","+observed.Y+" foreground "+Native.GetForegroundWindow()+" hit HWND "+Native.WindowFromPoint(observed)+" main "+new System.Windows.Interop.WindowInteropHelper(s.Window).Handle);Native.Rect ownRect;Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(s.Window).Handle,out ownRect);var nativeClass=new System.Text.StringBuilder(256);Native.GetClassName(Native.WindowFromPoint(observed),nativeClass,256);results.Add("Native bounds "+ownRect.Bounds+" hit class "+nativeClass+" enabled "+s.Window.IsEnabled+" native hit "+Native.SendMessage(new System.Windows.Interop.WindowInteropHelper(s.Window).Handle,0x84,IntPtr.Zero,new IntPtr((observed.Y<<16)|(observed.X&65535))));s.EditorWheel.PreviewMouseDown+=delegate{results.Add("Wheel received mouse down");};Native.TestMouse(true);await Wait(40);wheelPoint=s.EditorWheel.PointToScreen(new Point(110,40));Native.SetCursorPos((int)wheelPoint.X,(int)wheelPoint.Y);await Wait(40);Native.TestMouse(false);await Wait(80);Check(s.IsEditorOpen&&Math.Abs(s.EditorWheel.Hue-90)<2&&s.EditorWheel.Saturation>.6&&s.Current.Colors[4]==s.EditorWheel.Hex,"actual wheel drag updates hue saturation and role color (open "+s.IsEditorOpen+", hue "+s.EditorWheel.Hue+", saturation "+s.EditorWheel.Saturation+")");
            Native.TestActivate(new System.Windows.Interop.WindowInteropHelper(s.Window).Handle);Keyboard.Focus(s.EditorWheel);double hueBefore=s.EditorWheel.Hue;Native.TestKey(39,false);Native.TestKey(39,true);await Wait(100);Check(s.EditorWheel.Hue!=hueBefore&&s.EditorWheel.Hex==s.Current.Colors[4],"wheel keyboard edit updates color (hue: "+hueBefore+" -> "+s.EditorWheel.Hue+")");
            Snapshot(s.ColorEditor,"color-wheel-preview.png");Native.TestKey(27,false);Native.TestKey(27,true);await Wait(100);Check(!s.IsEditorOpen&&s.Current.Colors[4]==beforeWheel[4],"Escape cancels wheel changes and restores opening color");
            Click(s.Get<Button>("CopyCss"));Check(System.Windows.Clipboard.GetText()==ColorLogic.Css(s.Current),"actual clipboard CSS copy");
            s.Get<TextBox>("Seed").Focus();await Wait(100);results.Add("Foreground: "+Native.GetForegroundWindow()+" studio: "+new System.Windows.Interop.WindowInteropHelper(s.Window).Handle);Native.TestKey(9,false);Native.TestKey(9,true);await Wait(150);
            if(Keyboard.FocusedElement!=s.Get<Button>("ApplySeed")) {results.Add("Native Tab did not reach studio; checking WPF traversal separately.");s.Get<TextBox>("Seed").MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));}
            Check(Keyboard.FocusedElement==s.Get<Button>("ApplySeed"),"Tab order reaches Apply from seed");
            string latestAccent=s.Current.Colors[4];Click(s.Get<Button>("PreviewTab"));await Wait(80);Check(ColorLogic.Hex(System.Drawing.Color.FromArgb(((SolidColorBrush)s.Get<Button>("PreviewButton").Background).Color.R,((SolidColorBrush)s.Get<Button>("PreviewButton").Background).Color.G,((SolidColorBrush)s.Get<Button>("PreviewButton").Background).Color.B))==latestAccent,"wheel accent survives dismissal and preview navigation");Click(s.Get<Button>("StudioTab"));
            s.Window.Width=940;s.Window.Height=720;await Wait(100);Check(s.Window.ActualWidth>=940&&s.Window.ActualHeight>=720,"minimum studio layout size");Snapshot(s.Window,"studio-minimum.png");
            var editorCard=s.Get<Border>("EditorCard");Check(FindVisual<ScrollViewer>(s.Get<Grid>("Workspace")).All(v=>v.TemplatedParent is TextBox)&&Fits(s,s.Get<Border>("ExportCard"))&&s.Get<Button>("Generate").TranslatePoint(new Point(0,s.Get<Button>("Generate").ActualHeight),editorCard).Y<=editorCard.ActualHeight-editorCard.Padding.Bottom+1,"palette fits minimum window without content scrolling or clipped controls");
            for(int role=0;role<5;role++){s.OpenColorEditor(role);await Wait(60);var start=s.ColorEditor.TranslatePoint(new Point(),s.Window);var end=s.ColorEditor.TranslatePoint(new Point(s.ColorEditor.ActualWidth,s.ColorEditor.ActualHeight),s.Window);Check(start.X>=0&&start.Y>=0&&end.X<=s.Window.ActualWidth&&end.Y<=s.Window.ActualHeight&&s.ColorEditor.ActualWidth<=250&&s.ColorEditor.ActualHeight<s.Window.ActualHeight-48,"compact editor contained at minimum window for "+ColorLogic.Roles[role]);s.CloseEditor();}
            s.OpenColorEditor(4);await Wait(80);Snapshot(s.Window,"color-editor-contained.png");Native.TestActivate(new System.Windows.Interop.WindowInteropHelper(s.Window).Handle);var outside=s.Window.PointToScreen(new Point(20,s.Window.ActualHeight/2));Native.SetCursorPos((int)outside.X,(int)outside.Y);Native.TestMouse(true);Native.TestMouse(false);await Wait(80);Check(!s.IsEditorOpen,"clicking outside contained editor dismisses it");s.SelectRole(-1);
            Click(s.Get<Button>("PreviewTab"));await Wait(100);Check(s.Get<Grid>("PreviewPage").IsVisible&&!s.Get<Grid>("StudioPage").IsVisible&&Fits(s,s.Get<Border>("ContrastCard"))&&Fits(s,s.Get<Button>("PreviewButton")),"preview and contrast fit their own screen at minimum size");Snapshot(s.Window,"preview-minimum.png");Click(s.Get<Button>("StudioTab"));
            var savedRecords=s.Store.Data.Palettes.ToList();for(int i=0;i<30;i++){var p=new Palette{Name="Overflow palette "+i};ColorLogic.Generate(p);s.Store.Data.Palettes.Add(p);}s.RenderLibrary();s.Window.UpdateLayout();await Wait(100);var bars=FindVisual<System.Windows.Controls.Primitives.ScrollBar>(s.Get<ScrollViewer>("LibraryScroll"));Snapshot(s.Window,"library-overflow.png");Check(s.Get<ScrollViewer>("LibraryScroll").ScrollableHeight>0&&bars.Any(b=>b.IsVisible&&b.ActualWidth==8&&FindVisual<System.Windows.Controls.Primitives.Thumb>(b).Any(t=>t.Template.FindName("ThumbBody",t)!=null)),"overflowing library uses custom slim scrollbar ("+string.Join("; ",bars.Select(b=>b.IsVisible+" width "+b.ActualWidth+" thumbs "+FindVisual<System.Windows.Controls.Primitives.Thumb>(b).Count))+")");s.Get<ScrollViewer>("LibraryScroll").ScrollToEnd();await Wait(100);Check(s.Get<ScrollViewer>("LibraryScroll").VerticalOffset>0,"custom library scrollbar supports scrolling");s.Store.Data.Palettes=savedRecords;s.RenderLibrary();
            Click(s.Get<Button>("Minimize"));Check(s.Window.WindowState==WindowState.Minimized,"custom minimize control");s.Window.WindowState=WindowState.Normal;s.Window.Activate();
            s.Window.Width=1160;s.Window.Height=760;
            var bounds=monitors[0];int x=bounds.Left+100,y=bounds.Top+100;
            fixture=new Forms.Form {FormBorderStyle=Forms.FormBorderStyle.None,StartPosition=Forms.FormStartPosition.Manual,Bounds=new Drawing.Rectangle(bounds.Left+40,bounds.Top+40,300,240),BackColor=Drawing.Color.FromArgb(18,58,188),TopMost=true,ShowInTaskbar=false};fixture.Show();fixture.Refresh();await Wait(200);
            s.SelectRole(-1);System.Windows.Clipboard.SetText("cancel sentinel");s.StartPicker();await Wait(650);Check(s.ActivePicker!=null&&Picker.LiveCaptures==monitors.Count,"real capture creates one overlay per display; "+s.Get<TextBlock>("Status").Text);Check(!s.Window.IsVisible,"studio excluded before screen capture");
            Native.TestKey(27,false);Native.TestKey(27,true);await Wait(250);Check(s.ActivePicker==null&&Picker.LiveCaptures==0&&System.Windows.Clipboard.GetText()=="cancel sentinel","Escape disposes captures and preserves clipboard");
            Check(s.SetShortcut("Ctrl+Shift+F9",false),"global shortcut registration");
            Native.SetCursorPos(x,y);Native.TestKey(17,false);Native.TestKey(16,false);Native.TestKey(120,false);Native.TestKey(120,true);Native.TestKey(16,true);Native.TestKey(17,true);await Wait(650);
            Check(s.ActivePicker!=null,"actual global shortcut activates picker");
            fixture.BackColor=Drawing.Color.Red;fixture.Refresh();Native.SetCursorPos(x,y);
            Native.TestMouse(true);Native.TestMouse(false);await Wait(300);
            Check(s.ActivePicker==null&&Picker.LiveCaptures==0,"selection disposes every capture");Check(s.Current.Seed=="#123ABC"&&System.Windows.Clipboard.GetText()=="#123ABC","click samples frozen pixel and copies exact HEX (seed "+s.Current.Seed+", clipboard "+System.Windows.Clipboard.GetText()+")");
            Check(s.Store.Data.Recent.Contains("#123ABC"),"actual picked color added to history");
            s.Current.Locks[1]=true;s.SelectRole(1);var beforePick=(string[])s.Current.Colors.Clone();string seedBefore=s.Current.Seed;
            Click((Button)RoleColumn(s,1).Children[1]);Native.TestActivate(new System.Windows.Interop.WindowInteropHelper(s.Window).Handle);Native.TestKey(17,false);Native.TestKey(16,false);Native.TestKey(120,false);Native.TestKey(120,true);Native.TestKey(16,true);Native.TestKey(17,true);await Wait(650);Check(!s.IsEditorOpen&&s.ActivePicker!=null,"global shortcut closes color popup before capture");Native.SetCursorPos(x,y);Native.TestMouse(true);Native.TestMouse(false);await Wait(250);
            Check(s.Current.Colors[1]=="#FF0000"&&s.Current.Seed==seedBefore&&Enumerable.Range(0,5).All(i=>i==1||s.Current.Colors[i]==beforePick[i])&&System.Windows.Clipboard.GetText()=="#FF0000"&&s.SelectedRole==1,"screen pick updates selected locked role only and retains selection");s.Current.Locks[1]=false;
            fixture.Close();fixture.Dispose();fixture=null;
            s.Get<TextBox>("PaletteName").Text="Integration "+Guid.NewGuid().ToString("N");s.Get<CheckBox>("Favorite").IsChecked=true;s.SavePalette();var reload=new Store(s.Store.PathName);Check(reload.Data.Palettes.Any(p=>p.Name==s.Current.Name&&p.Favorite),"UI save persists named favorite");
            await Wait(150);
            ToolChecks(s);await Wait(150);Snapshot(s.Window,"studio-preview.png");
            results.Add(count+" integration assertions passed");File.WriteAllLines("integration-results.txt",results);
            Click(s.Get<Button>("Close"));
        }catch(Exception e){IntegrationFailed=true;File.WriteAllLines("integration-results.txt",results.Concat(new[]{e.ToString()}));if(s.ActivePicker!=null){s.ActivePicker.Dispose();s.ActivePicker=null;}s.Window.Close();}
        finally {if(fixture!=null)fixture.Dispose();Native.SetCursorPos(original.X,original.Y);if(hadText&&clipboard!=null)try{System.Windows.Clipboard.SetText(clipboard);}catch{} }
    }
    static void ToolChecks(Studio s){
        Check(s.Window.ResizeMode==ResizeMode.CanMinimize&&s.Window.MinWidth==s.Window.MaxWidth&&s.Window.MinHeight==s.Window.MaxHeight,"window has fixed dimensions and no resize or maximize");
        var opening=s.Current.Clone();s.OpenColorEditor(4);s.EditorWheel.Value=.17;s.CloseEditor();Check(s.Current.Colors.SequenceEqual(opening.Colors),"Cancel restores opening palette");
        s.OpenColorEditor(4);for(int i=1;i<8;i++)s.EditorWheel.Value=.1+i*.03;s.CloseEditor(true);string accepted=s.Current.Colors[4];s.Undo();Check(s.Current.Colors[4]==opening.Colors[4],"one undo reverts a full wheel gesture");s.Redo();Check(s.Current.Colors[4]==accepted,"redo restores accepted color edit");
        s.SelectRole(1);s.Get<TextBox>("Seed").Focus();Check(s.SelectedRole==1,"field focus does not silently change picker target");Click(s.Get<Button>("PickBase"));Check(s.SelectedRole==-1,"explicit Base target");Click(s.Get<Button>("PickRole"));Check(s.SelectedRole==4,"explicit selected-role target");
        Check(s.Store.Data.Theme=="Light"&&((SolidColorBrush)s.Window.Background).Color.R>240,"workspace stays light");Check(Store.ReadLibrary("{\"Version\":1,\"Palettes\":[],\"Recent\":[],\"Shortcut\":\"Ctrl+Shift+C\",\"Theme\":\"Dark\"}").Theme=="Light","legacy dark preference safely becomes light");
        int snapshots=s.Store.Data.Snapshots.Count;s.SaveSnapshot();Check(s.Store.Data.Snapshots.Count==snapshots+1&&new Store(s.Store.PathName).Data.Snapshots.Count==snapshots+1,"palette snapshots persist across reload");
        string shortName=s.Current.Name;s.Current.Name=new string('a',100);s.SaveSnapshot();Check(s.Store.Data.Snapshots[0].Name.Length<=100&&s.Store.Warning==null,"long snapshot names remain valid on disk");s.Current.Name=shortName;
        var source=s.Current.Clone();source.Name="Import test";s.ImportPaletteJson(ColorLogic.Json(source));int count=s.Store.Data.Palettes.Count;s.ImportPaletteJson(ColorLogic.Json(source));Check(s.Store.Data.Palettes.Count==count+1&&s.Store.Data.Palettes.Select(p=>p.Name).Distinct().Count()==s.Store.Data.Palettes.Count,"palette imports avoid name collisions");
        string backup=new JavaScriptSerializer().Serialize(s.Store.Data);Check(s.RestoreLibraryJson(backup)&&Directory.GetFiles(Path.GetDirectoryName(s.Store.PathName),Path.GetFileName(s.Store.PathName)+".recovery-*").Length>0,"library restore workflow archives current file");
        var saved=s.Store.Data.Palettes[0];s.RenamePalette(saved,"Renamed palette");Check(s.Store.Data.Palettes.Any(p=>p.Name=="Renamed palette"),"rename saved palette");s.DuplicatePalette(saved);Check(s.Store.Data.Palettes.Any(p=>p.Name.StartsWith("Renamed palette copy")),"duplicate saved palette");int beforeDelete=s.Store.Data.Palettes.Count;s.DeletePalette(s.Store.Data.Palettes[0]);s.Undo();Check(s.Store.Data.Palettes.Count==beforeDelete,"delete palette is undoable");
        s.Get<TextBox>("LibrarySearch").Text="no-such-palette";Check(s.Get<StackPanel>("Saved").Children.Count==1&&s.Get<StackPanel>("Saved").Children[0] is TextBlock,"library search empty result");s.Get<TextBox>("LibrarySearch").Text="";s.Get<ComboBox>("LibrarySort").SelectedIndex=1;Check(s.Get<StackPanel>("Saved").Children.Count==s.Store.Data.Palettes.Count,"library search and sort show all matches");
        Click(s.Get<Button>("ExportPreview"));s.Window.UpdateLayout();Check(FindVisual<TextBox>(s.Window).Any(t=>t.IsReadOnly&&t.Text==ColorLogic.Css(s.Current)),"export review shows exact CSS output");s.CloseUtility();
        Click(s.Get<Button>("Settings"));s.Window.UpdateLayout();Check(FindVisual<Button>(s.Window).Any(b=>Convert.ToString(b.Content)=="Record shortcut")&&FindVisual<Button>(s.Window).Any(b=>Convert.ToString(b.Content)=="Restore library"),"settings exposes recorder and local backup recovery");var dialog=FindVisual<Border>(s.Window).Single(b=>b.Width==540&&b.CornerRadius.TopLeft==16);var origin=dialog.TranslatePoint(new Point(0,0),s.Window);Check(Math.Abs(origin.X+dialog.ActualWidth/2-s.Window.ActualWidth/2)<1&&Math.Abs(origin.Y+dialog.ActualHeight/2-s.Window.ActualHeight/2)<1,"settings centered in full window");Snapshot(s.Window,"settings-preview.png");s.CloseUtility();
        int historyCount=s.Store.Data.Snapshots.Count;int palettes=s.Store.Data.Palettes.Count;var recent=s.Store.Data.Recent.ToArray();s.ClearSnapshotHistory();Check(s.Store.Data.Snapshots.Count==0&&new Store(s.Store.PathName).Data.Snapshots.Count==0&&s.Store.Data.Palettes.Count==palettes&&s.Store.Data.Recent.SequenceEqual(recent),"clear history persists without deleting palettes or recent colors");s.Undo();Check(s.Store.Data.Snapshots.Count==historyCount,"clear history is undoable");Click(s.Get<Button>("HistoryButton"));s.Window.UpdateLayout();Snapshot(s.Window,"history-preview.png");s.CloseUtility();
        s.OpenColorEditor(4);s.Window.UpdateLayout();var advanced=FindVisual<Expander>(s.ColorEditor).Single();advanced.IsExpanded=true;s.Window.UpdateLayout();var formats=FindVisual<ComboBox>(s.ColorEditor).Single();formats.SelectedItem="OKLCH";var numeric=FindVisual<TextBox>(s.ColorEditor).Single(t=>t.FontSize==12);numeric.Text="0.7 0.5 30";Click(FindVisual<Button>(s.ColorEditor).Single(b=>Convert.ToString(b.Content)=="Preview values"));Check(FindVisual<TextBlock>(s.ColorEditor).Any(t=>t.Text.Contains("Chroma reduced")),"advanced OKLCH input reports gamut mapping");Snapshot(s.Window,"advanced-editor.png");s.CloseEditor();
        s.Current=opening;s.Store.Data.Palettes.Clear();s.Get<TextBox>("PaletteName").Text=opening.Name;s.Render();s.RenderLibrary();s.SelectRole(-1);
    }
    static StackPanel RoleColumn(Studio s,int index){return (StackPanel)((Border)s.Get<System.Windows.Controls.Primitives.UniformGrid>("Colors").Children[index]).Child;}
    static bool Fits(Studio s,FrameworkElement element){var p=element.TranslatePoint(new Point(element.ActualWidth,element.ActualHeight),s.Get<Grid>("Workspace"));return p.X<=s.Get<Grid>("Workspace").ActualWidth+1&&p.Y<=s.Get<Grid>("Workspace").ActualHeight+1;}
    static List<T> FindVisual<T>(DependencyObject root) where T:DependencyObject {var found=new List<T>();for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T)found.Add((T)child);found.AddRange(FindVisual<T>(child));}return found;}
    static void Snapshot(FrameworkElement window,string path) {var image=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);var visual=new DrawingVisual();using(var context=visual.RenderOpen())context.DrawRectangle(new VisualBrush(window),null,new Rect(0,0,window.ActualWidth,window.ActualHeight));image.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(path))encoder.Save(stream);}
}
}











