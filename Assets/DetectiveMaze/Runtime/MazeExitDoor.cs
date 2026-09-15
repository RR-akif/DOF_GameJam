using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class MazeExitDoor : MonoBehaviour
{
    private MazePuzzleController owner;
    private bool consumed;

    public void Initialize(MazePuzzleController controller)
    {
        owner = controller;
        consumed = false;
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryComplete(other);

    // Also handles a detective already overlapping the doorway when it is enabled.
    private void OnTriggerStay2D(Collider2D other) => TryComplete(other);

    private void TryComplete(Collider2D other)
    {
        if (consumed || owner == null || !owner.IsOpen) return;
        MazeDetectiveMovement detective = other.GetComponentInParent<MazeDetectiveMovement>();
        // Tags and unrelated 2D colliders cannot complete the maze.
        if (detective == null || detective.Owner != owner) return;
        if (MazePuzzleController.Instance != owner) return;
        consumed = true;
        MazePuzzleController.Instance.CompletePuzzle();
    }
}
