using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Disables only explicitly supplied main-game scripts; no input-system assumptions.</summary>
internal sealed class MazeInputIsolation
{
    private struct Snapshot
    {
        public MonoBehaviour Component;
        public bool WasEnabled;
    }
    private readonly List<Snapshot> snapshots = new List<Snapshot>();
    private bool acquired;
    private bool cursorWasVisible;
    private CursorLockMode cursorLock;
    private EventSystem eventSystem;
    private GameObject selectedBefore;
    private bool navigationBefore;

    public void Acquire(MonoBehaviour[] controllers, Transform puzzleRoot)
    {
        if (acquired) return;
        acquired = true;
        cursorWasVisible = Cursor.visible;
        cursorLock = Cursor.lockState;
        eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            navigationBefore = eventSystem.sendNavigationEvents;
            selectedBefore = eventSystem.currentSelectedGameObject;
            eventSystem.sendNavigationEvents = false;
            eventSystem.SetSelectedGameObject(null);
        }
        var seen = new HashSet<MonoBehaviour>();
        if (controllers != null)
        {
            foreach (MonoBehaviour component in controllers)
            {
                if (component == null || !seen.Add(component)) continue;
                if (component.transform == puzzleRoot || component.transform.IsChildOf(puzzleRoot))
                {
                    Debug.LogWarning("Main Game Controllers must reference main-game scripts outside MazePuzzlePopup.", component);
                    continue;
                }
                snapshots.Add(new Snapshot { Component = component, WasEnabled = component.enabled });
                component.enabled = false;
            }
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Release()
    {
        if (!acquired) return;
        acquired = false;
        foreach (Snapshot snapshot in snapshots)
            if (snapshot.Component != null) snapshot.Component.enabled = snapshot.WasEnabled;
        snapshots.Clear();
        if (eventSystem != null)
        {
            eventSystem.sendNavigationEvents = navigationBefore;
            if (selectedBefore != null && selectedBefore.activeInHierarchy)
                eventSystem.SetSelectedGameObject(selectedBefore);
        }
        eventSystem = null;
        selectedBefore = null;
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorWasVisible;
    }
}
