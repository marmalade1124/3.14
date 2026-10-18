using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace PiStudio {
public sealed partial class Studio {
    public readonly Window Window;
    public readonly Store Store;
    public Palette Current;
    public Picker ActivePicker;
    IntPtr handle;
    bool rendering;
    string activeShortcut;
    int selectedRole=-1;
    readonly Border[] roleFrames=new Border[5];
    readonly Button[] roleSwatches=new Button[5];
    readonly TextBox[] roleInputs=new TextBox[5];
    readonly TextBlock[] roleBadges=new TextBlock[5];
    public int SelectedRole {get{return selectedRole;}}
    Grid editorLayer;
    public Border ColorEditor {get;private set;}
    public bool IsEditorOpen {get{return editorLayer!=null;}}
    public ColorWheel EditorWheel {get;private set;}
    public T Get<T>(string name) where T:FrameworkElement {return (T)Window.FindName(name);}
    static Brush Brush(string hex) {return (Brush)new BrushConverter().ConvertFromString(hex);}
    public Studio(string dataPath) {
        Store=new Store(dataPath);
        using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("Main.xaml")) Window=(Window)XamlReader.Load(resource);
        Current=Store.Data.Draft??new Palette(); if(Current.Colors[0]==null)ColorLogic.Generate(Current);
        Get<TextBox>("Seed").Text=Current.Seed; Get<TextBox>("PaletteName").Text=Current.Name;Get<CheckBox>("Favorite").IsChecked=Current.Favorite;
        Get<TextBox>("Shortcut").Text=Store.Data.Shortcut;
        On("Close",delegate {Window.Close();}); On("Minimize",delegate {Window.WindowState=WindowState.Minimized;});
        Get<FrameworkElement>("TitleBar").MouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e) {if((e.OriginalSource is TextBlock||e.OriginalSource==s||e.OriginalSource is Grid)&&e.ClickCount==1)Window.DragMove();};
        On("Pick",delegate {StartPicker();}); On("ApplySeed",delegate {ApplySeed();});
        On("StudioTab",delegate{ShowPage(false);});On("PreviewTab",delegate{ShowPage(true);});
        Get<TextBox>("Seed").KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Enter)ApplySeed();};
        On("Generate",delegate {Current.Variation=(Current.Variation+1)%100000;ColorLogic.Generate(Current);Render();Persist();});
        On("Light",delegate {SetMode(false);});On("Dark",delegate {SetMode(true);});
        On("CopyCss",delegate {Copy(ColorLogic.Css(Current),"CSS variables copied.");});
        On("Export",delegate {var dialog=new SaveFileDialog {Filter="JSON palette (*.json)|*.json",FileName=SafeName(Current.Name)+".json",DefaultExt=".json"};if(dialog.ShowDialog(Window)==true)try{File.WriteAllText(dialog.FileName,ColorLogic.Json(Current));Status("Exported "+dialog.FileName);}catch(Exception e){Status("Export failed: "+e.Message);}});
        On("Save",delegate {SavePalette();});On("SetShortcut",delegate {SetShortcut(Get<TextBox>("Shortcut").Text,true);});
        Get<CheckBox>("FavoritesOnly").Click+=delegate{RenderLibrary();};
        Get<CheckBox>("Favorite").Click+=delegate{RememberChange();Current.Favorite=Get<CheckBox>("Favorite").IsChecked==true;Persist();};
        bool nameChanged=false;Get<TextBox>("PaletteName").GotKeyboardFocus+=delegate{nameChanged=false;};Get<TextBox>("PaletteName").TextChanged+=delegate {if(!rendering){if(!nameChanged){RememberChange();nameChanged=true;}Current.Name=Get<TextBox>("PaletteName").Text;Persist();}};
        Window.SourceInitialized+=delegate {handle=new WindowInteropHelper(Window).Handle;Native.RoundWindow(handle);HwndSource.FromHwnd(handle).AddHook(Hook);SetShortcut(Store.Data.Shortcut,false);};
        Window.Closing+=delegate {CloseEditor();CloseUtility();if(ActivePicker!=null){ActivePicker.Dispose();ActivePicker=null;}Native.UnregisterHotKey(handle,1);Persist();};
        Window.Loaded+=delegate {if(Store.Warning!=null)Status(Store.Warning);};
        InitializeTools();Render(); RenderLibrary();
    }
    static string SafeName(string name) {string n=string.Concat((name??"palette").Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c)).Trim().TrimEnd('.');return n.Length==0?"palette":n;}
    void On(string name,Action action) {Get<Button>(name).Click+=delegate{bool generate=name=="Generate"||name=="Light"||name=="Dark";if(generate)RememberChange();action();if(generate){AddSnapshot();Persist();}};}
    public void Status(string value) {var status=Get<TextBlock>("Status");status.Text=value;status.ToolTip=value;var peer=System.Windows.Automation.Peers.UIElementAutomationPeer.FromElement(status)??System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(status);if(peer!=null)peer.RaiseAutomationEvent(System.Windows.Automation.Peers.AutomationEvents.LiveRegionChanged);}
    public void Persist() {Store.Data.Draft=Current.Clone();if(!Store.Save())Status(Store.Warning??"Saving is disabled; original library is preserved.");ShowStorageWarning();UpdateSaveState();}
    void ShowStorageWarning() {var warning=Get<TextBlock>("StorageWarning");warning.Text=Store.Warning??"";warning.ToolTip=Store.Warning;warning.Visibility=Store.Warning==null?Visibility.Collapsed:Visibility.Visible;}
    public bool Copy(string value,string message) {try{System.Windows.Clipboard.SetText(value);Status(message);return true;}catch(Exception e){Status("Clipboard unavailable. Try again. "+e.Message);return false;}}
    public void ShowPage(bool preview) {
        CloseEditor();Get<Grid>("StudioPage").Visibility=preview?Visibility.Collapsed:Visibility.Visible;Get<Grid>("PreviewPage").Visibility=preview?Visibility.Visible:Visibility.Collapsed;
        Get<Button>("StudioTab").Background=UiBrush(preview?"#F5F5F8":"#DDD7F3");Get<Button>("PreviewTab").Background=UiBrush(preview?"#DDD7F3":"#F5F5F8");
        Get<TextBlock>("PageHeading").Text=preview?"In the real world":"A little color. Endless possibility.";
        UpdatePreview();
        Animate(Get<Grid>(preview?"PreviewPage":"StudioPage"));
    }
    public void SelectRole(int index) {
        if(index < -1||index>4)throw new ArgumentOutOfRangeException("index");
        selectedRole=index;
        for(int i=0;i<5;i++)if(roleFrames[i]!=null) {
            roleFrames[i].BorderBrush=UiBrush(i==index?"#6C59C8":"#FFFFFF");roleFrames[i].Background=UiBrush(i==index?"#F0ECFC":"#FFFFFF");
            roleBadges[i].Text=i==index?"● EDITING":" ";
            System.Windows.Automation.AutomationProperties.SetHelpText(roleSwatches[i],i==index?"Selected screen-picker target. Click to copy HEX and edit.":"Click to select, copy HEX and edit.");
        }
        string role=index<0?"Base color":ColorLogic.Roles[index];
        Get<TextBlock>("PickTarget").Text="PICKING: "+role.ToUpperInvariant();Get<Button>("Pick").Content="⌖   Pick "+role.ToLowerInvariant();
        Get<Button>("PickBase").Background=UiBrush(index<0?"#FFFFFF":"#DDD7F3");Get<Button>("PickRole").Background=UiBrush(index<0?"#DDD7F3":"#FFFFFF");
        Get<TextBlock>("SelectionHeading").Text=index<0?"Choose a role to edit":"Editing "+role+"  ·  "+Current.Colors[index];
    }
    public void SetRoleColor(int index,string hex,bool save) {
        hex=ColorLogic.Normalize(hex);Current.Colors[index]=hex;
        roleSwatches[index].Background=Brush(hex);if(roleInputs[index].Text!=hex)roleInputs[index].Text=hex;
        UpdatePreview();SelectRole(index);if(save)Persist();
    }
    public void CloseEditor(bool apply=false) {if(editorLayer==null)return;var layer=editorLayer;var before=editorBefore;editorLayer=null;editorBefore=null;RemoveOverlay(layer);if(apply){if(ColorLogic.Json(before)!=ColorLogic.Json(Current))RememberChange(before);}else Current=before;SyncCurrent();Persist();}
    public void OpenColorEditor(int index) {
        CloseEditor();SelectRole(index);
        editorBefore=Current.Clone();
        var content=new StackPanel();var shell=new Border {Width=248,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Background=UiBrush("#FFFFFF"),BorderBrush=UiBrush("#D9D2EE"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(16),Padding=new Thickness(16),Child=content};
        content.Children.Add(new TextBlock {Text="Edit "+ColorLogic.Roles[index],FontFamily=new FontFamily("Bahnschrift"),FontSize=17,FontWeight=FontWeights.SemiBold,Foreground=UiBrush("#292A38")});
        content.Children.Add(new TextBlock {Text="Drag to edit. Changes preview live.",FontSize=11,Foreground=UiBrush("#646676"),Margin=new Thickness(0,6,0,12)});
        EditorWheel=new ColorWheel();EditorWheel.SetHex(Current.Colors[index]);content.Children.Add(new Viewbox {Width=180,Height=180,Child=EditorWheel});
        var brightnessHeading=new Grid {Margin=new Thickness(0,10,0,4)};brightnessHeading.ColumnDefinitions.Add(new ColumnDefinition());brightnessHeading.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(42)});brightnessHeading.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
        brightnessHeading.Children.Add(new TextBlock {Text="Brightness",Foreground=UiBrush("#292A38"),VerticalAlignment=VerticalAlignment.Center});
        var brightnessValue=new TextBlock {Name="BrightnessValue",Text=Math.Round(EditorWheel.Value*100)+"%",FontFamily=new FontFamily("Cascadia Mono, Consolas"),FontSize=12,Foreground=UiBrush("#646676"),TextAlignment=TextAlignment.Right,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(brightnessValue,1);brightnessHeading.Children.Add(brightnessValue);content.Children.Add(brightnessHeading);
        double initialBrightness=EditorWheel.Value;var resetBrightness=new Button {Name="ResetBrightness",Content="Reset",FontSize=12,MinHeight=28,Padding=new Thickness(8,2,8,2),Margin=new Thickness(10,0,-8,0),Background=Brush("#00FFFFFF"),Foreground=UiBrush("#6C59C8"),ToolTip="Restore brightness to "+Math.Round(initialBrightness*100)+"% from when this editor opened"};System.Windows.Automation.AutomationProperties.SetName(resetBrightness,"Reset brightness");Grid.SetColumn(resetBrightness,2);brightnessHeading.Children.Add(resetBrightness);resetBrightness.Click+=delegate{EditorWheel.Value=initialBrightness;};
        var brightness=new Slider {Minimum=0,Maximum=1,Value=EditorWheel.Value,SmallChange=.01,LargeChange=.1};System.Windows.Automation.AutomationProperties.SetName(brightness,"Color brightness");content.Children.Add(brightness);
        var hexInput=new TextBox {Text=Current.Colors[index],FontFamily=new FontFamily("Cascadia Mono, Consolas"),Margin=new Thickness(0,12,0,10),MaxLength=20};System.Windows.Automation.AutomationProperties.SetName(hexInput,"Color wheel HEX");content.Children.Add(hexInput);
        bool syncing=false;
        EditorWheel.Changed+=delegate {if(syncing)return;syncing=true;string hex=EditorWheel.Hex;brightness.Value=EditorWheel.Value;hexInput.Text=hex;SetRoleColor(index,hex,false);syncing=false;};
        brightness.ValueChanged+=delegate {brightnessValue.Text=Math.Round(brightness.Value*100)+"%";if(!syncing)EditorWheel.Value=brightness.Value;};
        hexInput.TextChanged+=delegate {if(syncing)return;try {string hex=ColorLogic.Normalize(hexInput.Text);syncing=true;EditorWheel.SetHex(hex);brightness.Value=EditorWheel.Value;SetRoleColor(index,hex,false);hexInput.ClearValue(Control.BorderBrushProperty);}catch(ArgumentException){hexInput.BorderBrush=UiBrush("#B84132");}finally{syncing=false;}};
        var actions=new StackPanel {Orientation=Orientation.Horizontal};var copy=new Button {Content="Copy HEX",Padding=new Thickness(8,7,8,7),FontSize=12,Background=UiBrush("#EFEDF8"),Margin=new Thickness(0,0,6,0)};copy.Click+=delegate{Copy(Current.Colors[index],"Copied "+Current.Colors[index]);};actions.Children.Add(copy);
        var cancel=new Button {Content="Cancel",Padding=new Thickness(8,7,8,7),Background=UiBrush("#EFEDF8"),Margin=new Thickness(0,0,6,0)};cancel.Click+=delegate{CloseEditor();roleSwatches[index].Focus();};actions.Children.Add(cancel);
        var done=new Button {Content="Apply",Padding=new Thickness(8,7,8,7),Background=UiBrush("#6C59C8"),Foreground=SystemParameters.HighContrast?SystemColors.HighlightTextBrush:Brush("#FFFFFF")};done.Click+=delegate{CloseEditor(true);roleSwatches[index].Focus();};actions.Children.Add(done);content.Children.Add(actions);AdvancedEditor(content,index,hexInput);
        var layer=new Grid {Background=new SolidColorBrush(Color.FromArgb(36,30,26,48))};layer.Children.Add(shell);Grid.SetRowSpan(layer,3);Panel.SetZIndex(layer,100);ColorEditor=shell;editorLayer=layer;
        layer.MouseDown+=delegate(object sender,MouseButtonEventArgs e){if(e.OriginalSource==layer){CloseEditor();roleSwatches[index].Focus();e.Handled=true;}};
        shell.PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){CloseEditor();roleSwatches[index].Focus();e.Handled=true;}};
        KeyboardNavigation.SetTabNavigation(shell,KeyboardNavigationMode.Cycle);
        AttachOverlay(layer,100);Window.Dispatcher.BeginInvoke(DispatcherPriority.Input,(Action)delegate{if(editorLayer==layer)Keyboard.Focus(EditorWheel);});
    }
    public bool ApplySeed() {
        try {string seed=ColorLogic.Normalize(Get<TextBox>("Seed").Text);RememberChange();Current.Seed=seed;Current.Variation=0;ColorLogic.Generate(Current);SelectRole(-1);Store.AddRecent(Current.Seed);AddSnapshot();Render();Persist();Status(Store.Warning??"Palette generated. Locked colors were preserved.");return true;}
        catch(ArgumentException e){Status(e.Message);Get<TextBox>("Seed").Focus();return false;}
    }
    void SetMode(bool dark) {Current.Dark=dark;ColorLogic.Generate(Current);Render();Persist();}
    public void SavePalette() {
        string name=Get<TextBox>("PaletteName").Text.Trim();if(name.Length==0){Status("Give this palette a name before saving.");Get<TextBox>("PaletteName").Focus();return;}
        if(Store.ReadOnly){Status(Store.Warning);return;}
        Current.Name=name;Current.Favorite=Get<CheckBox>("Favorite").IsChecked==true;
        int i=Store.Data.Palettes.FindIndex(p=>string.Equals(p.Name,name,StringComparison.OrdinalIgnoreCase));
        if(i>=0&&MessageBox.Show(Window,"Replace the saved palette ‘"+name+"’?","Save palette",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
        RememberChange();if(i>=0)Store.Data.Palettes[i]=Current.Clone();else Store.Data.Palettes.Add(Current.Clone());
        Persist();RenderLibrary();if(Store.Warning==null)Status("Saved locally: "+name);
    }
    public bool SetShortcut(string value,bool persist) {
        uint modifiers,key;if(!Native.Hotkey(value,out modifiers,out key)){Status("Use a modifier and A–Z or F1–F24, for example Ctrl+Shift+C.");return false;}
        Native.UnregisterHotKey(handle,1);
        if(!Native.RegisterHotKey(handle,1,modifiers,key)) {
            if(activeShortcut!=null){uint oldModifiers,oldKey;Native.Hotkey(activeShortcut,out oldModifiers,out oldKey);Native.RegisterHotKey(handle,1,oldModifiers,oldKey);}
            Status("Shortcut is unavailable or already in use. Previous shortcut retained; the Pick button still works.");return false;
        }
        activeShortcut=value;Store.Data.Shortcut=value;Get<TextBlock>("ShortcutHint").Text=value;
        if(persist){Persist();if(Store.Warning==null)Status("Global shortcut updated: "+value);}return true;
    }
    IntPtr Hook(IntPtr hwnd,int message,IntPtr w,IntPtr l,ref bool handled) {if(message==0x312&&w.ToInt32()==1){handled=true;StartPicker();}return IntPtr.Zero;}
    public async void StartPicker() {
        if(ActivePicker!=null)return;
        CloseEditor();CloseUtility();int target=selectedRole;
        var previous=Window.WindowState;
        ActivePicker=new Picker(delegate(string selected){ActivePicker=null;Window.Show();Window.WindowState=previous;Window.Activate();
            if(selected!=null){RememberChange();if(target<0){Current.Seed=selected;Current.Variation=0;ColorLogic.Generate(Current);AddSnapshot();}else Current.Colors[target]=selected;Store.AddRecent(selected);ShowPage(false);Render();Persist();Copy(selected,"Copied "+selected+" · "+(target<0?"Palette generated; locked colors preserved.":ColorLogic.Roles[target]+" updated. Other roles unchanged."));}
            else Status("Selection cancelled. Clipboard unchanged.");},target<0?"Base color":ColorLogic.Roles[target]);
        Window.Hide();
        try {
            Native.DwmFlush();await System.Threading.Tasks.Task.Delay(120);
            if(ActivePicker!=null)ActivePicker.Start();
        }catch(Exception e){if(ActivePicker!=null){ActivePicker.Dispose();ActivePicker=null;}Window.Show();Window.WindowState=previous;Window.Activate();Status("Screen capture failed: "+e.Message);}
    }
    public void Render() {
        CloseEditor();
        rendering=true;
        Get<TextBox>("Seed").Text=Current.Seed;
        Get<Button>("Light").Background=UiBrush(Current.Dark?"#F5F5F8":"#E9E5FA");Get<Button>("Dark").Background=UiBrush(Current.Dark?"#E9E5FA":"#F5F5F8");
        var panel=Get<System.Windows.Controls.Primitives.UniformGrid>("Colors");panel.Children.Clear();
        for(int i=0;i<5;i++) {
            int index=i;var column=new StackPanel();
            var frame=new Border {Child=column,BorderThickness=new Thickness(3),CornerRadius=new CornerRadius(12),Padding=new Thickness(7),Margin=new Thickness(0,0,i==4?0:4,0)};roleFrames[i]=frame;
            var badge=new TextBlock {Text=" ",FontSize=9,FontWeight=FontWeights.Bold,Foreground=UiBrush("#5542A7"),Margin=new Thickness(0,0,0,7)};roleBadges[i]=badge;column.Children.Add(badge);
            var swatch=new Button {Background=Brush(Current.Colors[i]),Height=58,Padding=new Thickness(0),ToolTip="Copy HEX and edit "+ColorLogic.Roles[i]};roleSwatches[i]=swatch;
            System.Windows.Automation.AutomationProperties.SetName(swatch,"Copy and edit "+ColorLogic.Roles[i]);
            swatch.Click+=delegate{OpenColorEditor(index);CopyRole(index);};column.Children.Add(swatch);
            column.Children.Add(new TextBlock {Text=ColorLogic.Roles[i],FontSize=11,Margin=new Thickness(0,11,0,6),FontWeight=FontWeights.SemiBold});
            var input=new TextBox {Text=Current.Colors[i],FontFamily=new FontFamily("Cascadia Mono, Consolas"),FontSize=12,Padding=new Thickness(6,7,6,7),MaxLength=20};
            bool fieldChanged=false;roleInputs[i]=input;input.GotKeyboardFocus+=delegate{fieldChanged=false;SelectRole(index);};
            System.Windows.Automation.AutomationProperties.SetName(input,ColorLogic.Roles[i]+" HEX");
            Action apply=delegate {if(rendering||roleInputs[index]!=input)return;try {string hex=ColorLogic.Normalize(input.Text);if(Current.Colors[index]!=hex){Current.Colors[index]=hex;swatch.Background=Brush(hex);UpdatePreview();SelectRole(index);Persist();}input.Text=hex;input.ClearValue(Control.BorderBrushProperty);}catch(ArgumentException e){input.BorderBrush=UiBrush("#B84132");Status(e.Message);}};
            input.LostKeyboardFocus+=delegate{apply();};input.KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Enter)apply();};column.Children.Add(input);
            input.TextChanged+=delegate {if(rendering||roleInputs[index]!=input)return;try{string hex=ColorLogic.Normalize(input.Text);if(Current.Colors[index]!=hex){if(!fieldChanged&&editorLayer==null){RememberChange();fieldChanged=true;}Current.Colors[index]=hex;swatch.Background=Brush(hex);UpdatePreview();SelectRole(index);Persist();}input.ClearValue(Control.BorderBrushProperty);}catch(ArgumentException){}};
            var locked=new CheckBox {Content="Lock regen",ToolTip="Preserve this role during regeneration; direct edits still work.",IsChecked=Current.Locks[i],FontSize=11,Margin=new Thickness(0,9,0,0)};System.Windows.Automation.AutomationProperties.SetName(locked,"Lock "+ColorLogic.Roles[i]+" during regeneration");locked.Click+=delegate{RememberChange();Current.Locks[index]=locked.IsChecked==true;SelectRole(index);UpdatePreview();Persist();};column.Children.Add(locked);
            var tools=new DockPanel {Margin=new Thickness(0,6,0,0)};var copyOnly=new Button {Content="Copy",MinHeight=30,Padding=new Thickness(4,2,4,2),FontSize=10,Background=UiBrush("#EFEDF8")};System.Windows.Automation.AutomationProperties.SetName(copyOnly,"Copy only "+ColorLogic.Roles[i]);copyOnly.Click+=delegate{CopyRole(index);};DockPanel.SetDock(copyOnly,Dock.Left);tools.Children.Add(copyOnly);var issue=new TextBlock {FontSize=10,VerticalAlignment=VerticalAlignment.Center,TextAlignment=TextAlignment.Right};roleIssues[i]=issue;var inspect=new Button {Content=issue,MinHeight=30,Padding=new Thickness(2),Background=Brush("#00FFFFFF"),ToolTip="View contrast pairs"};inspect.Click+=delegate{ShowPage(true);};tools.Children.Add(inspect);column.Children.Add(tools);panel.Children.Add(frame);
        }
        var history=Get<WrapPanel>("History");history.Children.Clear();
        if(Store.Data.Recent.Count==0)history.Children.Add(new TextBlock {Text="Your next pick starts here.",FontSize=11,Foreground=UiBrush("#646676"),Margin=new Thickness(3)});
        foreach(string hex in Store.Data.Recent.Take(5)) {string selected=hex;var b=new Button {Background=Brush(hex),Width=30,Height=30,MinHeight=30,Padding=new Thickness(0),Margin=new Thickness(0,0,6,6),ToolTip=hex};System.Windows.Automation.AutomationProperties.SetName(b,"Use recent color "+hex);b.Click+=delegate{Get<TextBox>("Seed").Text=selected;ApplySeed();};history.Children.Add(b);}
        UpdatePreview();SelectRole(selectedRole);ShowStorageWarning();UpdateSaveState();rendering=false;
        if(Window.IsLoaded&&SystemParameters.ClientAreaAnimation&&InputManager.Current.MostRecentInputDevice is MouseDevice)Get<Border>("PreviewBg").BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(.92,1,TimeSpan.FromMilliseconds(120)));
    }
    public void UpdatePreview() {
        string[] c=Current.Colors;
        Get<Border>("PreviewBg").Background=Brush(c[0]);Get<Border>("PreviewSurface").Background=Brush(c[1]);
        foreach(string n in new[]{"PreviewHeading","PreviewCardHeading","PreviewInputLabel"})Get<TextBlock>(n).Foreground=Brush(c[2]);
        foreach(string n in new[]{"PreviewLabel","PreviewBody","PreviewCardBody"})Get<TextBlock>(n).Foreground=Brush(c[3]);
        Get<Button>("PreviewButton").Background=Brush(c[4]);Get<Button>("PreviewButton").Foreground=Brush(ColorLogic.ButtonText(Current));
        var input=Get<TextBox>("PreviewInput");input.Background=Brush(c[0]);input.Foreground=Brush(c[2]);input.CaretBrush=Brush(c[2]);input.BorderBrush=Brush(c[3]);input.SelectionBrush=Brush(c[4]);
        var contrast=Get<UniformGrid>("Contrast");contrast.Children.Clear();
        foreach(int t in new[]{2,3})foreach(int b in new[]{0,1})AddContrast(contrast,ColorLogic.Roles[t]+" / "+ColorLogic.Roles[b],ColorLogic.Contrast(c[t],c[b]),4.5);
        AddContrast(contrast,"Button label / Accent",ColorLogic.Contrast(ColorLogic.ButtonText(Current),c[4]),4.5);
        AddContrast(contrast,"Accent / Background (non-text)",ColorLogic.Contrast(c[4],c[0]),3);
        var conflicts=ColorLogic.Conflicts(Current);var warning=Get<TextBlock>("Conflicts");warning.Text=conflicts.Count==0?"All listed pairs meet their thresholds.":conflicts.Count+" contrast checks fail. Review FAIL results; locked values are preserved.";warning.ToolTip=string.Join("\n",conflicts);
        for(int i=0;i<5;i++)if(roleIssues[i]!=null){double ratio=i<2?Math.Min(ColorLogic.Contrast(c[2],c[i]),ColorLogic.Contrast(c[3],c[i])):i<4?Math.Min(ColorLogic.Contrast(c[i],c[0]),ColorLogic.Contrast(c[i],c[1])):ColorLogic.Contrast(c[4],c[0]);bool pass=ratio>=(i==4?3:4.5);roleIssues[i].Text=(pass?"✓ ":"! ")+ratio.ToString("0.0")+":1";roleIssues[i].Foreground=UiBrush(pass?"#346346":"#934521");roleIssues[i].ToolTip=i==4?"Accent/background non-text boundary: 3:1":"Lowest relevant text-pair ratio: 4.5:1";}
    }
    void AddContrast(Panel panel,string label,double ratio,double threshold) {
        bool pass=ratio>=threshold;panel.Children.Add(new TextBlock {Text=label+"   "+ratio.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+":1 "+(pass?"PASS":"FAIL"),Foreground=UiBrush(pass?"#346346":"#934521"),FontSize=11,Margin=new Thickness(0,0,20,10)});
    }
    public void RenderLibrary() {
        var panel=Get<StackPanel>("Saved");panel.Children.Clear();string query=Get<TextBox>("LibrarySearch").Text;var found=Store.Data.Palettes.Where(p=>(Get<CheckBox>("FavoritesOnly").IsChecked!=true||p.Favorite)&&p.Name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0);var records=(Get<ComboBox>("LibrarySort").SelectedIndex==1?found.OrderBy(p=>p.Name):found.Reverse()).ToArray();
        if(records.Length==0)panel.Children.Add(new TextBlock {Text=Store.Data.Palettes.Count==0?"No palettes yet.\nImport or save one.":"No matches.\nClear search or favorites.",Foreground=UiBrush("#646676"),FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(3,5,0,0)});
        foreach(var palette in records) {var p=palette;var row=new DockPanel();var more=new Button {Content="⋯",Width=32,MinHeight=32,Padding=new Thickness(0),Background=Brush("#00FFFFFF"),ToolTip="Rename, duplicate or delete "+p.Name};DockPanel.SetDock(more,Dock.Right);more.Click+=delegate{PaletteMenu(p,more);};row.Children.Add(more);var b=new Button {Content=new TextBlock {Text=(p.Favorite?"★  ":"")+p.Name,TextTrimming=TextTrimming.CharacterEllipsis},Padding=new Thickness(8,8,8,8),HorizontalContentAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,6),ToolTip="Open "+p.Name};b.Click+=delegate{RememberChange();Current=p.Clone();SyncCurrent();Persist();Status("Opened "+p.Name);};row.Children.Add(b);panel.Children.Add(row);}
    }
}
public static class Program {
    [STAThread] public static int Main(string[] args) {
        if(args.Contains("--self-test"))return Tests.Run();
        bool feature=args.Contains("--feature-test");bool integration=args.Contains("--integration-test")||feature;
        string path=integration?Path.Combine(Environment.CurrentDirectory,".test-data","integration-"+Guid.NewGuid().ToString("N")+".json"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3.14","library.json");
        try {
            bool first;using(var instance=new System.Threading.Mutex(true,integration?"Local\\PiStudio.Integration":"Local\\PiStudio.3.14",out first)) {
                if(!first){MessageBox.Show("3.14 is already running. Use its global shortcut or open window.","3.14");return 0;}
                try {
                    var app=new Application {ShutdownMode=ShutdownMode.OnMainWindowClose};var studio=new Studio(path);app.MainWindow=studio.Window;
                    if(integration)studio.Window.Loaded+=delegate {if(feature)Tests.Features(studio);else Tests.Integration(studio);};
                    int code=app.Run(studio.Window);return integration&&Tests.IntegrationFailed?1:code;
                }finally {instance.ReleaseMutex();}
            }
        }catch(Exception e){File.WriteAllText(Path.Combine(Path.GetTempPath(),"3.14-error.txt"),e.ToString());MessageBox.Show("3.14 could not start. "+e.Message,"3.14");return 1;}
    }
}
}


