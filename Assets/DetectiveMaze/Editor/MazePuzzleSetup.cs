using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Run inside Unity to create real serialized assets, with that project's own Unity version.</summary>
public static class MazePuzzleSetup
{
    private const string Root = "Assets/DetectiveMaze";
    private const string ResourcesRoot = Root + "/Resources/DetectiveMaze";

    [MenuItem("Tools/Detective Maze/Build Prefab and Test Scene", false, 10)]
    public static void BuildPrefabAndTestScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Stop Play mode before building the maze assets.");
            return;
        }
#if !ENABLE_INPUT_SYSTEM
        Debug.LogError("Set Edit > Project Settings > Player > Active Input Handling to Both, then restart Unity.");
        return;
#else
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            EnsureFolder(Root + "/Generated");
            ImportGeneratedArt();
            int layer = EnsureLayer();
            CreateMaterials();
            RenderTexture texture = CreateRenderTexture();
            GameObject prefab = CreatePrefab(layer, texture);
            ValidatePrefab(prefab);
            CreateTestScene(prefab, layer);
            AssetDatabase.SaveAssets();
            Debug.Log("Detective Maze is assembled. Press Play: only the popup opens. Reach the open door to close it. WASD / arrows / left stick move; Escape / gamepad East cancels.", prefab);
        }
        catch (Exception error) { Debug.LogException(error); }
