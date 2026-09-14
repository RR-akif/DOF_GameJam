using UnityEngine;
using UnityEditor;
namespace CircuitPuzzle.Editor {
[CustomEditor(typeof(PuzzleLevel))]
public class PuzzleLevelEditor : UnityEditor.Editor {
    Element brush=Element.Start; Ink ink=Ink.White; int width,height;
    void OnEnable(){var l=(PuzzleLevel)target;width=l.width;height=l.height;}
    public override void OnInspectorGUI(){
        var l=(PuzzleLevel)target;serializedObject.Update();EditorGUILayout.PropertyField(serializedObject.FindProperty("title"));serializedObject.ApplyModifiedProperties();
        EditorGUILayout.LabelField("Grid size",EditorStyles.boldLabel);width=EditorGUILayout.IntSlider("Width",width,2,12);height=EditorGUILayout.IntSlider("Height",height,2,12);
        if(GUILayout.Button("Apply size (preserve overlapping cells)")){
            Undo.RecordObject(l,"Resize puzzle");var cells=new Cell[width*height];
            for(int y=0;y<Mathf.Min(height,l.height);y++)for(int x=0;x<Mathf.Min(width,l.width);x++)cells[y*width+x]=l.At(x,y);
            l.width=width;l.height=height;l.cells=cells;EditorUtility.SetDirty(l);
        }
        brush=(Element)EditorGUILayout.EnumPopup("Paint element",brush);ink=(Ink)EditorGUILayout.EnumPopup("Paint color",ink);
        EditorGUILayout.HelpBox("Click to paint. Right-click to erase. Bottom-left is (0,0). White is a wildcard for ends/dots and blocks all colors on fails. Starts always define their own line color.",MessageType.Info);
        float size=Mathf.Clamp((EditorGUIUtility.currentViewWidth-45)/l.width,18,45);
        for(int y=l.height-1;y>=0;y--){EditorGUILayout.BeginHorizontal();for(int x=0;x<l.width;x++){
            var cell=l.At(x,y);var r=GUILayoutUtility.GetRect(size,size,GUILayout.Width(size));
            Color old=GUI.color;GUI.color=PuzzleLevel.ColorFor(cell.ink);GUI.Box(r,new GUIContent(Symbol(cell.element),"("+x+", "+y+") "+cell.ink+" "+cell.element));GUI.color=old;
            var e=Event.current;if(e.type==EventType.MouseDown && r.Contains(e.mousePosition) && (e.button==0 || e.button==1)){
                Undo.RecordObject(l,"Paint puzzle");if(l.cells==null || l.cells.Length!=l.width*l.height)System.Array.Resize(ref l.cells,l.width*l.height);
                l.cells[y*l.width+x]=new Cell(e.button==1?Element.Empty:brush,ink);EditorUtility.SetDirty(l);e.Use();Repaint();
            }
        }EditorGUILayout.EndHorizontal();}
        if(GUILayout.Button("Validate layout")){var errors=l.Problems();EditorUtility.DisplayDialog("Layout validation",errors.Count==0?"Structural checks passed. Play-test to verify a solution exists.":string.Join("\n",errors),"OK");}
        if(Application.isPlaying && GUILayout.Button("Test this level in open scene")){
            var popup=Object.FindObjectOfType<PuzzlePopup>();if(popup!=null){popup.Close();popup.Open(l);}else Debug.LogWarning("Add the PuzzlePopup prefab to the scene first.");
        }
    }
    static string Symbol(Element e){switch(e){case Element.Start:return "■";case Element.End:return "□";case Element.Fail:return "X";case Element.Dot:return "●";default:return "·";}}
}
}
