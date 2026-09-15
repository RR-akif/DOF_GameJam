using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MazePuzzleController : MonoBehaviour
{
    public const string LayerName = "MazePuzzle";

    public static MazePuzzleController Instance;

    public event Action OnPuzzleSuccess;
    public event Action OnPuzzleClosed;

    public bool IsOpen { get; private set; }


    [Header("Prefab references (assigned by the setup command)")]
    [SerializeField] private Canvas popupCanvas;
    [SerializeField] private Image mazePanel;
    [SerializeField] private RawImage viewport;
    [SerializeField] private Camera puzzleCamera;
    [SerializeField] private MazeBuilder mazeBuilder;
    [SerializeField] private PuzzleInputHandler inputHandler;
    [SerializeField] private RenderTexture renderTextureTemplate;


    [Header("Startup")]
    [Tooltip(
        "Open the popup immediately when Play starts. " +
        "Disable only when a main-game trigger should open it later."
    )]
    [SerializeField] private bool openOnStart = true;


    [Header("Assign only when testing over main-game controls")]
    [Tooltip(
        "Drag EVERY movement/look/interaction input reader to this list, " +
        "including legacy scripts. No automatic discovery or changes to their code."
    )]
    [SerializeField] private MonoBehaviour[] mainGameControllers =
        Array.Empty<MonoBehaviour>();

    [SerializeField, Range(512, 2048)]
    private int maxTextureDimension = 1600;


    private readonly MazeInputIsolation inputIsolation =
        new MazeInputIsolation();

    private readonly MazeWorldIsolation worldIsolation =
        new MazeWorldIsolation();


    private RenderTexture runtimeTexture;

    private bool ready;
    private bool opening;
    private bool closing;
    private bool completing;

    private Vector2 lastViewportSize;


    // ======================================================
    // RESET SINGLETON
    // ======================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetSingleton()
    {
        Instance = null;
    }


    // ======================================================
    // AWAKE
    // ======================================================

    private void Awake()
    {
        Debug.Log(
            "MAZE DEBUG: Awake called on " + gameObject.name,
            this
        );


        // Prevent duplicate maze controllers.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "Only one active MazePuzzlePopup is supported. " +
                "Disabling the duplicate.",
                this
            );

            gameObject.SetActive(false);

            return;
        }


        Instance = this;


        try
        {
            RequireReferences();


            puzzleCamera.enabled = false;

            puzzleCamera.orthographic = true;

            puzzleCamera.clearFlags =
                CameraClearFlags.SolidColor;

            puzzleCamera.backgroundColor =
                new Color(0f, 0f, 0f, 0f);

            puzzleCamera.allowHDR = false;
            puzzleCamera.allowMSAA = false;


            popupCanvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            popupCanvas.sortingOrder = 30000;


            mazePanel.sprite =
                MazeBuilder.LoadSprite("PopupBackdrop");

            mazePanel.color =
                new Color(1f, 1f, 1f, 0.92f);

            mazePanel.raycastTarget = true;


            RectTransform rect = viewport.rectTransform;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            viewport.raycastTarget = true;


            inputHandler.DisableMazeInput();

            inputHandler.CancelRequested += ClosePuzzle;


            HideView();


            ready = true;


            Debug.Log(
                "MAZE DEBUG: Awake completed successfully. ready=true",
                this
            );
        }
        catch (Exception error)
        {
            ready = false;

            HideView();

            Debug.LogError(
                "MAZE DEBUG: Awake failed. ready=false",
                this
            );

            Debug.LogException(
                error,
                this
            );
        }
    }


    // ======================================================
    // START
    // ======================================================

    private void Start()
    {
        Debug.Log(
            "MAZE DEBUG: Start called. Open On Start = " +
            openOnStart,
            this
        );


        if (openOnStart)
        {
            StartPuzzle();
        }
    }


    // ======================================================
    // START PUZZLE
    // ======================================================

    public void StartPuzzle()
    {
        // --------------------------------------------------
        // IMPORTANT DEBUG INFORMATION
        // --------------------------------------------------

        Debug.Log(
            "MAZE DEBUG: StartPuzzle called | " +

            "ready=" + ready +

            ", active=" + isActiveAndEnabled +

            ", correctInstance=" + (Instance == this) +

            ", IsOpen=" + IsOpen +

            ", opening=" + opening +

            ", closing=" + closing +

            ", completing=" + completing +

            ", timeScale=" + Time.timeScale +

            ", Physics2D=" +
            Physics2D.simulationMode,

            this
        );


        // --------------------------------------------------
        // CHECK EARLY RETURN CONDITIONS
        // --------------------------------------------------

        if (!ready)
        {
            Debug.LogError(
                "MAZE DEBUG: Puzzle cannot start because ready=false.",
                this
            );

            return;
        }


        if (!isActiveAndEnabled)
        {
            Debug.LogError(
                "MAZE DEBUG: Puzzle cannot start because " +
                "MazePuzzleController is not active/enabled.",
                this
            );

            return;
        }


        if (Instance != this)
        {
            Debug.LogError(
                "MAZE DEBUG: Puzzle cannot start because " +
                "this controller is NOT the singleton Instance.",
                this
            );

            return;
        }


        if (IsOpen)
        {
            Debug.LogWarning(
                "MAZE DEBUG: Puzzle is already open.",
                this
            );

            return;
        }


        if (opening)
        {
            Debug.LogWarning(
                "MAZE DEBUG: Puzzle is already opening.",
                this
            );

            return;
        }


        if (closing)
        {
            Debug.LogWarning(
                "MAZE DEBUG: Puzzle is currently closing.",
                this
            );

            return;
        }


        if (completing)
        {
            Debug.LogWarning(
                "MAZE DEBUG: Puzzle is currently completing.",
                this
            );

            return;
        }


        // --------------------------------------------------
        // PHYSICS / TIME CHECK
        // --------------------------------------------------

        if (Time.timeScale <= 0f)
        {
            Debug.LogError(
                "MAZE DEBUG: Time.timeScale is <= 0. " +
                "The maze requires timeScale > 0.",
                this
            );

            return;
        }


        if (Physics2D.simulationMode !=
            SimulationMode2D.FixedUpdate)
        {
            Debug.LogError(
                "MAZE DEBUG: Physics 2D Simulation Mode must " +
                "be Fixed Update. Current mode = " +
                Physics2D.simulationMode,
                this
            );

            return;
        }


        opening = true;


        try
        {
            // --------------------------------------------------
            // CHECK MAZE LAYER
            // --------------------------------------------------

            int layer =
                LayerMask.NameToLayer(LayerName);


            Debug.Log(
                "MAZE DEBUG: MazePuzzle layer index = " +
                layer,
                this
            );


            if (layer < 0)
            {
                throw new InvalidOperationException(
                    "The MazePuzzle layer is missing. " +
                    "Run the Detective Maze setup menu."
                );
            }


            // --------------------------------------------------
            // OPEN PUZZLE
            // --------------------------------------------------

            IsOpen = true;


            Debug.Log(
                "MAZE DEBUG: Acquiring input isolation...",
                this
            );

            inputIsolation.Acquire(
                mainGameControllers,
                transform
            );


            Debug.Log(
                "MAZE DEBUG: Acquiring world isolation...",
                this
            );

            worldIsolation.Acquire(
                layer,
                puzzleCamera
            );


            // --------------------------------------------------
            // SHOW POPUP
            // --------------------------------------------------

            Debug.Log(
                "MAZE DEBUG: Enabling popup canvas...",
                this
            );

            popupCanvas.gameObject.SetActive(true);


            Canvas.ForceUpdateCanvases();


            // --------------------------------------------------
            // RENDER TEXTURE
            // --------------------------------------------------

            Debug.Log(
                "MAZE DEBUG: Updating render texture...",
                this
            );

            UpdateRenderTexture();


            // --------------------------------------------------
            // BUILD MAZE
            // --------------------------------------------------

            Debug.Log(
                "MAZE DEBUG: Building maze...",
                this
            );


            mazeBuilder.gameObject.SetActive(false);


            mazeBuilder.Build(
                this,
                inputHandler,
                puzzleCamera
            );


            mazeBuilder.gameObject.SetActive(true);


            // --------------------------------------------------
            // RESET MAZE DETECTIVE
            // --------------------------------------------------

            if (mazeBuilder.Player == null)
            {
                throw new InvalidOperationException(
                    "MazeBuilder.Player is null after Build()."
                );
            }


            mazeBuilder.Player.ResetTo(
                mazeBuilder.SpawnPosition
            );


            // --------------------------------------------------
            // ENABLE MAZE INPUT
            // --------------------------------------------------

            inputHandler.EnableMazeInput();


            // --------------------------------------------------
            // ENABLE PUZZLE CAMERA
            // --------------------------------------------------

            puzzleCamera.gameObject.SetActive(true);

            puzzleCamera.enabled = true;


            Debug.Log(
                "MAZE DEBUG: PUZZLE SUCCESSFULLY OPENED!",
                this
            );
        }
        catch (Exception error)
        {
            Debug.LogError(
                "MAZE DEBUG: Exception occurred while opening puzzle.",
                this
            );


            ClosePuzzle();


            Debug.LogException(
                error,
                this
            );
        }
        finally
        {
            opening = false;


            Debug.Log(
                "MAZE DEBUG: StartPuzzle finished. " +
                "IsOpen=" + IsOpen,
                this
            );
        }
    }


    // ======================================================
    // COMPLETE PUZZLE
    // ======================================================

    public void CompletePuzzle()
    {
        if (!IsOpen || completing || closing)
        {
            Debug.LogWarning(
                "MAZE DEBUG: CompletePuzzle ignored. " +
                "IsOpen=" + IsOpen +
                ", completing=" + completing +
                ", closing=" + closing,
                this
            );

            return;
        }


        completing = true;


        try
        {
            Debug.Log(
                "MAZE DEBUG: PUZZLE SUCCESS!",
                this
            );


            InvokeSafely(
                OnPuzzleSuccess
            );
        }
        finally
        {
            ClosePuzzle();

            completing = false;
        }
    }


    // ======================================================
    // CLOSE PUZZLE
    // ======================================================

    public void ClosePuzzle()
    {
        if (closing)
            return;


        bool wasOpen = IsOpen;


        IsOpen = false;

        closing = true;


        try
        {
            if (inputHandler != null)
            {
                inputHandler.DisableMazeInput();
            }


            if (mazeBuilder != null &&
                mazeBuilder.Player != null)
            {
                mazeBuilder.Player.StopMoving();
            }


            HideView();


            worldIsolation.Release();

            inputIsolation.Release();


            if (wasOpen)
            {
                InvokeSafely(
                    OnPuzzleClosed
                );
            }


            Debug.Log(
                "MAZE DEBUG: Puzzle closed.",
                this
            );
        }
        finally
        {
            closing = false;
        }
    }


    // ======================================================
    // LATE UPDATE
    // ======================================================

    private void LateUpdate()
    {
        if (!IsOpen)
            return;


        worldIsolation.RefreshCameras();


        Vector2 size =
            viewport.rectTransform.rect.size;


        if (
            (size - lastViewportSize).sqrMagnitude > 1f ||
            runtimeTexture == null ||
            !runtimeTexture.IsCreated()
        )
        {
            try
            {
                UpdateRenderTexture();

                mazeBuilder.FrameCamera(
                    puzzleCamera
                );
            }
            catch (Exception error)
            {
                ClosePuzzle();

                Debug.LogException(
                    error,
                    this
                );
            }
        }
    }


    // ======================================================
    // UPDATE RENDER TEXTURE
    // ======================================================

    private void UpdateRenderTexture()
    {
        Vector2 size =
            viewport.rectTransform.rect.size;


        if (size.x < 1f ||
            size.y < 1f)
        {
            size =
                new Vector2(
                    Mathf.Max(1, Screen.width),
                    Mathf.Max(1, Screen.height)
                );
        }


        lastViewportSize = size;


        float scale =
            Mathf.Min(
                1f,
                maxTextureDimension /
                Mathf.Max(
                    size.x,
                    size.y
                )
            );


        int width =
            Mathf.Max(
                16,
                Mathf.RoundToInt(
                    size.x * scale
                )
            );


        int height =
            Mathf.Max(
                16,
                Mathf.RoundToInt(
                    size.y * scale
                )
            );


        if (
            runtimeTexture != null &&
            runtimeTexture.width == width &&
            runtimeTexture.height == height
        )
        {
            if (!runtimeTexture.IsCreated())
            {
                runtimeTexture.Create();
            }

            return;
        }


        bool wasEnabled =
            puzzleCamera.enabled;


        puzzleCamera.enabled = false;


        ReleaseRenderTexture();


        runtimeTexture =
            new RenderTexture(
                renderTextureTemplate
            )
            {
                name =
                    "DetectiveMaze_RuntimeRenderTexture",

                width = width,

                height = height,

                hideFlags =
                    HideFlags.DontSave
            };


        if (!runtimeTexture.Create())
        {
            throw new InvalidOperationException(
                "Could not allocate the maze RenderTexture."
            );
        }


        puzzleCamera.targetTexture =
            runtimeTexture;


        viewport.texture =
            runtimeTexture;


        puzzleCamera.aspect =
            (float)width / height;


        puzzleCamera.enabled =
            wasEnabled;
    }


    // ======================================================
    // RELEASE RENDER TEXTURE
    // ======================================================

    private void ReleaseRenderTexture()
    {
        if (puzzleCamera != null)
        {
            puzzleCamera.targetTexture =
                renderTextureTemplate;
        }


        if (viewport != null)
        {
            viewport.texture =
                renderTextureTemplate;
        }


        if (runtimeTexture == null)
            return;


        runtimeTexture.Release();

        Destroy(runtimeTexture);

        runtimeTexture = null;
    }


    // ======================================================
    // HIDE VIEW
    // ======================================================

    private void HideView()
    {
        if (puzzleCamera != null)
        {
            puzzleCamera.enabled = false;
        }


        if (popupCanvas != null)
        {
            popupCanvas.gameObject.SetActive(false);
        }


        if (mazeBuilder != null)
        {
            mazeBuilder.gameObject.SetActive(false);
        }
    }


    // ======================================================
    // REQUIRE REFERENCES
    // ======================================================

    private void RequireReferences()
    {
        if (
            popupCanvas == null ||
            mazePanel == null ||
            viewport == null ||
            puzzleCamera == null ||
            mazeBuilder == null ||
            inputHandler == null ||
            renderTextureTemplate == null
        )
        {
            throw new InvalidOperationException(
                "MazePuzzlePopup has missing references. " +
                "Run Tools > Detective Maze > " +
                "Build Prefab and Test Scene."
            );
        }
    }


    // ======================================================
    // INVOKE EVENTS SAFELY
    // ======================================================

    private void InvokeSafely(
        Action handlers
    )
    {
        if (handlers == null)
            return;


        foreach (
            Delegate handler
            in handlers.GetInvocationList()
        )
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception error)
            {
                Debug.LogException(
                    error,
                    this
                );
            }
        }
    }


    // ======================================================
    // DISABLE
    // ======================================================

    private void OnDisable()
    {
        ClosePuzzle();
    }


    // ======================================================
    // DESTROY
    // ======================================================

    private void OnDestroy()
    {
        ClosePuzzle();


        if (inputHandler != null)
        {
            inputHandler.CancelRequested -=
                ClosePuzzle;
        }


        ReleaseRenderTexture();


        if (Instance == this)
        {
            Instance = null;
        }
    }
}