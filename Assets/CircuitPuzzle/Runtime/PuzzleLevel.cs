using System;
using System.Collections.Generic;
using UnityEngine;
namespace CircuitPuzzle {
public enum Element { Empty, Start, End, Fail, Dot }
public enum Ink { White, Red, Blue, Gold, Green }
[Serializable] public struct Cell {
    public Element element;
    public Ink ink;
    public Cell(Element e, Ink i = Ink.White) { element=e; ink=i; }
}
[CreateAssetMenu(menuName="Circuit Puzzle/Level")]
public class PuzzleLevel : ScriptableObject {
    public string title = "CIRCUIT / 01";
    [Range(2,12)] public int width=5, height=5;
    [HideInInspector] public Cell[] cells = new Cell[25];
    public Cell At(int x,int y) { int i=y*width+x; return cells!=null && i<cells.Length ? cells[i] : default(Cell); }
    public List<string> Problems() {
        var errors=new List<string>();
        if(width<2 || width>12 || height<2 || height>12) errors.Add("Grid dimensions must be 2–12.");
        if(cells==null || cells.Length!=width*height) { errors.Add("Apply the grid size in the level editor."); return errors; }
        int starts=0,ends=0;
        foreach(var c in cells) { if(c.element==Element.Start) starts++; if(c.element==Element.End) ends++; }
        if(starts==0) errors.Add("Place at least one Start.");
        if(starts!=ends) errors.Add("Use the same number of Starts and Ends.");
        foreach(var c in cells) if(c.element==Element.Start) {
            bool match=false; foreach(var e in cells) if(e.element==Element.End && (e.ink==Ink.White || e.ink==c.ink)) match=true;
            if(!match) errors.Add("A "+c.ink+" start has no compatible end.");
        }
        foreach(var c in cells) if(c.element==Element.Dot && c.ink!=Ink.White) {
            bool match=false; foreach(var s in cells) if(s.element==Element.Start && s.ink==c.ink) match=true;
            if(!match) errors.Add("A "+c.ink+" dot has no same-colored start.");
        }
        return errors;
    }
    public static Color ColorFor(Ink i) {
        switch(i) {
            case Ink.Red:return new Color(1,.24f,.3f);
            case Ink.Blue:return new Color(.2f,.6f,1);
            case Ink.Gold:return new Color(1,.78f,.25f);
            case Ink.Green:return new Color(.28f,1,.64f);
            default:return new Color(.9f,.95f,1);
        }
    }
}
}
