using UnityEditor;
using UnityEngine;

namespace RollTheBall.Editor
{
    [CustomEditor(typeof(RollTheBallPuzzleController))]
    public sealed class RollTheBallPuzzleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var puzzle = (RollTheBallPuzzleController)target;
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("StartPuzzle()")) puzzle.StartPuzzle();
                using (new EditorGUI.DisabledScope(!puzzle.IsOpen || puzzle.IsRolling || !puzzle.ShowDebugControls))
                    if (GUILayout.Button("Debug: Auto-Solve")) puzzle.DebugAutoSolve();
                using (new EditorGUI.DisabledScope(!puzzle.IsOpen))
                    if (GUILayout.Button("CancelPuzzle()")) puzzle.CancelPuzzle();
            }
            EditorGUILayout.HelpBox("Drop the prefab in any scene, enter Play Mode, and click Test: Roll the Ball. All popup visuals are generated when opened.", MessageType.Info);
        }
    }

    public static class RollTheBallPrefabTools
    {
        private const string Root = "Assets/RollTheBall/";

        [MenuItem("GameObject/Puzzles/Roll the Ball", false, 10)]
        public static void CreateInstance()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/RollTheBallPuzzle.prefab");
            if (!prefab) { Debug.LogError("RollTheBallPuzzle.prefab was not found. Import the complete package."); return; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Roll the Ball puzzle");
            Selection.activeGameObject = instance;
        }

        [MenuItem("Tools/Roll the Ball/Validate Default Layout")]
        public static void ValidateDefault()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "Layouts/DefaultLayout.json");
            RollTheBallBoard.ValidatePlayable(RollTheBallLayout.FromJson(json.text));
            Debug.Log("Roll the Ball: default layout is unsolved and its six legal slides reach the goal.");
        }

        [MenuItem("Tools/Roll the Ball/Rebuild Prefab")]
        public static void RebuildPrefab()
        {
            ValidateDefault();
            var go = new GameObject("RollTheBallPuzzle");
            try
            {
                var controller = go.AddComponent<RollTheBallPuzzleController>();
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("layoutJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "Layouts/DefaultLayout.json");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, Root + "Prefabs/RollTheBallPuzzle.prefab");
            }
            finally { Object.DestroyImmediate(go); }
            AssetDatabase.SaveAssets();
        }
    }

    [CustomEditor(typeof(RollTheBallLayoutAsset))]
    public sealed class RollTheBallLayoutAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!GUILayout.Button("Validate Layout + Solution")) return;
            try
            {
                RollTheBallBoard.ValidatePlayable(((RollTheBallLayoutAsset)target).layout);
                Debug.Log("Layout and legal solution are valid.", target);
            }
            catch (System.Exception error) { Debug.LogException(error, target); }
        }
    }
}
