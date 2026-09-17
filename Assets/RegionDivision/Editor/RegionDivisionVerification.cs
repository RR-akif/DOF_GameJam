#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RegionDivision.Editor
{
    /// <summary>No test framework dependency. Rule checks can also run in Unity
    /// batchmode with -executeMethod RegionDivision.Editor.RegionDivisionVerification.RunModelChecks.</summary>
    public static class RegionDivisionVerification
    {
        [MenuItem("Tools/Region Division/Run Rule Checks")]
        public static void RunModelChecks()
        {
            RegionDivisionRuleChecks.RunAll();
            Debug.Log("Region Division: all rule checks passed.");
        }

        [MenuItem("Tools/Region Division/Run Lifecycle Checks (Play Mode)")]
        public static void RunLifecycleChecks()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            if (RegionDivisionInputLock.IsLocked) throw new InvalidOperationException("Close the current puzzle before testing.");
            GameObject root = new GameObject("Region Division Lifecycle Test");
            var puzzle = root.AddComponent<RegionDivisionPuzzleController>();
            EventSystem previous = EventSystem.current;
            bool priorEnabled = previous != null && previous.enabled;
            CursorLockMode priorLock = Cursor.lockState;
            bool priorVisible = Cursor.visible;
            int solved = 0;
            puzzle.OnPuzzleSolved += () => {
                Assert(puzzle.IsOpen, "The solved event must precede auto-close.");
                Assert(puzzle.IsGridValidAndComplete, "The grid must validate during the callback.");
                solved++;
                puzzle.StartPuzzle(); // Reentrant opening must be ignored during completion.
            };
            try
            {
                Assert(!puzzle.IsOpen && puzzle.ClaimedCellCount == 0, "Starts closed and empty.");
                puzzle.StartPuzzle();
                Assert(puzzle.IsOpen && RegionDivisionInputLock.IsLocked, "Opening acquires input.");
                if (previous != null) Assert(!previous.enabled, "Host EventSystem is suspended.");
                CheckPointerInteraction(puzzle);
                string error;
                Assert(puzzle.TryClaimRegion(0, 0, 2, 1, out error), "A scripted valid claim works.");
                Assert(puzzle.TryUnclaimRegion(1, 1), "Any filled cell can unclaim the region.");
                Assert(puzzle.ClaimedCellCount == 0, "Unclaim clears every cell in the region.");
                Assert(!puzzle.TryClaimRegion(0, 0, 0, 1, out error), "Invalid claim is rejected.");
                Assert(puzzle.ClaimedCellCount == 0, "Invalid claim leaves no state.");
                puzzle.TryClaimRegion(0, 0, 2, 1, out error);
                puzzle.StartPuzzle();
                Assert(puzzle.IsOpen && puzzle.ClaimedCellCount == 0, "Repeated StartPuzzle starts fresh.");
                puzzle.CancelPuzzle();
                puzzle.CancelPuzzle();
                Assert(!puzzle.IsOpen && puzzle.ClaimedCellCount == 0 && solved == 0, "Cancel is idempotent and silent.");
                if (previous != null) Assert(previous.enabled == priorEnabled, "Cancel restores the EventSystem.");
                Assert(Cursor.lockState == priorLock && Cursor.visible == priorVisible, "Cancel restores the cursor.");
                puzzle.StartPuzzle();
                puzzle.TryClaimRegion(0, 0, 2, 1, out error);
                puzzle.DebugAutoSolve();
                Assert(solved == 0 && puzzle.IsOpen && puzzle.ClaimedCellCount == 6, "Disabled auto-solve leaves the session unchanged.");
                CompleteTestBoard(puzzle, 1);
                Assert(solved == 1 && !puzzle.IsOpen && puzzle.ClaimedCellCount == 0, "Normal completion fires once, closes, and clears.");
                puzzle.DebugAutoSolve();
                Assert(solved == 1, "A closed puzzle cannot solve again.");
                puzzle.StartPuzzle();
                Assert(puzzle.ClaimedCellCount == 0, "Reopening after success starts fresh.");
                puzzle.enabled = false;
                Assert(!puzzle.IsOpen, "Disabling the component releases the session.");
                if (previous != null) Assert(previous.enabled == priorEnabled, "Disabling restores the EventSystem.");
                puzzle.StartPuzzle();
                root.SetActive(false);
                Assert(!puzzle.IsOpen, "Deactivating the root releases the session.");
                puzzle.StartPuzzle();
                CompleteTestBoard(puzzle, 0);
                Assert(solved == 2 && !puzzle.IsOpen, "A deactivated root can be explicitly restarted.");
                Debug.Log("Region Division: all lifecycle checks passed. Raw-input release is guarded through the current frame.");
            }
            finally
            {
                puzzle.CancelPuzzle();
                UnityEngine.Object.Destroy(root);
            }
        }

        [MenuItem("Tools/Region Division/Select Prefab")]
        private static void SelectPrefab()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RegionDivision/Prefabs/RegionDivisionPuzzle.prefab");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private static void Assert(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Region Division lifecycle check failed: " + message); }

        // Only exercises the disposable test controller; no gameplay auto-solve hook.
        private static void CompleteTestBoard(RegionDivisionPuzzleController puzzle, int firstRegion)
        {
            RegionDivisionLayout layout = RegionDivisionLayouts.CreateDefault();
            AuthoredRegion[] regions = RegionDivisionLayouts.CreateDefaultSolution();
            for (int i = firstRegion; i < regions.Length; i++)
            {
                AuthoredRegion region = regions[i];
                ClueCell clue = layout.clues[region.clueIndex];
                string error;
                Assert(puzzle.TryClaimRegion(clue.col, clue.row, region.oppositeCol, region.oppositeRow, out error),
                    "Test partition claim: " + error);
            }
        }

        private static void CheckPointerInteraction(RegionDivisionPuzzleController puzzle)
        {
            var input = puzzle.GetComponentInChildren<RegionDivisionGridInput>();
            Assert(input != null, "A generated grid has an input surface.");
            var grid = (RectTransform)input.transform;
            var pointer = new PointerEventData(EventSystem.current) {
                pointerId = -1, button = PointerEventData.InputButton.Left
            };
            pointer.position = CellCenter(grid, 1, 0);
            input.OnPointerDown(pointer);
            pointer.position = CellCenter(grid, 2, 2);
            input.OnDrag(pointer); input.OnPointerUp(pointer);
            Assert(puzzle.ClaimedCellCount == 0, "Dragging from a non-clue cannot start a claim.");

            pointer.position = CellCenter(grid, 0, 0);
            input.OnPointerDown(pointer);
            pointer.position = CellCenter(grid, 2, 1);
            input.OnDrag(pointer);
            Assert(puzzle.ClaimedCellCount == 0, "Preview is never committed before release.");
            var secondPointer = new PointerEventData(EventSystem.current) {
                pointerId = 99, button = PointerEventData.InputButton.Left, position = pointer.position
            };
            input.OnPointerUp(secondPointer);
            Assert(puzzle.ClaimedCellCount == 0, "A second pointer cannot complete the active drag.");
            input.OnPointerUp(pointer);
            input.OnEndDrag(pointer);
            Assert(puzzle.ClaimedCellCount == 6, "Pointer-up claims once; end-drag cannot duplicate it.");

            pointer.position = CellCenter(grid, 1, 0);
            input.OnPointerDown(pointer);
            pointer.position = CellCenter(grid, 2, 1);
            input.OnDrag(pointer); input.OnPointerUp(pointer);
            Assert(puzzle.ClaimedCellCount == 6, "Dragging a filled region does not undo it.");
            pointer.position = CellCenter(grid, 1, 1);
            input.OnPointerDown(pointer); input.OnPointerUp(pointer);
            Assert(puzzle.ClaimedCellCount == 0, "A tap on a non-clue filled cell undoes the region.");

            pointer.position = CellCenter(grid, 0, 0);
            input.OnPointerDown(pointer);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null,
                grid.TransformPoint(new Vector3(grid.rect.xMax + 10, grid.rect.center.y, 0)));
            input.OnDrag(pointer); input.OnPointerUp(pointer);
            Assert(puzzle.ClaimedCellCount == 0, "Release outside the grid rejects and clears the gesture.");

            pointer.position = CellCenter(grid, 0, 0);
            input.OnPointerDown(pointer);
            puzzle.SendMessage("OnApplicationFocus", false);
            pointer.position = CellCenter(grid, 2, 1);
            input.OnPointerUp(pointer);
            Assert(puzzle.ClaimedCellCount == 0, "Focus loss cancels a captured pointer.");
        }

        private static Vector2 CellCenter(RectTransform grid, int col, int row)
        {
            Rect bounds = grid.rect;
            Vector3 local = new Vector3(bounds.xMin + (col + 0.5f) * bounds.width / 7f,
                bounds.yMax - (row + 0.5f) * bounds.height / 6f, 0);
            return RectTransformUtility.WorldToScreenPoint(null, grid.TransformPoint(local));
        }
    }
}
#endif
