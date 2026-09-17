using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RollTheBall
{
    [DisallowMultipleComponent]
    public sealed class RollTheBallPuzzleController : MonoBehaviour
    {
        [Header("Layout (ScriptableObject takes priority over JSON)")]
        [SerializeField] private RollTheBallLayoutAsset layoutAsset;
        [SerializeField] private TextAsset layoutJson;

        [Header("Animation")]
        [SerializeField, Min(.1f)] private float ballCellsPerSecond = 2.5f;

        [Header("Testing hooks (available in builds too)")]
        [SerializeField] private bool showDebugControls = true;
        [SerializeField] private bool logSolvedPlaceholder = true;


        // =========================================================
        // EVENTS
        // =========================================================

        public event Action OnPuzzleSolved;

        // NEW:
        // Fires only when an actually open puzzle is cancelled.
        public event Action OnPuzzleCancelled;


        // =========================================================
        // PUBLIC STATE
        // =========================================================

        public bool IsOpen
        {
            get { return state != PuzzleState.Closed; }
        }

        public bool IsRolling
        {
            get { return state == PuzzleState.Rolling; }
        }

        public Vector2Int BallCell { get; private set; }

        public float BallCellsPerSecond
        {
            get { return ballCellsPerSecond; }

            set
            {
                ballCellsPerSecond =
                    Mathf.Max(.1f, value);
            }
        }

        public bool ShowDebugControls
        {
            get { return showDebugControls; }

            set
            {
                showDebugControls = value;

                if (!value)
                    RemoveLauncher();
            }
        }


        // =========================================================
        // INTERNAL STATE
        // =========================================================

        private enum PuzzleState
        {
            Closed,
            Playing,
            Rolling,
            Completing
        }

        private PuzzleState state;

        private static RollTheBallPuzzleController activePuzzle;

        private RollTheBallLayout runLayout;
        private RollTheBallBoard board;
        private RollTheBallView view;
        private RollTheBallInputLock inputLock;

        private GameObject modalEventSystem;
        private GameObject launcher;
        private GameObject launcherEventSystem;

        private int generation;

        private bool lifecycleTransition;
        private bool restartRequested;
        private bool cancelRequested;

        private RollTheBallTileDrag dragged;

        private int dragPointer;

        private Vector2 dragStartPointer;

        private Vector2Int dragFrom;

        private SlideAxis dragAxis;

        private int dragMin;
        private int dragMax;

        private float dragDistance;


        // =========================================================
        // STATIC RESET
        // =========================================================

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activePuzzle = null;
        }


        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            OnPuzzleSolved += LogSolved;
        }


        private void LogSolved()
        {
            if (logSolvedPlaceholder)
            {
                Debug.Log("Roll the ball solved");
            }
        }


        // =========================================================
        // LAYOUT
        // =========================================================

        public void SetLayoutJson(TextAsset json)
        {
            if (!json)
                throw new ArgumentNullException("json");

            RollTheBallBoard.ValidatePlayable(
                RollTheBallLayout.FromJson(json.text)
            );

            CancelPuzzle();

            layoutAsset = null;
            layoutJson = json;
        }


        public void SetLayoutAsset(
            RollTheBallLayoutAsset asset)
        {
            if (!asset)
                throw new ArgumentNullException("asset");

            RollTheBallBoard.ValidatePlayable(
                asset.layout
            );

            CancelPuzzle();

            layoutAsset = asset;
        }


        // =========================================================
        // START PUZZLE
        // =========================================================

        [ContextMenu("Start Puzzle (Play Mode)")]
        public void StartPuzzle()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Enter Play Mode to open Roll the Ball.",
                    this
                );

                return;
            }

            if (lifecycleTransition)
            {
                restartRequested = true;
                return;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (!enabled)
                enabled = true;

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogError(
                    "Roll the Ball needs an active parent to open.",
                    this
                );

                return;
            }

            lifecycleTransition = true;

            try
            {
                CloseInternal();

                RemoveLauncher();

                if (layoutAsset &&
                    layoutAsset.layout != null)
                {
                    runLayout =
                        RollTheBallLayout.FromJson(
                            JsonUtility.ToJson(
                                layoutAsset.layout
                            )
                        );
                }
                else if (layoutJson)
                {
                    runLayout =
                        RollTheBallLayout.FromJson(
                            layoutJson.text
                        );
                }
                else
                {
                    throw new InvalidOperationException(
                        "Assign a layout asset or JSON. " +
                        "The shipped prefab includes DefaultLayout.json."
                    );
                }

                RollTheBallBoard.ValidatePlayable(
                    runLayout
                );

                board =
                    new RollTheBallBoard(
                        runLayout
                    );

                BallCell =
                    board.Start;

                if (activePuzzle &&
                    activePuzzle != this)
                {
                    activePuzzle.CancelPuzzle();
                }

                activePuzzle = this;

                inputLock =
                    new RollTheBallInputLock(
                        transform
                    );

                view =
                    new RollTheBallView(
                        this,
                        board,
                        showDebugControls
                    );

                modalEventSystem =
                    RollTheBallInputLock.CreateEventSystem(
                        view.Root.transform
                    );

                modalEventSystem.SetActive(true);

                EventSystem.current =
                    modalEventSystem
                        .GetComponent<EventSystem>();

                EventSystem.current
                    .SetSelectedGameObject(
                        view.CloseButton.gameObject
                    );

                state =
                    PuzzleState.Playing;
            }
            catch (Exception error)
            {
                Debug.LogException(
                    error,
                    this
                );

                CloseInternal();
            }
            finally
            {
                lifecycleTransition = false;

                ProcessPendingLifecycle();
            }
        }


        // =========================================================
        // CANCEL PUZZLE
        // =========================================================

        [ContextMenu("Cancel Puzzle (Play Mode)")]
        public void CancelPuzzle()
        {
            if (lifecycleTransition)
            {
                cancelRequested = true;
                restartRequested = false;
                return;
            }

            // Was the puzzle actually open?
            bool wasOpen =
                state != PuzzleState.Closed;

            lifecycleTransition = true;

            try
            {
                CloseInternal();
            }
            finally
            {
                lifecycleTransition = false;

                ProcessPendingLifecycle();
            }

            // IMPORTANT:
            // Only notify Level 3 when an actual
            // open puzzle was cancelled.
            if (wasOpen)
            {
                InvokePuzzleCancelled();
            }
        }


        private void InvokePuzzleCancelled()
        {
            var handlers =
                OnPuzzleCancelled;

            if (handlers == null)
                return;

            foreach (
                Action handler
                in handlers.GetInvocationList())
            {
                try
                {
                    handler();
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


        // =========================================================
        // PENDING LIFECYCLE
        // =========================================================

        private void ProcessPendingLifecycle()
        {
            if (cancelRequested)
            {
                cancelRequested = false;
                restartRequested = false;

                CancelPuzzle();
            }
            else if (restartRequested)
            {
                restartRequested = false;

                if (isActiveAndEnabled)
                {
                    StartPuzzle();
                }
            }
        }


        // =========================================================
        // INTERNAL CLOSE
        // =========================================================

        private void CloseInternal()
        {
            generation++;

            StopAllCoroutines();

            dragged = null;

            state =
                PuzzleState.Closed;

            var oldView =
                view;

            var oldLock =
                inputLock;

            var oldSystem =
                modalEventSystem;

            view = null;
            inputLock = null;
            modalEventSystem = null;
            board = null;
            runLayout = null;

            BallCell =
                default(Vector2Int);

            if (activePuzzle == this)
            {
                activePuzzle = null;
            }

            if (oldSystem)
            {
                oldSystem.SetActive(false);
            }

            if (oldView != null)
            {
                oldView.Dispose();
            }

            if (oldLock != null)
            {
                oldLock.Dispose();
            }
        }


        // =========================================================
        // PUBLIC SLIDE
        // =========================================================

        public bool TrySlideTile(
            Vector2Int from,
            Vector2Int to)
        {
            if (state != PuzzleState.Playing ||
                dragged ||
                !board.TrySlide(from, to))
            {
                return false;
            }

            view.SyncTiles();

            CheckForPath();

            return true;
        }


        // =========================================================
        // DRAG
        // =========================================================

        internal void BeginTileDrag(
            RollTheBallTileDrag tile,
            PointerEventData e)
        {
            if (state != PuzzleState.Playing ||
                dragged ||
                e.button !=
                PointerEventData.InputButton.Left ||
                !board.IsMovable(tile.cell))
            {
                return;
            }

            if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    view.BoardRect,
                    e.position,
                    e.pressEventCamera,
                    out dragStartPointer))
            {
                return;
            }

            dragged =
                tile;

            dragPointer =
                e.pointerId;

            dragFrom =
                tile.cell;

            dragAxis =
                board.AxisAt(dragFrom);

            dragDistance =
                0f;

            board.GetSlideLimits(
                dragFrom,
                out dragMin,
                out dragMax
            );

            tile.transform
                .SetAsLastSibling();
        }


        internal void DragTile(
            RollTheBallTileDrag tile,
            PointerEventData e)
        {
            if (state != PuzzleState.Playing ||
                dragged != tile ||
                e.pointerId != dragPointer)
            {
                return;
            }

            Vector2 pointer;

            if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    view.BoardRect,
                    e.position,
                    e.pressEventCamera,
                    out pointer))
            {
                return;
            }

            Vector2 delta =
                pointer -
                dragStartPointer;

            dragDistance =
                Mathf.Clamp(
                    (
                        dragAxis ==
                        SlideAxis.Horizontal
                            ? delta.x
                            : delta.y
                    ) / view.CellSize,
                    dragMin,
                    dragMax
                );

            Vector2 axis =
                dragAxis ==
                SlideAxis.Horizontal
                    ? Vector2.right
                    : Vector2.up;

            ((RectTransform)tile.transform)
                .anchoredPosition =
                    view.Position(dragFrom) +
                    axis *
                    (
                        dragDistance *
                        view.CellSize
                    );
        }


        internal void EndTileDrag(
            RollTheBallTileDrag tile,
            PointerEventData e)
        {
            if (dragged != tile ||
                e.pointerId != dragPointer ||
                state != PuzzleState.Playing)
            {
                return;
            }

            DragTile(
                tile,
                e
            );

            int steps =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        dragDistance
                    ),
                    dragMin,
                    dragMax
                );

            var to =
                dragFrom +
                (
                    dragAxis ==
                    SlideAxis.Horizontal
                        ? Vector2Int.right
                        : Vector2Int.up
                ) * steps;

            dragged =
                null;

            if (!TrySlideTile(
                dragFrom,
                to))
            {
                view.SyncTiles();
            }
        }


        private void AbortDrag()
        {
            if (!dragged)
                return;

            dragged =
                null;

            if (view != null)
            {
                view.SyncTiles();
            }
        }


        // =========================================================
        // CHECK PATH
        // =========================================================

        private void CheckForPath()
        {
            List<Vector2Int> path;

            if (state != PuzzleState.Playing ||
                !board.TryFindPath(
                    BallCell,
                    out path))
            {
                return;
            }

            state =
                PuzzleState.Rolling;

            AbortDrag();

            view.SetRolling();

            StartCoroutine(
                RollBall(
                    path,
                    generation
                )
            );
        }


        // =========================================================
        // BALL ROLLING
        // =========================================================

        private IEnumerator RollBall(
            List<Vector2Int> path,
            int run)
        {
            float angle =
                0f;

            for (
                int i = 1;
                i < path.Count;
                i++)
            {
                Vector2 from =
                    view.Position(
                        path[i - 1]
                    );

                Vector2 to =
                    view.Position(
                        path[i]
                    );

                float elapsed =
                    0f;

                float duration =
                    1f /
                    Mathf.Max(
                        .1f,
                        ballCellsPerSecond
                    );

                float startAngle =
                    angle;

                float stepAngle =
                    (
                        1f /
                        (.235f * .5f)
                    ) * Mathf.Rad2Deg;

                while (
                    elapsed <
                    duration)
                {
                    if (run != generation ||
                        state !=
                        PuzzleState.Rolling)
                    {
                        yield break;
                    }

                    elapsed +=
                        Time.unscaledDeltaTime;

                    float t =
                        Mathf.Clamp01(
                            elapsed /
                            duration
                        );

                    view.Ball
                        .anchoredPosition =
                            Vector2.Lerp(
                                from,
                                to,
                                t
                            );

                    angle =
                        startAngle -
                        stepAngle * t;

                    view.Ball
                        .localRotation =
                            Quaternion.Euler(
                                0f,
                                0f,
                                angle
                            );

                    yield return null;
                }

                if (run != generation ||
                    state !=
                    PuzzleState.Rolling)
                {
                    yield break;
                }

                view.Ball
                    .anchoredPosition =
                        to;

                BallCell =
                    path[i];
            }

            if (run == generation &&
                BallCell == board.Goal)
            {
                FinishSolved(run);
            }
        }


        // =========================================================
        // SOLVED
        // =========================================================

        private void FinishSolved(int run)
        {
            if (state !=
                PuzzleState.Rolling)
            {
                return;
            }

            state =
                PuzzleState.Completing;

            try
            {
                var handlers =
                    OnPuzzleSolved;

                if (handlers != null)
                {
                    foreach (
                        Action handler
                        in handlers
                            .GetInvocationList())
                    {
                        try
                        {
                            handler();
                        }
                        catch (
                            Exception error)
                        {
                            Debug.LogException(
                                error,
                                this
                            );
                        }
                    }
                }
            }
            finally
            {
                // IMPORTANT:
                // Use CloseInternal instead of CancelPuzzle
                // here so solving the puzzle does NOT
                // fire OnPuzzleCancelled.

                if (this &&
                    generation == run)
                {
                    lifecycleTransition = true;

                    try
                    {
                        CloseInternal();
                    }
                    finally
                    {
                        lifecycleTransition = false;

                        ProcessPendingLifecycle();
                    }
                }
            }
        }


        // =========================================================
        // DEBUG AUTO SOLVE
        // =========================================================

        [ContextMenu("Debug Auto-Solve (Play Mode)")]
        public void DebugAutoSolve()
        {
            if (state != PuzzleState.Playing ||
                !showDebugControls)
            {
                return;
            }

            AbortDrag();

            while (
                board.UndoLastSlide())
            {
            }

            foreach (
                var move
                in runLayout.debugSolution)
            {
                if (!board.TrySlide(
                    move.from,
                    move.to))
                {
                    throw new InvalidOperationException(
                        "Validated solution became illegal."
                    );
                }
            }

            view.SyncTiles();

            CheckForPath();
        }


        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (state ==
                PuzzleState.Closed)
            {
                UpdateLauncher();

                return;
            }

            if (view != null &&
                view.RefreshScreen())
            {
                AbortDrag();
            }
        }


        private void LateUpdate()
        {
            if (view == null ||
                !modalEventSystem)
            {
                return;
            }

            var system =
                modalEventSystem
                    .GetComponent<EventSystem>();

            if (!system
                .currentSelectedGameObject)
            {
                system
                    .SetSelectedGameObject(
                        view.CloseButton.gameObject
                    );
            }
        }


        // =========================================================
        // DEBUG LAUNCHER
        // =========================================================

        private void UpdateLauncher()
        {
            if (!showDebugControls ||
                RollTheBallInputLock.IsLocked)
            {
                RemoveLauncher();

                return;
            }

            if (!launcher)
            {
                launcher =
                    RollTheBallView.CreateCanvas(
                        transform,
                        "Debug launcher",
                        32750
                    );

                var button =
                    RollTheBallView.MakeButton(
                        launcher.transform,
                        "Test: Roll the Ball",
                        Vector2.zero,
                        new Vector2(
                            216,
                            50
                        ),
                        StartPuzzle,
                        RollTheBallView.Hex(
                            0x3F655E
                        )
                    );

                var rect =
                    (RectTransform)
                    button.transform;

                rect.anchorMin =
                    rect.anchorMax =
                    rect.pivot =
                        new Vector2(
                            0f,
                            1f
                        );

                rect.anchoredPosition =
                    new Vector2(
                        20f,
                        -20f
                    );
            }

            bool hostSystemExists =
                false;

            foreach (
                var system
                in FindObjectsOfType<EventSystem>())
            {
                if (system.isActiveAndEnabled &&
                    (
                        !launcherEventSystem ||
                        system.gameObject !=
                        launcherEventSystem
                    ))
                {
                    hostSystemExists =
                        true;

                    break;
                }
            }

            if (hostSystemExists)
            {
                if (launcherEventSystem)
                {
                    launcherEventSystem
                        .SetActive(false);

                    Destroy(
                        launcherEventSystem
                    );

                    launcherEventSystem =
                        null;
                }
            }
            else if (
                !launcherEventSystem)
            {
                try
                {
                    launcherEventSystem =
                        RollTheBallInputLock
                            .CreateEventSystem(
                                launcher.transform
                            );

                    launcherEventSystem
                        .SetActive(true);
                }
                catch (
                    Exception error)
                {
                    Debug.LogException(
                        error,
                        this
                    );

                    showDebugControls =
                        false;

                    RemoveLauncher();
                }
            }
        }


        private void RemoveLauncher()
        {
            if (launcherEventSystem)
            {
                launcherEventSystem
                    .SetActive(false);
            }

            launcherEventSystem =
                null;

            if (launcher)
            {
                launcher
                    .SetActive(false);

                Destroy(
                    launcher
                );

                launcher =
                    null;
            }
        }


        // =========================================================
        // UNITY EVENTS
        // =========================================================

        private void OnApplicationFocus(
            bool focused)
        {
            if (!focused)
            {
                AbortDrag();
            }
        }


        private void OnApplicationPause(
            bool paused)
        {
            if (paused)
            {
                AbortDrag();
            }
        }


        private void OnDisable()
        {
            CloseInternal();

            RemoveLauncher();
        }


        private void OnDestroy()
        {
            CloseInternal();

            RemoveLauncher();

            OnPuzzleSolved -=
                LogSolved;
        }
    }
}