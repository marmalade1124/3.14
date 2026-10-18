using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace PiStudio {
public class Palette {
    public string Name { get; set; }
    public string Seed { get; set; }
    public bool Dark { get; set; }
    public int Variation { get; set; }
    public string[] Colors { get; set; }
    public bool[] Locks { get; set; }
    public bool Favorite { get; set; }
    public Palette() { Name = "Untitled palette"; Seed = "#7160D4"; Colors = new string[5]; Locks = new bool[5]; }
    public Palette Clone() { return new Palette { Name=Name, Seed=Seed, Dark=Dark, Variation=Variation, Colors=(string[])Colors.Clone(), Locks=(bool[])Locks.Clone(), Favorite=Favorite }; }
}
public static partial class ColorLogic {
    public static readonly string[] Roles = { "Background", "Surface", "Primary text", "Secondary text", "Accent" };
    public static readonly string[] Tokens = { "background", "surface", "text-primary", "text-secondary", "accent" };
    public static string Normalize(string value) {
        string s = (value ?? "").Trim(); if(s.StartsWith("#"))s=s.Substring(1);
        if (!Regex.IsMatch(s, "\\A(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})\\z")) throw new ArgumentException("Enter a 3- or 6-digit HEX color, such as #7160D4.");
        if (s.Length == 3) s = string.Concat(s.Select(c => new string(c,2)));
        return "#" + s.ToUpperInvariant();
    }
    public static Color Parse(string hex) { return ColorTranslator.FromHtml(Normalize(hex)); }
    public static string Hex(Color c) { return string.Format("#{0:X2}{1:X2}{2:X2}",c.R,c.G,c.B); }
    static double Channel(byte b) { double v=b/255.0; return v<=0.04045 ? v/12.92 : Math.Pow((v+0.055)/1.055,2.4); }
    public static double Luminance(string hex) { Color c=Parse(hex); return 0.2126*Channel(c.R)+0.7152*Channel(c.G)+0.0722*Channel(c.B); }
    public static double Contrast(string a,string b) { double x=Luminance(a),y=Luminance(b); return (Math.Max(x,y)+0.05)/(Math.Min(x,y)+0.05); }
    static string Hsl(double h,double s,double l) {
        h=(h%360+360)%360/60; double c=(1-Math.Abs(2*l-1))*s, x=c*(1-Math.Abs(h%2-1)), m=l-c/2;
        double r=0,g=0,b=0;
        if(h<1){r=c;g=x;} else if(h<2){r=x;g=c;} else if(h<3){g=c;b=x;} else if(h<4){g=x;b=c;} else if(h<5){r=x;b=c;} else {r=c;b=x;}
        return Hex(Color.FromArgb((int)Math.Round((r+m)*255),(int)Math.Round((g+m)*255),(int)Math.Round((b+m)*255)));
    }
    public static void Generate(Palette p) {
        p.Seed=Normalize(p.Seed); Color seed=Parse(p.Seed); double h=seed.GetHue()+(p.Variation%7)*11;
        double[] seedLch=ToOklch(p.Seed);h=seedLch[2]+(p.Variation%7)*11;
        string[] generated=p.Dark ? new[]{Oklch(.14,.014,h),Oklch(.21,.018,h),Oklch(.96,.01,h),Oklch(.76,.018,h),p.Variation==0?p.Seed:Oklch(.72,Math.Max(.06,Math.Min(.17,seedLch[1])),h)}
            : new[]{Oklch(.985,.008,h),Oklch(.94,.015,h),Oklch(.22,.025,h),Oklch(.46,.025,h),p.Variation==0?p.Seed:Oklch(.58,Math.Max(.06,Math.Min(.17,seedLch[1])),h)};
        for(int i=0;i<5;i++) if(!p.Locks[i]) p.Colors[i]=generated[i];
    }
    public static string ButtonText(Palette p) { return Contrast("#FFFFFF",p.Colors[4])>=Contrast("#111111",p.Colors[4]) ? "#FFFFFF" : "#111111"; }
    public static List<string> Conflicts(Palette p) {
        var result=new List<string>();
        foreach(int t in new[]{2,3}) foreach(int b in new[]{0,1}) if(Contrast(p.Colors[t],p.Colors[b])<4.5)
            result.Add(Roles[t]+" / "+Roles[b]+" falls below 4.5:1"+(p.Locks[t]||p.Locks[b]?"; locked values preserved.":"."));
        if(Contrast(p.Colors[4],p.Colors[0])<3) result.Add("Accent / Background falls below 3:1 for essential non-text boundaries.");
        return result;
    }
    public static string Css(Palette p) { var s=new StringBuilder(":root {\n"); for(int i=0;i<5;i++) s.Append("  --"+Tokens[i]+": "+p.Colors[i]+";\n"); return s.Append("}\n").ToString(); }
    public static string Json(Palette p) { return new JavaScriptSerializer().Serialize(new { version=1, name=p.Name, seed=p.Seed, mode=p.Dark?"dark":"light", colors=Tokens.Select((t,i)=>new {t,i}).ToDictionary(x=>x.t,x=>p.Colors[x.i]), locks=Tokens.Select((t,i)=>new {t,i}).ToDictionary(x=>x.t,x=>p.Locks[x.i]) }); }
    public static void Validate(Palette p) {
        if(p==null||p.Name==null||p.Name.Length>100||p.Colors==null||p.Colors.Length!=5||p.Locks==null||p.Locks.Length!=5||p.Variation<0) throw new ArgumentException("Invalid palette record.");
        p.Seed=Normalize(p.Seed); for(int i=0;i<5;i++) p.Colors[i]=Normalize(p.Colors[i]);
    }
}
}