#endif
    }

    private static void ImportGeneratedArt()
    {
        string[] names = { "Wall", "Floor", "LockedDoor", "OpenDoor", "Detective", "PopupBackdrop", "LightGlow" };
        foreach (string name in names)
        {
            string path = ResourcesRoot + "/Art/" + name + ".png";
            if (!File.Exists(path)) throw new FileNotFoundException("Missing generated art", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Could not import " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1024f;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = name == "PopupBackdrop" ? 2048 : 512;
            importer.SaveAndReimport();
        }
    }

    private static int EnsureLayer()
    {
        int existing = LayerMask.NameToLayer(MazePuzzleController.LayerName);
        if (existing >= 0) return existing;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = settings.FindProperty("layers");
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty entry = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(entry.stringValue)) continue;
            entry.stringValue = MazePuzzleController.LayerName;
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return i;
        }
        throw new InvalidOperationException("All user layers are occupied. Free one slot for the reserved MazePuzzle layer, then rerun setup.");
    }

    private static void CreateMaterials()
    {
        EnsureFolder(ResourcesRoot + "/Materials");
        // Use Unity's sprite shaders, including their sprite texture / alpha handling.
        // Never use the old custom shared MazeSprite material for any artwork.
        string shaderName = GraphicsSettings.currentRenderPipeline == null
            ? "Sprites/Default" : "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
            throw new InvalidOperationException("The sprite shader is unavailable: " + shaderName + ". This package supports Built-in or URP rendering.");
        string[] artworkNames = { "Wall", "Floor", "LockedDoor", "OpenDoor", "Detective", "LightGlow" };
        foreach (string artworkName in artworkNames)
        {
            Sprite artwork = MazeBuilder.LoadSprite(artworkName);
            string path = ResourcesRoot + "/Materials/" + artworkName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = artworkName };
                AssetDatabase.CreateAsset(material, path);
            }
            // Repair existing generated materials too when this command is rerun.
            material.shader = shader;
            material.mainTexture = artwork.texture;
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(material);
        }
        string physicsPath = ResourcesRoot + "/MazeNoFriction.physicsMaterial2D";
        if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physicsPath) == null)
        {
            AssetDatabase.CreateAsset(new PhysicsMaterial2D("MazeNoFriction")
                { friction = 0f, bounciness = 0f }, physicsPath);
        }
        AssetDatabase.SaveAssets();
    }

    private static RenderTexture CreateRenderTexture()
    {
        string path = Root + "/Generated/MazeView.renderTexture";
        var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (existing != null) return existing;
        var texture = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32)
        {
            name = "MazeView", antiAliasing = 1, useMipMap = false, autoGenerateMips = false,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    private static GameObject CreatePrefab(int layer, RenderTexture texture)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            root = new GameObject("MazePuzzlePopup");
            SceneManager.MoveGameObjectToScene(root, preview);
            root.SetActive(false);
            MazePuzzleController controller = root.AddComponent<MazePuzzleController>();

            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 1000);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = NewUI("MazePanel", canvasObject.transform);
            Stretch(panelObject.GetComponent<RectTransform>());
            Image panel = panelObject.AddComponent<Image>();
            panel.sprite = null; // REQUIRED: controller applies generated backdrop in Awake.
            panel.color = new Color(1f, 1f, 1f, 0.92f);
            panel.raycastTarget = true;

            GameObject viewportObject = NewUI("RawImage", panelObject.transform);
            Stretch(viewportObject.GetComponent<RectTransform>()); // REQUIRED full-stretch, zero offsets.
            RawImage viewport = viewportObject.AddComponent<RawImage>();
            viewport.texture = texture;
            viewport.raycastTarget = true;

            GameObject cameraObject = new GameObject("PuzzleCamera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.layer = layer;
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << layer;
            camera.targetTexture = texture;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            GameObject mazeRoot = new GameObject("MazeRoot", typeof(MazeBuilder));
            mazeRoot.transform.SetParent(root.transform, false);
            mazeRoot.layer = layer;
            mazeRoot.transform.localPosition = new Vector3(10000f, 10000f, 0f);
            MazeBuilder builder = mazeRoot.GetComponent<MazeBuilder>();
            GameObject inputObject = new GameObject("PuzzleInputHandler", typeof(PuzzleInputHandler));
            inputObject.transform.SetParent(root.transform, false);
            PuzzleInputHandler input = inputObject.GetComponent<PuzzleInputHandler>();

            var serialized = new SerializedObject(controller);
            SetReference(serialized, "popupCanvas", canvas);
            SetReference(serialized, "mazePanel", panel);
            SetReference(serialized, "viewport", viewport);
            SetReference(serialized, "puzzleCamera", camera);
            SetReference(serialized, "mazeBuilder", builder);
            SetReference(serialized, "inputHandler", input);
            SetReference(serialized, "renderTextureTemplate", texture);
            serialized.FindProperty("openOnStart").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            builder.Build(controller, input, camera); // Complete editable preview hierarchy in the actual prefab.
            canvasObject.SetActive(false);
            mazeRoot.SetActive(false);
            root.SetActive(true); // Controller must stay active so Instance.StartPuzzle() is callable.
            string path = AssetDatabase.GenerateUniqueAssetPath(Root + "/Generated/MazePuzzlePopup.prefab");
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static void CreateTestScene(GameObject prefab, int layer)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var popup = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        var controller = popup.GetComponent<MazePuzzleController>();
        GameObject backgroundCamera = new GameObject("TestBackgroundCamera", typeof(Camera));
        Camera camera = backgroundCamera.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.03f, 0.045f);
        camera.cullingMask = ~(1 << layer);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        // No launch menu, buttons, title canvas, or game UI after completion.
        // Diagnostics are optional and run only from the component context menu.
        new GameObject("MazeDiagnostics", typeof(MazePuzzlePlayModeChecks));

        GameObject probes = new GameObject("TemporaryControllerProbes");
        MazeControllerProbe activeProbe = probes.AddComponent<MazeControllerProbe>();
        MazeControllerProbe disabledProbe = probes.AddComponent<MazeControllerProbe>();
        disabledProbe.enabled = false;
        var serialized = new SerializedObject(controller);
        SerializedProperty controllers = serialized.FindProperty("mainGameControllers");
        controllers.arraySize = 2;
        controllers.GetArrayElementAtIndex(0).objectReferenceValue = activeProbe;
        controllers.GetArrayElementAtIndex(1).objectReferenceValue = disabledProbe;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
        string path = AssetDatabase.GenerateUniqueAssetPath(Root + "/Generated/DetectiveMazeTest.unity");
        EditorSceneManager.SaveScene(scene, path);
        Selection.activeGameObject = popup;
    }

    [MenuItem("Tools/Detective Maze/Validate Selected Prefab", false, 20)]
    public static void ValidateSelectedPrefab()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || selected.GetComponent<MazePuzzleController>() == null)
        {
            Debug.LogError("Select the MazePuzzlePopup prefab asset or its scene root first.");
            return;
        }
        try { ValidatePrefab(selected); Debug.Log("Maze prefab structure and asset references passed validation.", selected); }
        catch (Exception error) { Debug.LogException(error, selected); }
    }

    private static void ValidatePrefab(GameObject root)
    {
        if (root == null) throw new InvalidOperationException("Prefab serialization failed.");
        MazeLayout.Validate(MazeLayout.CreateRows());
        int layer = LayerMask.NameToLayer(MazePuzzleController.LayerName);
        Transform maze = root.transform.Find("MazeRoot");
        Transform panel = root.transform.Find("Canvas/MazePanel");
        if (maze == null || panel == null || root.transform.Find("PuzzleInputHandler") == null)
            throw new InvalidOperationException("Required prefab hierarchy is missing.");
        RawImage viewport = panel.Find("RawImage").GetComponent<RawImage>();
        RectTransform rect = viewport.rectTransform;
        if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one ||
            rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero)
            throw new InvalidOperationException("RawImage must be fully stretched with zero offsets.");
        Camera camera = root.transform.Find("PuzzleCamera").GetComponent<Camera>();
        if (!camera.orthographic || camera.targetTexture == null || viewport.texture != camera.targetTexture || camera.cullingMask != (1 << layer))
            throw new InvalidOperationException("Camera, layer, or RenderTexture wiring is incorrect.");
        foreach (Transform child in maze.GetComponentsInChildren<Transform>(true))
            if (child.gameObject.layer != layer) throw new InvalidOperationException("Wrong maze layer: " + child.name);
        if (root.GetComponentInChildren<MeshRenderer>(true) != null || root.GetComponentInChildren<Collider>(true) != null ||
            root.GetComponentInChildren<CharacterController>(true) != null)
            throw new InvalidOperationException("Maze must use only sprites and 2D physics.");
        foreach (SpriteRenderer renderer in maze.GetComponentsInChildren<SpriteRenderer>(true))
            if (renderer.sprite == null || renderer.sharedMaterial == null)
                throw new InvalidOperationException("A maze sprite or material reference is missing.");
        ValidateArtwork(maze.Find("DetectivePlayer"), "Detective");
        ValidateArtwork(maze.Find("StartDoor"), "LockedDoor");
        ValidateArtwork(maze.Find("EndDoor"), "OpenDoor");
        if (root.GetComponentInChildren<Button>(true) != null)
            throw new InvalidOperationException("The popup must not contain a launch menu or test buttons.");
        if (maze.Find("Grid/Walls") == null || maze.Find("StartDoor") == null || maze.Find("EndDoor") == null ||
            maze.Find("DetectivePlayer").GetComponent<CircleCollider2D>() == null)
            throw new InvalidOperationException("Generated maze contents are incomplete.");
        if (PrefabUtility.IsPartOfPrefabAsset(root) && root.GetComponentInChildren<MazePuzzleTestButton>(true) != null)
            throw new InvalidOperationException("The temporary test button must not be in the prefab.");
    }

    private static void EnsureFolder(string path)
    {
        Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void ValidateArtwork(Transform target, string artworkName)
    {
        if (target == null) throw new InvalidOperationException("Missing artwork object: " + artworkName);
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        Sprite expected = MazeBuilder.LoadSprite(artworkName);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ResourcesRoot + "/Materials/" + artworkName + ".mat");
        if (renderer == null || renderer.sprite != expected || material == null ||
            renderer.sharedMaterial != material || material.mainTexture != expected.texture)
            throw new InvalidOperationException("Incorrect sprite/material binding for " + artworkName);
    }

    private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        serialized.FindProperty(name).objectReferenceValue = value;
    }
}
