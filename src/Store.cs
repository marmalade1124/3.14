using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace PiStudio {
public class Library {
    public int Version {get;set;}
    public List<Palette> Palettes {get;set;}
    public List<string> Recent {get;set;}
    public string Shortcut {get;set;}
    public Palette Draft {get;set;}
    public List<Palette> Snapshots {get;set;}
    public string Theme {get;set;}
    public bool HintDismissed {get;set;}
    public Library() { Version=1; Palettes=new List<Palette>(); Recent=new List<string>(); Snapshots=new List<Palette>(); Theme="Light"; Shortcut="Ctrl+Shift+C"; }
}
public class Store {
    public readonly string PathName;
    public string Warning;
    public bool ReadOnly;
    public Library Data;
    public Store(string path) { PathName=path; Load(); }
    void Load() {
        Data=new Library();
        if(!File.Exists(PathName)) return;
        try {
            Data=ReadLibrary(File.ReadAllText(PathName));
        } catch(Exception e) { ReadOnly=true; Warning="Library could not be read. Original file preserved; saving is disabled. Restore a valid copy and restart. "+e.Message+" File: "+PathName; }
    }
    public static Library ReadLibrary(string json) {
            var serializer=new JavaScriptSerializer();
            var fields=serializer.DeserializeObject(json) as Dictionary<string,object>;
            if(fields==null||!new[]{"Version","Palettes","Recent","Shortcut"}.All(fields.ContainsKey))throw new InvalidDataException("Required library fields are missing.");
            if(!(fields["Version"] is int)||(int)fields["Version"]!=1)throw new InvalidDataException("Unsupported library version.");
            var loaded=serializer.Deserialize<Library>(json);
            if(loaded==null||loaded.Version!=1||loaded.Palettes==null||loaded.Recent==null||loaded.Shortcut==null||loaded.Palettes.Count>10000||loaded.Recent.Count>1000) throw new InvalidDataException("Unrecognized library format.");
            foreach(var p in loaded.Palettes) {ColorLogic.Validate(p);if(string.IsNullOrWhiteSpace(p.Name))throw new InvalidDataException("A saved palette has no name.");}
            uint mods,key;if(!Native.Hotkey(loaded.Shortcut,out mods,out key))throw new InvalidDataException("Invalid stored shortcut.");
            if(loaded.Snapshots==null||loaded.Snapshots.Count>100||!new[]{"System","Light","Dark"}.Contains(loaded.Theme))throw new InvalidDataException("Invalid library preferences or history.");
            loaded.Theme="Light"; // Preserve legacy libraries while retiring app appearance preferences.
            foreach(var p in loaded.Snapshots)ColorLogic.Validate(p);
            if(loaded.Draft!=null) ColorLogic.Validate(loaded.Draft);
            loaded.Recent=loaded.Recent.Select(ColorLogic.Normalize).Distinct().Take(24).ToList();
            return loaded;
    }
    public bool Restore(string json){var incoming=ReadLibrary(json);var previous=Data;bool readOnly=ReadOnly;try{if(File.Exists(PathName))File.Copy(PathName,PathName+".recovery-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff"),false);Data=incoming;ReadOnly=false;if(Save())return true;Data=previous;ReadOnly=readOnly;return false;}catch(Exception e){Data=previous;ReadOnly=readOnly;Warning="Restore failed; original library preserved: "+e.Message;return false;}}
    public bool Save() {
        if(ReadOnly) return false;
        string temp=PathName+".tmp";
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(PathName));
            string json=new JavaScriptSerializer().Serialize(Data);ReadLibrary(json);
            byte[] bytes=Encoding.UTF8.GetBytes(json);
            using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) { stream.Write(bytes,0,bytes.Length); stream.Flush(true); }
            if(File.Exists(PathName)) File.Replace(temp,PathName,PathName+".bak"); else File.Move(temp,PathName);
            Warning=null; return true;
        } catch(Exception e) { Warning="Could not save library: "+e.Message; return false; }
    }
    public void AddRecent(string hex) { hex=ColorLogic.Normalize(hex); Data.Recent.Remove(hex); Data.Recent.Insert(0,hex); Data.Recent=Data.Recent.Take(24).ToList(); }
}
}
