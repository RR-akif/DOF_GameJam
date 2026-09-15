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
    [Tooltip("Open the popup immediately when Play starts. Disable only when a main-game trigger should open it later.")]
    [SerializeField] private bool openOnStart = true;

    [Header("Assign only when testing over main-game controls")]
    [Tooltip("Drag EVERY movement/look/interaction input reader to this list, including legacy scripts. No automatic discovery or changes to their code.")]
    [SerializeField] private MonoBehaviour[] mainGameControllers = Array.Empty<MonoBehaviour>();
    [SerializeField, Range(512, 2048)] private int maxTextureDimension = 1600;

    private readonly MazeInputIsolation inputIsolation = new MazeInputIsolation();
    private readonly MazeWorldIsolation worldIsolation = new MazeWorldIsolation();
    private RenderTexture runtimeTexture;
    private bool ready;
    private bool opening;
    private bool closing;
    private bool completing;
    private Vector2 lastViewportSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSingleton() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Only one active MazePuzzlePopup is supported. Disabling the duplicate.", this);
            gameObject.SetActive(false);
            return;
        }
        Instance = this;
        try
        {
            RequireReferences();
            puzzleCamera.enabled = false; // Always off until a RenderTexture is assigned.
            puzzleCamera.orthographic = true;
            puzzleCamera.clearFlags = CameraClearFlags.SolidColor;
            puzzleCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            puzzleCamera.allowHDR = false;
            puzzleCamera.allowMSAA = false;
            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            popupCanvas.sortingOrder = 30000;
            mazePanel.sprite = MazeBuilder.LoadSprite("PopupBackdrop"); // No Inspector art assignment.
            mazePanel.color = new Color(1f, 1f, 1f, 0.92f);
            mazePanel.raycastTarget = true;
            RectTransform rect = viewport.rectTransform;
            // REQUIRED: RawImage fills MazePanel, even if a prefab was edited incorrectly.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            viewport.raycastTarget = true;
            inputHandler.DisableMazeInput();
            inputHandler.CancelRequested += ClosePuzzle;
            HideView();
            ready = true;
        }
        catch (Exception error)
        {
            HideView();
            Debug.LogException(error, this);
        }
    }

    private void Start()
    {
        // Run once. Closing or completing never starts another maze automatically.
        if (openOnStart) StartPuzzle();
    }

    /// <summary>Open a fresh maze, reset the detective, and lock the supplied main-game controllers.</summary>
    public void StartPuzzle()
    {
        if (!ready || !isActiveAndEnabled || Instance != this || IsOpen || opening || closing || completing) return;
        if (Time.timeScale <= 0f || Physics2D.simulationMode != SimulationMode2D.FixedUpdate)
        {
            Debug.LogError("Detective Maze needs Time.timeScale > 0 and Physics 2D Simulation Mode = Fixed Update. It does not change your game's time or physics simulation mode.", this);
            return;
        }
        opening = true;
        try
        {
            int layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0) throw new InvalidOperationException("The MazePuzzle layer is missing. Run the Detective Maze setup menu.");
            IsOpen = true;
            inputIsolation.Acquire(mainGameControllers, transform);
            worldIsolation.Acquire(layer, puzzleCamera);
            popupCanvas.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            UpdateRenderTexture();
            // Build while inactive, so no half-built objects can simulate or trigger success.
            mazeBuilder.gameObject.SetActive(false);
            mazeBuilder.Build(this, inputHandler, puzzleCamera);
            mazeBuilder.gameObject.SetActive(true);
            mazeBuilder.Player.ResetTo(mazeBuilder.SpawnPosition);
            inputHandler.EnableMazeInput();
            puzzleCamera.gameObject.SetActive(true);
            puzzleCamera.enabled = true;
        }
        catch (Exception error)
        {
            ClosePuzzle();
            Debug.LogException(error, this);
        }
        finally { opening = false; }
    }

    /// <summary>Success is idempotent for each open session; manual calls are safe.</summary>
    public void CompletePuzzle()
    {
        if (!IsOpen || completing || closing) return;
        completing = true;
        try
        {
            Debug.Log("success");
            InvokeSafely(OnPuzzleSuccess);
        }
        finally
        {
            ClosePuzzle();
            completing = false;
        }
    }

    /// <summary>Cancel or close after success; restore the exact previous controller states.</summary>
    public void ClosePuzzle()
    {
        if (closing) return;
        bool wasOpen = IsOpen;
        IsOpen = false;
        closing = true;
        try
        {
            if (inputHandler != null) inputHandler.DisableMazeInput();
            if (mazeBuilder != null && mazeBuilder.Player != null) mazeBuilder.Player.StopMoving();
            HideView();
            worldIsolation.Release();
            inputIsolation.Release();
            if (wasOpen) InvokeSafely(OnPuzzleClosed);
        }
        finally { closing = false; }
    }

    private void LateUpdate()
    {
        if (!IsOpen) return;
        worldIsolation.RefreshCameras();
        Vector2 size = viewport.rectTransform.rect.size;
        if ((size - lastViewportSize).sqrMagnitude > 1f || runtimeTexture == null || !runtimeTexture.IsCreated())
        {
            try
            {
                UpdateRenderTexture();
                mazeBuilder.FrameCamera(puzzleCamera);
            }
            catch (Exception error)
            {
                ClosePuzzle();
                Debug.LogException(error, this);
            }
        }
    }

    private void UpdateRenderTexture()
    {
        Vector2 size = viewport.rectTransform.rect.size;
        if (size.x < 1f || size.y < 1f) size = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        lastViewportSize = size;
        float scale = Mathf.Min(1f, maxTextureDimension / Mathf.Max(size.x, size.y));
        int width = Mathf.Max(16, Mathf.RoundToInt(size.x * scale));
        int height = Mathf.Max(16, Mathf.RoundToInt(size.y * scale));
        if (runtimeTexture != null && runtimeTexture.width == width && runtimeTexture.height == height)
        {
            if (!runtimeTexture.IsCreated()) runtimeTexture.Create();
            return;
        }
        bool wasEnabled = puzzleCamera.enabled;
        puzzleCamera.enabled = false;
        ReleaseRenderTexture();
        runtimeTexture = new RenderTexture(renderTextureTemplate)
        {
            name = "DetectiveMaze_RuntimeRenderTexture",
            width = width,
            height = height,
            hideFlags = HideFlags.DontSave
        };
        if (!runtimeTexture.Create()) throw new InvalidOperationException("Could not allocate the maze RenderTexture.");
        puzzleCamera.targetTexture = runtimeTexture;
        viewport.texture = runtimeTexture;
        puzzleCamera.aspect = (float)width / height;
        puzzleCamera.enabled = wasEnabled;
    }

    private void ReleaseRenderTexture()
    {
        if (puzzleCamera != null) puzzleCamera.targetTexture = renderTextureTemplate;
        if (viewport != null) viewport.texture = renderTextureTemplate;
        if (runtimeTexture == null) return;
        runtimeTexture.Release();
        Destroy(runtimeTexture);
        runtimeTexture = null;
    }

    private void HideView()
    {
        if (puzzleCamera != null) puzzleCamera.enabled = false;
        if (popupCanvas != null) popupCanvas.gameObject.SetActive(false);
        if (mazeBuilder != null) mazeBuilder.gameObject.SetActive(false);
    }

    private void RequireReferences()
    {
        if (popupCanvas == null || mazePanel == null || viewport == null || puzzleCamera == null ||
            mazeBuilder == null || inputHandler == null || renderTextureTemplate == null)
            throw new InvalidOperationException("MazePuzzlePopup has missing references. Run Tools > Detective Maze > Build Prefab and Test Scene.");
    }

    private void InvokeSafely(Action handlers)
    {
        if (handlers == null) return;
        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try { ((Action)handler)(); }
            catch (Exception error) { Debug.LogException(error, this); }
        }
    }

    private void OnDisable() => ClosePuzzle();

    private void OnDestroy()
    {
        ClosePuzzle();
        if (inputHandler != null) inputHandler.CancelRequested -= ClosePuzzle;
        ReleaseRenderTexture();
        if (Instance == this) Instance = null;
    }
}
