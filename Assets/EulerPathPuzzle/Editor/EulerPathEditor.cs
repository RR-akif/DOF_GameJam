using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DegreesOfFreedom.EulerPath.Editor
{
    [CustomEditor(typeof(EulerPathPuzzleController))]
    public sealed class EulerPathControllerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("All popup visuals are generated at runtime. Keep the prefab host active. Use the test button or F8 in Play Mode.", MessageType.Info);
            var puzzle = (EulerPathPuzzleController)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("TEST: StartPuzzle()")) puzzle.StartPuzzle();
                if (GUILayout.Button("CancelPuzzle()")) puzzle.CancelPuzzle();
                if (GUILayout.Button("Restart puzzle")) puzzle.RestartPuzzle();
            }
            if (GUILayout.Button("Validate assigned layout"))
                EulerPathEditorTools.Validate(puzzle.LayoutAsset != null ? puzzle.LayoutAsset.layout : EulerPathDefaultLayout.Create(), puzzle);
        }
    }

    [CustomEditor(typeof(EulerPathLayoutAsset))]
    public sealed class EulerPathLayoutInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Validate Euler rule, connectivity, and crossings"))
            {
                var asset = (EulerPathLayoutAsset)target;
                EulerPathEditorTools.Validate(asset.layout, asset);
            }
        }
    }

    public static class EulerPathEditorTools
    {
        public const string Root = "Assets/EulerPathPuzzle";
        public const string PrefabPath = Root + "/Prefabs/EulerPathPuzzle.prefab";
        public const string LayoutPath = Root + "/Layouts/OverlappingTriangles.asset";

        public static void Validate(EulerPathLayout data, Object context)
        {
            string error;
            if (!EulerPathValidator.IsValidEulerLayout(data, out error)) { Debug.LogError(error, context); return; }
            int odd = 0;
            foreach (var degree in EulerPathValidator.Degrees(data).Values) if (degree % 2 == 1) odd++;
            Debug.Log("Valid Euler layout: " + data.nodes.Length + " nodes, " + data.edges.Length + " edges, " + odd + " odd-degree nodes; connected.", context);
        }

        [MenuItem("Tools/Euler Path/Add prefab to current scene")]
        public static void AddPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) { Debug.LogError("Missing " + PrefabPath + ". Import the complete package."); return; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add Euler Path puzzle");
            Selection.activeGameObject = instance;
        }

        [MenuItem("Tools/Euler Path/Validate all layouts")]
        public static void ValidateAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EulerPathLayoutAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<EulerPathLayoutAsset>(AssetDatabase.GUIDToAssetPath(guid));
                Validate(asset.layout, asset);
            }
        }
    }

    // Invalid authored layouts fail the build, not just an Inspector warning.
    public sealed class EulerPathBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EulerPathLayoutAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EulerPathLayoutAsset>(path);
                string error;
                if (!EulerPathValidator.IsValidEulerLayout(asset.layout, out error))
                    throw new BuildFailedException("Invalid Euler Path layout at " + path + ": " + error);
            }
        }
    }
}
