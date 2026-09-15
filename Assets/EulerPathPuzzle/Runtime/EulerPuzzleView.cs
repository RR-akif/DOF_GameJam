using System;
using UnityEngine;
using UnityEngine.UI;

namespace DegreesOfFreedom.EulerPath
{
    internal sealed class EulerPuzzleView
    {
        public GameObject Popup { get; private set; }
        public GameObject DebugRoot { get; private set; }
        public EulerPathGraphic Graph { get; private set; }
        private Button debugButton, restartButton, cancelButton;
        private Text count, status;
        private RectTransform safeRoot;
        private Rect lastSafeArea;
        private int lastWidth, lastHeight;
        private Font font;
        private readonly Color paper = new Color(0.94f, 0.93f, 1, 1);
        private readonly Color muted = new Color(0.65f, 0.64f, 0.76f, 1);

        public EulerPuzzleView(Transform parent, Action start, Action restart, Action cancel)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("EulerPathCanvas (generated)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            safeRoot = Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one);
            UpdateSafeArea();

            DebugRoot = Rect("DebugTestButton", safeRoot, new Vector2(0, 1), new Vector2(0, 1)).gameObject;
            var debugRect = (RectTransform)DebugRoot.transform;
            debugRect.pivot = new Vector2(0, 1); debugRect.anchoredPosition = new Vector2(20, -20);
            debugRect.sizeDelta = new Vector2(290, 52);
            debugButton = Button("TestOpen", debugRect, Vector2.zero, Vector2.one, "TEST EULER PATH  [F8]", start, true);

            Popup = Rect("Popup", safeRoot, Vector2.zero, Vector2.one).gameObject;
            Fill(Popup, new Color(0.025f, 0.03f, 0.06f, 0.97f));
            var panel = Rect("Panel", Popup.transform, new Vector2(0.06f, 0.045f), new Vector2(0.94f, 0.955f));
            Fill(panel.gameObject, new Color(0.075f, 0.082f, 0.14f, 1));
            Label("CaseLabel", panel, new Vector2(0.06f, 0.91f), new Vector2(0.65f, 0.965f), "FIELD ANALYSIS  /  TRACE LOCK", 17, muted, TextAnchor.MiddleLeft);
            Label("Title", panel, new Vector2(0.06f, 0.83f), new Vector2(0.68f, 0.92f), "EULER PATH", 42, paper, TextAnchor.MiddleLeft);
            count = Label("Progress", panel, new Vector2(0.72f, 0.865f), new Vector2(0.94f, 0.94f), "", 31,
                new Color(1, 0.81f, 0.25f), TextAnchor.MiddleRight);
            Label("ProgressCaption", panel, new Vector2(0.72f, 0.82f), new Vector2(0.94f, 0.87f), "EDGES TRACED", 13, muted, TextAnchor.MiddleRight);
            var line = Rect("Divider", panel, new Vector2(0.06f, 0.812f), new Vector2(0.94f, 0.814f));
            Fill(line.gameObject, new Color(0.23f, 0.24f, 0.35f, 1));
            var graphRect = Rect("Graph (generated mesh)", panel, new Vector2(0.06f, 0.215f), new Vector2(0.94f, 0.80f));
            Graph = graphRect.gameObject.AddComponent<EulerPathGraphic>();
            Graph.raycastTarget = false;
            status = Label("Status", panel, new Vector2(0.045f, 0.16f), new Vector2(0.955f, 0.22f), "", 20, paper, TextAnchor.MiddleCenter);
            Label("Rules", panel, new Vector2(0.04f, 0.112f), new Vector2(0.96f, 0.162f),
                "Drag along every edge once. If released, resume at the highlighted node.", 15, muted, TextAnchor.MiddleCenter);
            restartButton = Button("Restart", panel, new Vector2(0.06f, 0.035f), new Vector2(0.31f, 0.103f), "RESTART  [R]", restart, false);
            cancelButton = Button("Cancel", panel, new Vector2(0.69f, 0.035f), new Vector2(0.94f, 0.103f), "CLOSE  [ESC]", cancel, false);
            Popup.SetActive(false);
        }

        public void UpdateSafeArea()
        {
            Rect area = Screen.safeArea;
            if (area == lastSafeArea && lastWidth == Screen.width && lastHeight == Screen.height) return;
            lastSafeArea = area; lastWidth = Screen.width; lastHeight = Screen.height;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safeRoot.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }

        public bool DebugHit(Vector2 p) { return DebugRoot.activeInHierarchy && Hit(debugButton, p); }
        public void InvokeDebug() { debugButton.onClick.Invoke(); }
        public bool HandlePopupButton(Vector2 p)
        {
            if (Hit(cancelButton, p)) { cancelButton.onClick.Invoke(); return true; }
            if (Hit(restartButton, p)) { restartButton.onClick.Invoke(); return true; }
            return false;
        }
        public void Render(EulerTraceSession session)
        {
            if (session == null) { count.text = ""; status.text = ""; Graph.SetSession(null); return; }
            count.text = session.TracedCount.ToString("00") + " / " + session.Layout.edges.Length.ToString("00");
            status.text = session.Hint;
            Graph.Refresh();
        }

        private static bool Hit(Button b, Vector2 p)
        {
            return b.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)b.transform, p, null);
        }
        private Button Button(string name, Transform parent, Vector2 min, Vector2 max, string text, Action action, bool accent)
        {
            var rect = Rect(name, parent, min, max);
            var fill = Fill(rect.gameObject, accent ? new Color(0.95f, 0.78f, 0.30f, 1) : new Color(0.17f, 0.18f, 0.29f, 1));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => action());
            Label("Label", rect, Vector2.zero, Vector2.one, text, 17,
                accent ? new Color(0.10f, 0.11f, 0.18f, 1) : paper, TextAnchor.MiddleCenter);
            return button;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static Image Fill(GameObject go, Color color)
        {
            var image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }
        private Text Label(string name, Transform parent, Vector2 min, Vector2 max, string value, int size, Color color, TextAnchor alignment)
        {
            var label = Rect(name, parent, min, max).gameObject.AddComponent<Text>();
            label.font = font; label.text = value; label.fontSize = size; label.color = color;
            label.alignment = alignment; label.raycastTarget = false;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 10; label.resizeTextMaxSize = size;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
    }
}
