using UnityEngine;

/// <summary>Test-scene input-reader stand-in; proves enabled-state and Update isolation.</summary>
public sealed class MazeControllerProbe : MonoBehaviour
{
    public int TickCount { get; private set; }
    private void Update() => TickCount++;
}
