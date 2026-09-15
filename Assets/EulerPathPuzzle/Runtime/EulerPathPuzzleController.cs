using System;
using UnityEngine;

namespace DegreesOfFreedom.EulerPath
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-32000)]
    public sealed class EulerPathPuzzleController : MonoBehaviour
    {
        [Tooltip("Swap this authored asset to change the graph. The supplied prefab uses the asymmetric polygon layout.")]
        [SerializeField] private EulerPathLayoutAsset layoutAsset;
        [Tooltip("Shows the standalone test-open button in Editor AND builds. F8 also opens it.")]
        [SerializeField] private bool showDebugButton = true;
        [SerializeField] private bool logSolvedToConsole = true;
        [Tooltip("Pauses physics/scaled time while the popup is open; restores the previous value on close.")]
        [SerializeField] private bool pauseGameTime = true;
        [Min(1)] [SerializeField] private float nodeHitRadius = 18f;
        [Min(1)] [SerializeField] private float edgeTolerance = 20f;

        public event Action OnPuzzleSolved;
        public bool IsOpen { get; private set; }
        public static bool IsMainGameInputLocked { get { return EulerPuzzleInputLock.IsLocked; } }
        public EulerPathLayoutAsset LayoutAsset { get { return layoutAsset; } set { if (!IsOpen) layoutAsset = value; } }
        public bool ShowDebugButton
        {
            get { return showDebugButton; }
            set { showDebugButton = value; if (view != null) view.DebugRoot.SetActive(value && !IsOpen); }
        }
        public int TracedEdgeCount { get { return session == null ? 0 : session.TracedCount; } }

        private EulerPuzzleView view;
        private EulerTraceSession session;
        private readonly EulerPuzzleInput input = new EulerPuzzleInput();
        private EulerPuzzleInputLock inputLock;
        private bool completing, closing;
        private int openedFrame = -1;

        private void Awake()
        {
            if (logSolvedToConsole) OnPuzzleSolved += LogSolved;
            if (showDebugButton) EnsureView();
        }
        private void OnEnable()
        {
            if (view != null) view.DebugRoot.SetActive(showDebugButton && !IsOpen);
        }
        private void OnDisable() { Close(); }
        private void OnDestroy() { Close(); OnPuzzleSolved -= LogSolved; }
        private void OnApplicationFocus(bool focused) { if (!focused) ReleasePointer(); }
        private void OnApplicationPause(bool paused) { if (paused) ReleasePointer(); }

        public void StartPuzzle()
        {
            if (IsOpen || completing || closing) return;
            if (IsMainGameInputLocked)
            { Debug.LogWarning("Another Euler Path popup is already open.", this); return; }
            if (!enabled) enabled = true;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (!gameObject.activeInHierarchy)
            { Debug.LogError("Euler Path cannot open beneath an inactive parent.", this); return; }
            var data = layoutAsset != null ? layoutAsset.layout : EulerPathDefaultLayout.Create();
            string error;
            if (!IsValidEulerLayout(data, out error))
            { Debug.LogError("Euler Path cannot open: " + error, this); return; }
            EnsureView();
            session = new EulerTraceSession(data, nodeHitRadius, edgeTolerance);
            session.Solved += Complete;
            input.Reset();
            inputLock = new EulerPuzzleInputLock();
            IsOpen = true;
            try
            {
                if (!inputLock.Acquire(transform, pauseGameTime)) { Close(); return; }
                if (!IsOpen || !isActiveAndEnabled) { Close(); return; }
                view.Graph.SetSession(session);
                view.DebugRoot.SetActive(false);
                view.Popup.SetActive(true);
                view.Render(session);
                openedFrame = Time.frameCount;
            }
            catch (Exception exception)
            {
                Close();
                Debug.LogException(exception, this);
            }
        }

        public void CancelPuzzle() { Close(); }

        public void RestartPuzzle()
        {
            if (!IsOpen || completing || session == null) return;
            session.Reset(); input.Reset(); view.Render(session);
            openedFrame = Time.frameCount;
        }

        public static bool IsValidEulerLayout(EulerPathLayout layout, out string error)
        { return EulerPathValidator.IsValidEulerLayout(layout, out error); }

        private void Update()
        {
            if (view != null) view.UpdateSafeArea();
            if (!IsOpen)
            {
                if (!showDebugButton || IsMainGameInputLocked) return;
                EnsureView();
                var frame = input.Poll();
                if (EulerPuzzleInput.DebugPressed() || (frame.down && view.DebugHit(frame.position))) view.InvokeDebug();
                return;
            }
            if (openedFrame == Time.frameCount || completing) return;
            if (EulerPuzzleInput.CancelPressed()) { CancelPuzzle(); return; }
            if (EulerPuzzleInput.RestartPressed()) { RestartPuzzle(); return; }
            var pointer = input.Poll();
            if (pointer.down && view.HandlePopupButton(pointer.position)) return;
            if (session == null) return;
            if (pointer.down) session.Begin(view.Graph.ScreenToGraph(pointer.position));
            // Include the up-frame position: an edge completed just before release still counts.
            if (pointer.held || pointer.up) session.Move(view.Graph.ScreenToGraph(pointer.position));
            if (!IsOpen || session == null) return;
            if (pointer.up) session.Release();
            view.Render(session);
        }

        private void Complete()
        {
            if (!IsOpen || completing || session == null || !session.IsSolved) return;
            completing = true;
            try
            {
                // Notify first; close immediately afterwards, including if a subscriber throws.
                var listeners = OnPuzzleSolved;
                if (listeners != null)
                    foreach (Action listener in listeners.GetInvocationList())
                        try { listener(); }
                        catch (Exception exception) { Debug.LogException(exception, this); }
            }
            finally { Close(); completing = false; }
        }

        private void Close()
        {
            if (closing) return;
            closing = true;
            try
            {
                IsOpen = false; input.Reset();
                if (session != null) { session.Solved -= Complete; session.Release(); session = null; }
                if (view != null && view.Popup != null)
                {
                    view.Popup.SetActive(false); view.Render(null);
                    view.DebugRoot.SetActive(showDebugButton && isActiveAndEnabled);
                }
                openedFrame = -1;
            }
            finally
            {
                try { if (inputLock != null) { inputLock.Dispose(); inputLock = null; } }
                finally { closing = false; }
            }
        }
        private void ReleasePointer()
        {
            input.Reset();
            if (session == null) return;
            session.Release();
            if (view != null) view.Render(session);
        }
        private void EnsureView()
        {
            if (view != null) return;
            view = new EulerPuzzleView(transform, StartPuzzle, RestartPuzzle, CancelPuzzle);
            view.DebugRoot.SetActive(showDebugButton && !IsOpen);
        }
        private void LogSolved() { Debug.Log("Euler path solved", this); }
    }
}
