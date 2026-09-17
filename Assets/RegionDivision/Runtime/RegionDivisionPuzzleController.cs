using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace RegionDivision
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Puzzles/Region Division Puzzle")]
    public sealed class RegionDivisionPuzzleController : MonoBehaviour
    {
        [SerializeField] private RegionDivisionLayout layout = RegionDivisionLayouts.CreateDefault();
        [Tooltip("Authored partition witness for editor validation only. Does not enable auto-solve.")]
        [SerializeField] private AuthoredRegion[] debugSolution = RegionDivisionLayouts.CreateDefaultSolution();
        [Tooltip("Show the standalone Open button, including in builds.")]
        [SerializeField] private bool showDebugHooks = true;
        [SerializeField] private bool logSuccessPlaceholder = true;

        private static readonly Color[] Palette = {
            new Color32(188, 165, 238, 255), new Color32(245, 177, 111, 255),
            new Color32(133, 184, 234, 255), new Color32(148, 210, 153, 255),
            new Color32(236, 157, 191, 255), new Color32(239, 217, 125, 255),
            new Color32(117, 212, 201, 255), new Color32(228, 156, 140, 255)
        };
        private static RegionDivisionPuzzleController activePuzzle;
        private RegionDivisionBoard board;
        private RegionDivisionView view;
        private IDisposable inputLock;
        private EventSystem puzzleEventSystem;
        private readonly List<EventSystem> suspendedEventSystems = new List<EventSystem>();
        private readonly List<BaseRaycaster> suspendedRaycasters = new List<BaseRaycaster>();
        private EventSystem previousEventSystem;
        private GameObject previousSelection;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private bool capturedCursor;
        private bool completing;
        private bool gestureActive;
        private bool draggingClaim;
        private int pointerId, startCol, startRow, pressedOwner;
        private Vector2 pressPosition;
        private bool movedForUndo;

        public event Action OnPuzzleSolved;
        public bool IsOpen { get; private set; }
        public int ClaimedCellCount { get { return board == null ? 0 : board.ClaimedCellCount; } }
        public bool IsGridValidAndComplete { get { return board != null && board.IsSolved(); } }
        public bool ShowDebugHooks { get { return showDebugHooks; } set { showDebugHooks = value; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { activePuzzle = null; }

        private void Awake() { OnPuzzleSolved += LogSolved; }
        private void LogSolved() { if (logSuccessPlaceholder) Debug.Log("Region division solved", this); }
        private void OnDisable() { CloseInternal(); }
        private void OnDestroy() { CloseInternal(); OnPuzzleSolved = null; }
        private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) AbortGesture(); }
        private void OnApplicationPause(bool paused) { if (paused) AbortGesture(); }

        [ContextMenu("Start Puzzle (Play Mode)")]
        public void StartPuzzle()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to open Region Division.", this); return; }
            if (completing) return; // A solved callback must not reopen the run being closed.
            if (activePuzzle != null && activePuzzle != this)
            { Debug.LogWarning("Another Region Division popup is already open.", this); return; }
            CloseInternal();
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            enabled = true;
            if (!gameObject.activeInHierarchy)
            { Debug.LogError("The puzzle's parent is inactive. Activate the parent before StartPuzzle().", this); return; }
            string error;
            if (!IsValidRegionLayout(layout, out error))
            { Debug.LogError("Region Division: " + error, this); return; }
            try
            {
                board = new RegionDivisionBoard(layout);
                view = new RegionDivisionView(transform, this, board, Palette);
                // Build the module on an inactive child. Only this event system will
                // process the modal UI, regardless of the host scene's input module.
                var eventObject = new GameObject("Puzzle EventSystem");
                eventObject.SetActive(false);
                eventObject.transform.SetParent(view.Root.transform, false);
                puzzleEventSystem = eventObject.AddComponent<EventSystem>();
                puzzleEventSystem.sendNavigationEvents = false;
#if ENABLE_INPUT_SYSTEM
                var module = eventObject.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
#else
                eventObject.AddComponent<RegionDivisionLegacyInputModule>();
#endif
                inputLock = RegionDivisionInputLock.Acquire();
                previousEventSystem = EventSystem.current;
                previousSelection = previousEventSystem == null ? null : previousEventSystem.currentSelectedGameObject;
                // All EventSystems query the global raycaster registry. Suspending
                // the host EventSystem alone would not isolate higher sorting layers
                // or physics raycasters from this puzzle's input module.
                foreach (BaseRaycaster raycaster in FindObjectsOfType<BaseRaycaster>())
                {
                    if (!raycaster.enabled) continue;
                    suspendedRaycasters.Add(raycaster);
                    raycaster.enabled = false;
                }
                foreach (EventSystem system in FindObjectsOfType<EventSystem>())
                {
                    if (system == puzzleEventSystem || !system.enabled) continue;
                    suspendedEventSystems.Add(system);
                    system.enabled = false;
                }
                previousCursorLock = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                capturedCursor = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                activePuzzle = this;
                IsOpen = true;
                view.Root.SetActive(true);
                eventObject.SetActive(true);
                EventSystem.current = puzzleEventSystem;
                Canvas.ForceUpdateCanvases();
                puzzleEventSystem.SetSelectedGameObject(view.Grid.gameObject);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                CloseInternal();
            }
        }

        [ContextMenu("Cancel Puzzle")]
        public void CancelPuzzle() { CloseInternal(); }

        /// <summary>Scripted claims and pointer releases share this exact path.</summary>
        public bool TryClaimRegion(int clueCol, int clueRow, int endCol, int endRow, out string error)
        {
            if (!IsOpen || completing || board == null)
            { error = "The puzzle is closed."; return false; }
            if (!board.TryClaim(clueCol, clueRow, endCol, endRow, out error))
            { view.SetStatus(error); return false; }
            view.Refresh();
            view.SetStatus("Region claimed. Tap any filled cell to undo it.");
            CheckForWin();
            return true;
        }

        public bool TryUnclaimRegion(int col, int row)
        {
            if (!IsOpen || completing || board == null || !board.TryUnclaim(col, row)) return false;
            AbortGesture();
            view.Refresh();
            view.SetStatus("Region cleared. Drag again from its number.");
            return true;
        }

        private void CheckForWin()
        {
            if (completing || !IsOpen || !board.IsSolved()) return;
            completing = true;
            AbortGesture();
            try
            {
                Action subscribers = OnPuzzleSolved;
                if (subscribers != null)
                    foreach (Action subscriber in subscribers.GetInvocationList())
                    {
                        // A bad listener must not prevent other listeners or input restoration.
                        try { subscriber(); }
                        catch (Exception exception) { Debug.LogException(exception, this); }
                    }
            }
            finally { CloseInternal(); completing = false; }
        }

        private void CloseInternal()
        {
            IsOpen = false;
            AbortGesture();
            if (view != null)
            {
                GameObject root = view.Root;
                view = null;
                if (root != null) { root.SetActive(false); Destroy(root); }
            }
            board = null;
            puzzleEventSystem = null;
            if (activePuzzle == this) activePuzzle = null;
            foreach (BaseRaycaster raycaster in suspendedRaycasters)
                if (raycaster != null) raycaster.enabled = true;
            suspendedRaycasters.Clear();
            foreach (EventSystem system in suspendedEventSystems)
                if (system != null) system.enabled = true;
            suspendedEventSystems.Clear();
            if (previousEventSystem != null && previousEventSystem.isActiveAndEnabled)
            {
                EventSystem.current = previousEventSystem;
                if (previousSelection != null && previousSelection.activeInHierarchy)
                    previousEventSystem.SetSelectedGameObject(previousSelection);
            }
            previousEventSystem = null;
            previousSelection = null;
            if (capturedCursor)
            {
                capturedCursor = false;
                Cursor.lockState = previousCursorLock;
                Cursor.visible = previousCursorVisible;
            }
            IDisposable token = inputLock;
            inputLock = null;
            if (token != null) token.Dispose();
        }

        // Retained as an inert compatibility entry point for existing UnityEvents
        // or scripts. Auto-solve is permanently disabled, regardless of debug flags.
        public void DebugAutoSolve()
        {
        }

        internal void PointerDown(PointerEventData data)
        {
            if (!IsOpen || completing || gestureActive || data.button != PointerEventData.InputButton.Left) return;
            int col, row;
            if (!Locate(data, out col, out row)) return;
            int owner = board.OwnerAt(col, row);
            if (owner < 0 && !board.CanStart(col, row))
            { view.SetStatus("Start on an unclaimed number."); return; }
            gestureActive = true;
            pointerId = data.pointerId;
            startCol = col; startRow = row;
            pressedOwner = owner;
            pressPosition = data.position;
            movedForUndo = false;
            draggingClaim = owner < 0;
            if (draggingClaim) UpdatePreview(data);
        }

        internal void PointerDrag(PointerEventData data)
        {
            if (!gestureActive || data.pointerId != pointerId || !IsOpen) return;
            float threshold = Mathf.Max(8f, Screen.dpi > 0 ? Screen.dpi * 0.04f : 8f);
            if ((data.position - pressPosition).sqrMagnitude > threshold * threshold) movedForUndo = true;
            if (draggingClaim) UpdatePreview(data);
        }

        internal void PointerUp(PointerEventData data)
        {
            if (!gestureActive || data.pointerId != pointerId || !IsOpen) return;
            // Touch cancellation is not a normal release/claim.
#if ENABLE_INPUT_SYSTEM
            var extended = data as ExtendedPointerEventData;
            if (extended != null && Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.touchId.ReadValue() == extended.touchId
                        && touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
                    { AbortGesture(); return; }
#else
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).fingerId == pointerId && Input.GetTouch(i).phase == TouchPhase.Canceled)
                { AbortGesture(); return; }
