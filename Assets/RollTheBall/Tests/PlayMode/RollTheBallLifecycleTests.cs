#if UNITY_EDITOR
using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RollTheBall.Tests
{
    public sealed class TestInputParticipant : MonoBehaviour, IRollTheBallInputParticipant
    {
        public bool gameplayEnabled = true;
        private sealed class Token : IDisposable
        {
            private readonly TestInputParticipant owner;
            private readonly bool previous;
            public Token(TestInputParticipant owner) { this.owner = owner; previous = owner.gameplayEnabled; owner.gameplayEnabled = false; }
            public void Dispose() { if (owner) owner.gameplayEnabled = previous; }
        }
        public IDisposable AcquirePuzzleInputLock() { return new Token(this); }
    }

    public sealed class RollTheBallLifecycleTests
    {
        private RollTheBallPuzzleController puzzle;
        private GameObject host;
        private EventSystem hostSystem;
        private TestInputParticipant participant;
        private float priorTimeScale;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            priorTimeScale = Time.timeScale;
            host = new GameObject("Test host input", typeof(EventSystem), typeof(TestInputParticipant));
            hostSystem = host.GetComponent<EventSystem>();
            participant = host.GetComponent<TestInputParticipant>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RollTheBall/Prefabs/RollTheBallPuzzle.prefab");
            Assert.That(prefab, Is.Not.Null);
            puzzle = Object.Instantiate(prefab).GetComponent<RollTheBallPuzzleController>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Time.timeScale = priorTimeScale;
            if (puzzle) Object.Destroy(puzzle.gameObject);
            if (host) Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosedPrefabHasNoPopupAndDebugLauncherCanOpenIt()
        {
            Assert.That(puzzle.IsOpen, Is.False);
            Assert.That(puzzle.transform.Find("Roll the Ball Popup"), Is.Null);
            var button = puzzle.GetComponentInChildren<UnityEngine.UI.Button>();
            Assert.That(button, Is.Not.Null);
            button.onClick.Invoke();
            Assert.That(puzzle.IsOpen, Is.True);
            Assert.That(RollTheBallInputLock.IsLocked, Is.True);
            Assert.That(hostSystem.enabled, Is.False);
            Assert.That(participant.gameplayEnabled, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CancelRestoresInputAndRestartResetsOccupancy()
        {
            int solved = 0;
            puzzle.OnPuzzleSolved += () => solved++;
            puzzle.StartPuzzle();
            Assert.That(puzzle.TrySlideTile(new Vector2Int(1,2), new Vector2Int(1,4)), Is.True);
            puzzle.CancelPuzzle();
            puzzle.CancelPuzzle(); // idempotent
            Assert.That(puzzle.IsOpen, Is.False);
            Assert.That(RollTheBallInputLock.IsLocked, Is.False);
            Assert.That(hostSystem.enabled, Is.True);
            Assert.That(participant.gameplayEnabled, Is.True);
            Assert.That(solved, Is.Zero);
            puzzle.StartPuzzle();
            Assert.That(puzzle.TrySlideTile(new Vector2Int(1,2), new Vector2Int(1,4)), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PreviouslyDisabledGameplayRemainsDisabled()
        {
            participant.gameplayEnabled = false;
            hostSystem.enabled = false;
            puzzle.StartPuzzle();
            puzzle.CancelPuzzle();
            Assert.That(participant.gameplayEnabled, Is.False);
            Assert.That(hostSystem.enabled, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutoSolveFromChangedBoardRollsAtTimeScaleZeroAndFiresBeforeClose()
        {
            Time.timeScale = 0;
            int solved = 0;
            bool openAtEvent = false;
            bool goalAtEvent = false;
            puzzle.OnPuzzleSolved += () => { solved++; openAtEvent = puzzle.IsOpen; goalAtEvent = puzzle.BallCell == new Vector2Int(4,1); };
            puzzle.BallCellsPerSecond = 1000;
            puzzle.StartPuzzle();
            Assert.That(puzzle.TrySlideTile(new Vector2Int(3,1), new Vector2Int(2,1)), Is.True);
            Assert.That(puzzle.TrySlideTile(new Vector2Int(4,0), new Vector2Int(3,0)), Is.True);
            puzzle.DebugAutoSolve();
            Assert.That(puzzle.IsRolling, Is.True);
            Assert.That(solved, Is.Zero);
            Assert.That(puzzle.TrySlideTile(new Vector2Int(1,4), new Vector2Int(1,2)), Is.False);
            for (int i = 0; i < 120 && puzzle.IsOpen; i++) yield return null;
            Assert.That(solved, Is.EqualTo(1));
            Assert.That(openAtEvent && goalAtEvent, Is.True);
            Assert.That(puzzle.IsOpen, Is.False);
            Assert.That(RollTheBallInputLock.IsLocked, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CancelDuringRollNeverFiresSuccessAndExternalDisableRestoresInput()
        {
            int solved = 0;
            puzzle.OnPuzzleSolved += () => solved++;
            puzzle.BallCellsPerSecond = .1f;
            puzzle.StartPuzzle();
            puzzle.DebugAutoSolve();
            yield return null;
            puzzle.CancelPuzzle();
            Assert.That(solved, Is.Zero);
            Assert.That(RollTheBallInputLock.IsLocked, Is.False);
            puzzle.StartPuzzle();
            puzzle.gameObject.SetActive(false);
            Assert.That(RollTheBallInputLock.IsLocked, Is.False);
            Assert.That(participant.gameplayEnabled, Is.True);
            yield return null;
            Assert.That(solved, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ThrowingSubscriberCannotPreventOtherSubscribersOrAutoClose()
        {
            int notified = 0;
            puzzle.OnPuzzleSolved += () => { throw new InvalidOperationException("Expected subscriber failure"); };
            puzzle.OnPuzzleSolved += () => notified++;
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Expected subscriber failure"));
            puzzle.BallCellsPerSecond = 1000;
            puzzle.StartPuzzle();
            puzzle.DebugAutoSolve();
            for (int i = 0; i < 120 && puzzle.IsOpen; i++) yield return null;
            Assert.That(notified, Is.EqualTo(1));
            Assert.That(puzzle.IsOpen, Is.False);
            Assert.That(RollTheBallInputLock.IsLocked, Is.False);
        }

        [UnityTest]
        public IEnumerator SuccessSubscriberMayImmediatelyOpenANewRun()
        {
            int solved = 0;
            puzzle.OnPuzzleSolved += () => { solved++; puzzle.StartPuzzle(); };
            puzzle.BallCellsPerSecond = 1000;
            puzzle.StartPuzzle();
            puzzle.DebugAutoSolve();
            for (int i = 0; i < 120 && solved == 0; i++) yield return null;
            Assert.That(solved, Is.EqualTo(1));
            Assert.That(puzzle.IsOpen, Is.True);
            Assert.That(puzzle.IsRolling, Is.False);
            Assert.That(puzzle.TrySlideTile(new Vector2Int(1,2), new Vector2Int(1,4)), Is.True);
        }

        [UnityTest]
        public IEnumerator DragClampsToAxisIgnoresSecondPointerAndSnaps()
        {
            puzzle.StartPuzzle();
            yield return null;
            RollTheBallTileDrag tile = null;
            foreach (var candidate in puzzle.GetComponentsInChildren<RollTheBallTileDrag>())
                if (candidate.cell == new Vector2Int(1,2)) tile = candidate;
            Assert.That(tile, Is.Not.Null);
            var rect = (RectTransform)tile.transform;
            var origin = rect.anchoredPosition;
            float step = ((RectTransform)rect.parent).rect.width / 5;
            var e = new PointerEventData(EventSystem.current) { pointerId = 11, button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.position) };
            tile.OnBeginDrag(e);
            e.position = RectTransformUtility.WorldToScreenPoint(null, rect.parent.TransformPoint(origin + new Vector2(step * 5, step * 1.7f)));
            tile.OnDrag(e);
            Assert.That(rect.anchoredPosition.x, Is.EqualTo(origin.x).Within(.01));
            var second = new PointerEventData(EventSystem.current) { pointerId = 12, position = e.position };
            tile.OnEndDrag(second);
            Assert.That(tile.cell, Is.EqualTo(new Vector2Int(1,2)));
            tile.OnEndDrag(e);
            Assert.That(tile.cell, Is.EqualTo(new Vector2Int(1,4)));
            Assert.That(rect.anchoredPosition.y, Is.EqualTo(origin.y + step * 2).Within(.01));
        }

        [UnityTest]
        public IEnumerator SecondPrefabCancelsFirstWithoutSharingRunState()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RollTheBall/Prefabs/RollTheBallPuzzle.prefab");
            var second = Object.Instantiate(prefab).GetComponent<RollTheBallPuzzleController>();
            try
            {
                puzzle.StartPuzzle();
                second.StartPuzzle();
                Assert.That(puzzle.IsOpen, Is.False);
                Assert.That(second.IsOpen, Is.True);
                second.CancelPuzzle();
                Assert.That(RollTheBallInputLock.IsLocked, Is.False);
                Assert.That(participant.gameplayEnabled, Is.True);
            }
            finally { Object.Destroy(second.gameObject); }
            yield return null;
        }
    }
}
#endif
