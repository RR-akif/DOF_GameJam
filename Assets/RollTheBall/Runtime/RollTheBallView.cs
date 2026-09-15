using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RollTheBall
{
    // All generated objects are children of the prefab. Disposal removes the whole view.
    internal sealed class RollTheBallView : IDisposable
    {
        internal GameObject Root { get; private set; }
        internal RectTransform BoardRect { get; private set; }
        internal RectTransform Ball { get; private set; }
        internal Button CloseButton { get; private set; }
        internal float CellSize { get; private set; }
        private RectTransform safeArea;
        private RectTransform panel;
        private Rect lastSafe;
        private int lastWidth, lastHeight;
        private readonly Dictionary<int, RollTheBallTileDrag> tiles = new Dictionary<int, RollTheBallTileDrag>();
        private Text status;
        private Button solveButton;
        private readonly RollTheBallBoard board;
        private static readonly Color Ink = Hex(0xF3E5D1);
        private static readonly Color Muted = Hex(0xB9A996);

        internal RollTheBallView(RollTheBallPuzzleController owner, RollTheBallBoard board, bool debug)
        {
            this.board = board;
            Root = CreateCanvas(owner.transform, "Roll the Ball Popup", 32760);
            try
            {
                // Full-screen opaque raycast target prevents interactions behind the panel.
                var dim = Graphic(Root.transform, "Modal backdrop", Vector2.zero, Vector2.zero, new Color(.025f,.035f,.05f,.9f));
                Stretch(dim.rectTransform);
                dim.raycastTarget = true;
                Root.AddComponent<RollTheBallCancelHandler>().owner = owner;
                safeArea = Rect(Root.transform, "Safe area", Vector2.zero, Vector2.zero);
                panel = Graphic(safeArea, "Puzzle panel", Vector2.zero, new Vector2(760, 950), Hex(0x252B30)).rectTransform;
                Label(panel, "A SLIDING GROOVE PUZZLE", new Vector2(0, 426), new Vector2(660, 26), 13, Muted);
                Label(panel, "ROLL THE BALL", new Vector2(0, 382), new Vector2(660, 60), 38, Ink, FontStyle.Bold);
                Label(panel, "Slide the wooden tiles. Uncover the fixed channel.", new Vector2(0, 336), new Vector2(690, 36), 18, Muted);
                CellSize = 640f / Mathf.Max(board.Columns, board.Rows);
                BoardRect = Rect(panel, "Fixed grid", new Vector2(0, -24), new Vector2(board.Columns * CellSize, board.Rows * CellSize));
                var backing = Graphic(BoardRect, "Board frame", Vector2.zero, BoardRect.sizeDelta + Vector2.one * 18, Hex(0x121B21));
                backing.cornerRadius = 15;
                var floors = Rect(BoardRect, "Fixed cells", Vector2.zero, BoardRect.sizeDelta);
                var grooves = Rect(BoardRect, "Fixed grooves", Vector2.zero, BoardRect.sizeDelta);
                var pieces = Rect(BoardRect, "Movable occupants", Vector2.zero, BoardRect.sizeDelta);
                for (int row = 0; row < board.Rows; row++)
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = new Vector2Int(col, row);
                    var data = board.FloorAt(cell);
                    Color tint = cell == board.Start ? Hex(0x3F7D9C) : cell == board.Goal ? Hex(0xAB5C62) :
                        data.tileType == TileType.Locked ? Hex(0x756D60) : Hex(0x655345);
                    var floor = Graphic(floors, "Floor " + col + "," + row, Position(cell), Vector2.one * (CellSize - 5), tint);
                    floor.cornerRadius = CellSize * .075f;
                    if (data.tileType == TileType.Locked) Screws(floor.rectTransform);
                    DrawGroove(grooves, cell, data);
                    if (board.IsMovable(cell))
                    {
                        int id = board.OccupantAt(cell);
                        var shadow = Graphic(pieces, "Tile " + id, Position(cell), Vector2.one * (CellSize - 7), Hex(0x3A281F));
                        shadow.raycastTarget = true;
                        var wood = Graphic(shadow.transform, "Wood", new Vector2(0, 3), Vector2.one * (CellSize - 9), Hex(0xC89B70));
                        wood.shape = ProceduralPuzzleGraphic.Shape.Wood;
                        wood.cornerRadius = CellSize * .075f;
                        Arrows(wood.rectTransform, board.AxisAt(cell));
                        var drag = shadow.gameObject.AddComponent<RollTheBallTileDrag>();
                        drag.owner = owner;
                        drag.cell = cell;
                        tiles.Add(id, drag);
                    }
                }
                // Goal ring is below moving tiles; start and goal are always fixed supports.
                var ring = Graphic(grooves, "Goal ring", Position(board.Goal), Vector2.one * CellSize * .38f, Hex(0xF1BCB7));
                ring.shape = ProceduralPuzzleGraphic.Shape.Circle;
                var hole = Graphic(ring.transform, "Goal socket", Vector2.zero, Vector2.one * CellSize * .27f, Hex(0x211D24));
                hole.shape = ProceduralPuzzleGraphic.Shape.Circle;
                Ball = Graphic(BoardRect, "Ball", Position(board.Start), Vector2.one * CellSize * .235f, Hex(0xE5EAEF)).rectTransform;
                Ball.GetComponent<ProceduralPuzzleGraphic>().shape = ProceduralPuzzleGraphic.Shape.Ball;
                status = Label(panel, "Follow the arrows on each tile. Blue start  /  Red goal", new Vector2(0, -379), new Vector2(700, 35), 16, Muted);
                CloseButton = MakeButton(panel, "Close", new Vector2(-222, -430), new Vector2(206, 48), owner.CancelPuzzle, Hex(0x414A51));
                CloseButton.gameObject.AddComponent<RollTheBallCancelHandler>().owner = owner;
                if (debug)
                {
                    solveButton = MakeButton(panel, "Debug: auto-solve", new Vector2(191, -430), new Vector2(268, 48), owner.DebugAutoSolve, Hex(0x547A70));
                    solveButton.gameObject.AddComponent<RollTheBallCancelHandler>().owner = owner;
                }
                RefreshScreen(true);
            }
            catch { Dispose(); throw; }
        }

        internal Vector2 Position(Vector2Int cell)
        {
            return new Vector2((cell.x - (board.Columns - 1) * .5f) * CellSize,
                (cell.y - (board.Rows - 1) * .5f) * CellSize);
        }

        internal bool RefreshScreen(bool force = false)
        {
            if (!force && Screen.safeArea == lastSafe && Screen.width == lastWidth && Screen.height == lastHeight) return false;
            lastSafe = Screen.safeArea;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            float width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
            var area = Screen.safeArea;
            if (area.width <= 0 || area.height <= 0) area = new UnityEngine.Rect(0, 0, width, height);
            safeArea.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
            safeArea.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            float scale = Mathf.Min((safeArea.rect.width - 24) / 760f, (safeArea.rect.height - 24) / 950f);
            panel.localScale = Vector3.one * Mathf.Max(.05f, scale);
            return true;
        }

        internal void SyncTiles()
        {
            for (int y = 0; y < board.Rows; y++)
            for (int x = 0; x < board.Columns; x++)
            {
                var cell = new Vector2Int(x, y);
                int id = board.OccupantAt(cell);
                if (id < 0) continue;
                var tile = tiles[id];
                tile.cell = cell;
                ((RectTransform)tile.transform).anchoredPosition = Position(cell);
            }
        }

        internal void SetRolling()
        {
            status.text = "Path clear. Watch the ball roll!";
            foreach (var tile in tiles.Values) tile.GetComponent<ProceduralPuzzleGraphic>().raycastTarget = false;
            if (solveButton) solveButton.interactable = false;
        }

        private void DrawGroove(Transform parent, Vector2Int cell, GridCell data)
        {
            if (!(data.grooveUp || data.grooveDown || data.grooveLeft || data.grooveRight)) return;
            var center = Position(cell);
            for (int layer = 0; layer < 2; layer++)
            {
                float width = CellSize * (layer == 0 ? .36f : .27f);
                Color tint = layer == 0 ? Hex(0x302B28) : Hex(0x171D21);
                Vector2[] offsets = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
                bool[] sides = { data.grooveUp, data.grooveRight, data.grooveDown, data.grooveLeft };
                for (int i = 0; i < 4; i++)
                {
                    if (!sides[i]) continue;
                    var size = i % 2 == 0 ? new Vector2(width, CellSize * .5f) : new Vector2(CellSize * .5f, width);
                    var channel = Graphic(parent, "Fixed channel", center + offsets[i] * CellSize * .25f, size, tint);
                    channel.cornerRadius = 0;
                }
                var join = Graphic(parent, "Rounded channel joint", center, Vector2.one * width, tint);
                join.shape = ProceduralPuzzleGraphic.Shape.Circle;
            }
        }

        private void Screws(RectTransform tile)
        {
            float inset = CellSize * .36f;
            foreach (int x in new[] { -1, 1 })
            foreach (int y in new[] { -1, 1 })
            {
                var screw = Graphic(tile, "Fixed tile screw", new Vector2(x * inset, y * inset), Vector2.one * CellSize * .095f, Hex(0xB9A998));
                screw.shape = ProceduralPuzzleGraphic.Shape.Circle;
                foreach (float angle in new[] { -45f, 45f })
                {
                    var slot = Graphic(screw.transform, "Screw cross", Vector2.zero, new Vector2(CellSize * .068f, CellSize * .015f), Hex(0x4B443B));
                    slot.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }

        private void Arrows(RectTransform tile, SlideAxis axis)
        {
            var center = Rect(tile, "Slide direction", Vector2.zero, Vector2.one * CellSize);
            center.localRotation = Quaternion.Euler(0, 0, axis == SlideAxis.Vertical ? 90 : 0);
            foreach (int sign in new[] { -1, 1 })
            foreach (int half in new[] { -1, 1 })
            {
                var stroke = Graphic(center, "Axis chevron", new Vector2(sign * CellSize * .22f, half * CellSize * .035f),
                    new Vector2(CellSize * .11f, CellSize * .026f), Hex(0x775438));
                stroke.rectTransform.localRotation = Quaternion.Euler(0, 0, -half * sign * 45);
            }
        }

        internal static GameObject CreateCanvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1080);
            scaler.matchWidthOrHeight = .5f;
            return go;
        }

        internal static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        internal static ProceduralPuzzleGraphic Graphic(Transform parent, string name, Vector2 position, Vector2 size, Color tint)
        {
            var rect = Rect(parent, name, position, size);
            var graphic = rect.gameObject.AddComponent<ProceduralPuzzleGraphic>();
            graphic.color = tint;
            graphic.raycastTarget = false;
            return graphic;
        }

        internal static Text Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color tint, FontStyle style = FontStyle.Normal)
        {
            var label = Rect(parent, value, position, size).gameObject.AddComponent<Text>();
#if UNITY_2022_2_OR_NEWER
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = tint;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        internal static Button MakeButton(Transform parent, string title, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick, Color tint)
        {
            var background = Graphic(parent, title, position, size, tint);
            background.raycastTarget = true;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f, 1);
            colors.pressedColor = new Color(.78f, .78f, .78f, 1);
            button.colors = colors;
            button.onClick.AddListener(onClick);
            Label(background.transform, title, Vector2.zero, size - new Vector2(12, 4), 17, Ink);
            return button;
        }

        internal static Color Hex(uint value)
        {
            return new Color(((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f, 1);
        }

        public void Dispose()
        {
            if (Root) { Root.SetActive(false); Object.Destroy(Root); }
            Root = null;
            tiles.Clear();
        }
    }
}
