using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RegionDivision
{
    /// <summary>All visuals are solid UI meshes and built-in font text. No sprites,
    /// textures, resources folders, scene cameras, or prefab art dependencies.</summary>
    internal sealed class RegionDivisionView
    {
        internal GameObject Root { get; private set; }
        internal RectTransform Grid { get; private set; }
        private readonly Image[] cells;
        private readonly Image preview;
        private readonly Text status;
        private readonly Text progress;
        private readonly Color[] palette;
        private readonly RegionDivisionBoard board;
        private readonly Font font;
        private static readonly Color Ink = new Color32(28, 35, 50, 255);
        private static readonly Color Empty = new Color32(241, 240, 234, 255);

        internal RegionDivisionView(Transform parent, RegionDivisionPuzzleController controller,
            RegionDivisionBoard board, Color[] palette)
        {
            this.board = board;
            this.palette = palette;
#if UNITY_2022_2_OR_NEWER
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            Root = new GameObject("Region Division Popup", typeof(RectTransform));
            Root.SetActive(false);
            Root.transform.SetParent(parent, false);
            var canvas = Root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            var scaler = Root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 900);
            scaler.matchWidthOrHeight = 1f;
            Root.AddComponent<GraphicRaycaster>();

            Image shade = MakeImage("Modal Blocker", Root.transform, new Color(0.02f, 0.03f, 0.06f, 0.90f), true);
            Stretch(shade.rectTransform);
            Image panel = MakeImage("Panel", shade.transform, new Color32(30, 37, 53, 255));
            panel.rectTransform.anchorMin = new Vector2(0.04f, 0.04f);
            panel.rectTransform.anchorMax = new Vector2(0.96f, 0.96f);
            panel.rectTransform.offsetMin = panel.rectTransform.offsetMax = Vector2.zero;

            Text title = MakeText("Title", panel.transform, "REGION DIVISION", 32, Color.white, FontStyle.Bold);
            Top(title.rectTransform, 22, 44, 24);
            Text instructions = MakeText("Instructions", panel.transform,
                "Drag from a number to make its rectangle.\nOne number per region. Tap a filled region to undo.", 20, new Color32(208, 216, 230, 255));
            Top(instructions.rectTransform, 73, 55, 16);

            RectTransform boardHost = MakeRect("Grid Area", panel.transform);
            Stretch(boardHost);
            boardHost.offsetMin = new Vector2(24, 170);
            boardHost.offsetMax = new Vector2(-24, -147);
            Image gridImage = MakeImage("Grid", boardHost, Ink, true);
            Grid = gridImage.rectTransform;
            var fit = gridImage.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)board.Columns / board.Rows;
            gridImage.gameObject.AddComponent<RegionDivisionGridInput>().Initialize(controller);

            cells = new Image[board.Columns * board.Rows];
            for (int row = 0; row < board.Rows; row++)
                for (int col = 0; col < board.Columns; col++)
                {
                    Image cell = MakeImage("Cell " + col + "," + row, Grid, Empty);
                    Place(cell.rectTransform, new RegionRectangle(col, row, col, row), 2f);
                    cells[row * board.Columns + col] = cell;
                }
            preview = MakeImage("Drag Preview", Grid, new Color(0.25f, 0.85f, 0.5f, 0.45f));
            preview.gameObject.SetActive(false);
            // Labels stay above the preview. Every visible cell graphic ignores raycasts;
            // the whole grid is the one stable pointer target.
            for (int i = 0; i < board.ClueCount; i++)
            {
                ClueCell clue = board.GetClue(i);
                Text label = MakeText("Clue " + i, Grid, clue.targetCellCount.ToString(), 40, Ink, FontStyle.Bold);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = 44;
                Place(label.rectTransform, new RegionRectangle(clue.col, clue.row, clue.col, clue.row), 5);
            }

            progress = MakeText("Progress", panel.transform, "", 19, new Color32(191, 205, 226, 255));
            Bottom(progress.rectTransform, 130, 28, 20);
            status = MakeText("Status", panel.transform, "", 19, Color.white);
            Bottom(status.rectTransform, 77, 46, 20);
            MakeButton("Forfeit", panel.transform, "Forfeit", controller.CancelPuzzle,
                0.30f, 0.70f, new Color32(61, 73, 94, 255));
            Refresh();
            SetStatus("Start on any unclaimed number.");
        }

        internal void Refresh()
        {
            for (int row = 0; row < board.Rows; row++)
                for (int col = 0; col < board.Columns; col++)
                {
                    int owner = board.OwnerAt(col, row);
                    cells[row * board.Columns + col].color = owner < 0 ? Empty : palette[owner % palette.Length];
                }
            progress.text = board.ClaimedCellCount + " / " + (board.Columns * board.Rows)
                + " cells     |     " + board.RegionCount + " / " + board.ClueCount + " regions";
        }

        internal void SetStatus(string message) { status.text = message; }

        internal void ShowPreview(RegionRectangle rect, bool valid)
        {
            Place(preview.rectTransform, rect, 1f);
            preview.color = valid ? new Color(0.16f, 0.77f, 0.42f, 0.50f) : new Color(1f, 0.23f, 0.27f, 0.45f);
            preview.gameObject.SetActive(true);
        }

        internal void HidePreview() { preview.gameObject.SetActive(false); }

        private void Place(RectTransform transform, RegionRectangle rect, float inset)
        {
            transform.anchorMin = new Vector2((float)rect.MinCol / board.Columns, 1f - (float)(rect.MaxRow + 1) / board.Rows);
            transform.anchorMax = new Vector2((float)(rect.MaxCol + 1) / board.Columns, 1f - (float)rect.MinRow / board.Rows);
            transform.offsetMin = new Vector2(inset, inset);
            transform.offsetMax = new Vector2(-inset, -inset);
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image MakeImage(string name, Transform parent, Color color, bool raycast = false)
        {
            Image image = MakeRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private Text MakeText(string name, Transform parent, string value, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            Text label = MakeRect(name, parent).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        private void MakeButton(string name, Transform parent, string value, UnityAction action,
            float left, float right, Color color)
        {
            Image image = MakeImage(name, parent, color, true);
            image.rectTransform.anchorMin = new Vector2(left, 0);
            image.rectTransform.anchorMax = new Vector2(right, 0);
            image.rectTransform.offsetMin = new Vector2(0, 22);
            image.rectTransform.offsetMax = new Vector2(0, 68);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);
            Text label = MakeText("Label", image.transform, value, 18, Color.white, FontStyle.Bold);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(6, 2);
            label.rectTransform.offsetMax = new Vector2(-6, -2);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 18;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Top(RectTransform rect, float top, float height, float side)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(side, -top - height);
            rect.offsetMax = new Vector2(-side, -top);
        }

        private static void Bottom(RectTransform rect, float bottom, float height, float side)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0);
            rect.offsetMin = new Vector2(side, bottom);
            rect.offsetMax = new Vector2(-side, bottom + height);
        }
    }
}
