using System.Collections.Generic;
using UnityEngine;
namespace CircuitPuzzle {
// Pure routing state: no scene references, UI dependencies, or asset mutations.
public class PuzzleState {
    public readonly PuzzleLevel level;
    public readonly List<List<Vector2Int>> paths=new List<List<Vector2Int>>();
    public readonly List<Ink> inks=new List<Ink>();
    public int active=-1;
    public PuzzleState(PuzzleLevel asset) {
        level=asset;
        for(int y=0;y<level.height;y++) for(int x=0;x<level.width;x++)
            if(level.At(x,y).element==Element.Start) { paths.Add(new List<Vector2Int>{new Vector2Int(x,y)}); inks.Add(level.At(x,y).ink); }
    }
    public int Owner(Vector2Int p) { for(int i=0;i<paths.Count;i++) if(paths[i].Contains(p)) return i; return -1; }
    public bool Select(Vector2Int p) {
        int owner=Owner(p); if(owner<0) {active=-1;return false;}
        active=owner; var path=paths[owner]; int index=path.IndexOf(p);
        path.RemoveRange(index+1,path.Count-index-1); return true;
    }
    public bool Step(Vector2Int p) {
        if(active<0 || p.x<0 || p.y<0 || p.x>=level.width || p.y>=level.height) return false;
        var path=paths[active]; var last=path[path.Count-1];
        if(Mathf.Abs(last.x-p.x)+Mathf.Abs(last.y-p.y)!=1) return false;
        int previous=path.IndexOf(p);
        if(previous>=0) {path.RemoveRange(previous+1,path.Count-previous-1);return true;}
        if(level.At(last.x,last.y).element==Element.End || Owner(p)>=0) return false;
        Cell cell=level.At(p.x,p.y); Ink ink=inks[active];
        if(cell.element==Element.Start) return false;
        if(cell.element==Element.Fail && (cell.ink==Ink.White || cell.ink==ink)) return false;
        if((cell.element==Element.End || cell.element==Element.Dot) && cell.ink!=Ink.White && cell.ink!=ink) return false;
        path.Add(p);return true;
    }
    public bool Solved {
        get {
            if(paths.Count==0) return false;
            foreach(var path in paths) {var p=path[path.Count-1];if(level.At(p.x,p.y).element!=Element.End) return false;}
            for(int y=0;y<level.height;y++) for(int x=0;x<level.width;x++) {
                var cell=level.At(x,y); int owner=Owner(new Vector2Int(x,y));
                if(cell.element==Element.End && owner<0) return false;
                if(cell.element==Element.Dot && (owner<0 || (cell.ink!=Ink.White && inks[owner]!=cell.ink))) return false;
            }
            return true;
        }
    }
}
}