#endif
            int col, row;
            bool inside = Locate(data, out col, out row);
            bool wasClaim = draggingClaim;
            int fromCol = startCol, fromRow = startRow;
            int owner = pressedOwner;
            float threshold = Mathf.Max(8f, Screen.dpi > 0 ? Screen.dpi * 0.04f : 8f);
            bool undoTap = !movedForUndo && (data.position - pressPosition).sqrMagnitude <= threshold * threshold;
            AbortGesture();
            if (!inside) { view.SetStatus("Release inside the grid. Nothing was claimed."); return; }
            if (wasClaim)
            {
                string error;
                TryClaimRegion(fromCol, fromRow, col, row, out error);
            }
            else if (undoTap && board.OwnerAt(col, row) == owner) TryUnclaimRegion(col, row);
        }

        private void UpdatePreview(PointerEventData data)
        {
            int col, row;
            bool inside = Locate(data, out col, out row);
            // Locate clamps the display endpoint; releasing outside still rejects.
            var rect = new RegionRectangle(startCol, startRow, col, row);
            string error = "Release inside the grid.";
            bool valid = inside && board.CanClaim(startCol, startRow, col, row, out error);
            view.ShowPreview(rect, valid);
            view.SetStatus(valid ? rect.Area + " cells — release to claim." : error);
        }

        private bool Locate(PointerEventData data, out int col, out int row)
        {
            col = startCol; row = startRow;
            Vector2 local;
            RectTransform grid = view.Grid;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(grid, data.position, data.pressEventCamera, out local)) return false;
            Rect bounds = grid.rect;
            if (bounds.width <= 0 || bounds.height <= 0) return false;
            float x = (local.x - bounds.xMin) / bounds.width;
            float y = (bounds.yMax - local.y) / bounds.height;
            col = Mathf.Clamp(Mathf.FloorToInt(x * board.Columns), 0, board.Columns - 1);
            row = Mathf.Clamp(Mathf.FloorToInt(y * board.Rows), 0, board.Rows - 1);
            return x >= 0 && x < 1 && y >= 0 && y < 1;
        }

        internal void AbortGesture()
        {
            gestureActive = false;
            draggingClaim = false;
            if (view != null && view.Root != null) view.HidePreview();
        }

        private void Update()
        {
            if (!IsOpen) return;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CancelPuzzle();
#else
            if (Input.GetKeyDown(KeyCode.Escape)) CancelPuzzle();
#endif
        }

        // IMGUI is deliberately used only for the standalone debug launcher. It
        // needs no EventSystem and leaves the closed puzzle's canvas uninstantiated.
        private void OnGUI()
        {
            if (!showDebugHooks || !Application.isPlaying || IsOpen || activePuzzle != null) return;
            if (GUI.Button(new Rect(16, 16, 235, 42), "DEBUG: Open Region Division")) StartPuzzle();
        }

        public static bool IsValidRegionLayout(RegionDivisionLayout value, out string error)
        { return RegionDivisionLayouts.IsValidRegionLayout(value, out error); }

        private void OnValidate()
        {
            string error;
            if (!IsValidRegionLayout(layout, out error)) Debug.LogError("Region Division layout: " + error, this);
            else if (debugSolution != null && debugSolution.Length > 0
                && !RegionDivisionLayouts.ValidateAuthoredSolution(layout, debugSolution, out error))
                Debug.LogError("Region Division authored solution: " + error, this);
        }
    }
}
