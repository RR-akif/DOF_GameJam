using UnityEngine;
using UnityEditor;
using System.IO;
namespace CircuitPuzzle.Editor {
public static class PuzzleKitSetup {
    const string Folder="Assets/CircuitPuzzle/Generated";
    [MenuItem("Tools/Circuit Puzzle/1. Generate Prefab and Sample Levels")]
    public static void Generate(){
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        bool createA=!File.Exists(Folder+"/01_FirstConnection.asset");
        var a=GetLevel("01_FirstConnection",5,5);
        if(createA){a.cells[0]=new Cell(Element.Start);a.cells[2]=new Cell(Element.Dot);a.cells[4]=new Cell(Element.End);a.cells[12]=new Cell(Element.Fail);EditorUtility.SetDirty(a);}
        bool createB=!File.Exists(Folder+"/02_TwoColors.asset");
        var b=GetLevel("02_TwoColors",5,5);
        if(createB){
            b.cells[0]=new Cell(Element.Start,Ink.Red);b.cells[2]=new Cell(Element.Dot,Ink.Red);b.cells[4]=new Cell(Element.End,Ink.Red);
            b.cells[20]=new Cell(Element.Start,Ink.Blue);b.cells[22]=new Cell(Element.Dot);b.cells[24]=new Cell(Element.End);
            b.cells[12]=new Cell(Element.Fail);b.cells[1]=new Cell(Element.Fail,Ink.Blue);b.cells[21]=new Cell(Element.Fail,Ink.Red);EditorUtility.SetDirty(b);
        }
        string path=Folder+"/PuzzlePopup.prefab";
        if(!File.Exists(path)){var root=new GameObject("PuzzlePopup");var popup=root.AddComponent<PuzzlePopup>();popup.defaultLevel=a;PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(path);EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("Puzzle kit ready. Drag Generated/PuzzlePopup.prefab into your scene and press Play. UI is built by the prefab at runtime.");
    }
    static PuzzleLevel GetLevel(string name,int w,int h){
        string path=Folder+"/"+name+".asset";var l=AssetDatabase.LoadAssetAtPath<PuzzleLevel>(path);if(l!=null)return l;
        l=ScriptableObject.CreateInstance<PuzzleLevel>();l.title=name.Replace('_',' ');l.width=w;l.height=h;l.cells=new Cell[w*h];AssetDatabase.CreateAsset(l,path);return l;
    }
    [MenuItem("Tools/Circuit Puzzle/2. Run Rule Checks")]
    public static void Check(){
        var l=ScriptableObject.CreateInstance<PuzzleLevel>();l.width=5;l.height=2;l.cells=new Cell[10];
        l.cells[0]=new Cell(Element.Start,Ink.Red);l.cells[4]=new Cell(Element.End);l.cells[2]=new Cell(Element.Dot,Ink.Red);
        var s=new PuzzleState(l);Require(s.Select(Vector2Int.zero),"select");Require(!s.Step(new Vector2Int(1,1)),"diagonal blocked");
        l.cells[1]=new Cell(Element.Fail);Require(!s.Step(Vector2Int.right),"white fail");
        l.cells[1]=new Cell(Element.Fail,Ink.Red);Require(!s.Step(Vector2Int.right),"same-color fail");
        l.cells[1]=new Cell(Element.Fail,Ink.Blue);Require(s.Step(Vector2Int.right),"other-color fail traversable");
        l.cells[2]=new Cell(Element.Dot,Ink.Blue);Require(!s.Step(new Vector2Int(2,0)),"wrong dot");l.cells[2]=new Cell(Element.Dot,Ink.Red);
        Require(s.Step(new Vector2Int(2,0)),"correct dot");Require(s.Step(new Vector2Int(3,0)),"advance");
        l.cells[4]=new Cell(Element.End,Ink.Blue);Require(!s.Step(new Vector2Int(4,0)),"wrong end");l.cells[4]=new Cell(Element.End);
        Require(s.Step(new Vector2Int(4,0)) && s.Solved,"white end and victory");
        Require(s.Select(new Vector2Int(2,0)) && !s.Solved,"reselect truncates");Require(s.Step(Vector2Int.right),"backtrack");
        Require(s.paths[0].Count==2,"backtrack length");
        s.Step(new Vector2Int(1,1));s.Step(new Vector2Int(2,1));s.Step(new Vector2Int(3,1));s.Step(new Vector2Int(4,1));s.Step(new Vector2Int(4,0));Require(!s.Solved,"missing dot prevents victory");
        l.cells[5]=new Cell(Element.Start,Ink.Blue);l.cells[9]=new Cell(Element.End,Ink.Blue);s=new PuzzleState(l);
        s.Select(Vector2Int.zero);s.Step(Vector2Int.right);s.Select(new Vector2Int(0,1));s.Step(new Vector2Int(1,1));Require(!s.Step(Vector2Int.right),"line overlap blocked");
        Object.DestroyImmediate(l);Debug.Log("Circuit Puzzle: all rule checks passed.");
    }
    static void Require(bool condition,string label){if(!condition)throw new System.Exception("Puzzle rule check failed: "+label);}
}
}
