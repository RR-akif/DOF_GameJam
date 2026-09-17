using UnityEngine;

public class MagicCubeAutoPuzzleTrigger : MonoBehaviour
{
    private bool puzzleTriggered = false;
    private bool puzzleCompleted = false;


    // =========================================================
    // PLAYER ENTERS THE MAGIC CUBE TRIGGER
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        // Only react to the player.
        if (!other.CompareTag("Player"))
            return;

        // If puzzle is already completed,
        // never open it again.
        if (puzzleCompleted)
            return;

        // Prevent opening multiple times
        // while it is already active.
        if (puzzleTriggered)
            return;

        if (Level3FlowController.Instance == null)
        {
            Debug.LogError(
                "MagicCubeAutoPuzzleTrigger: Level3FlowController was not found."
            );

            return;
        }

        // Mark puzzle as currently triggered.
        puzzleTriggered = true;

        // Tell Level3FlowController to open RollTheBall.
        Level3FlowController.Instance
            .StartRollTheBallPuzzle();
    }


    // =========================================================
    // CALLED WHEN PLAYER PRESSES CLOSE / FORFEIT
    // =========================================================

    public void ResetAfterCancel()
    {
        // If it was already solved,
        // never allow it to reopen.
        if (puzzleCompleted)
            return;

        // Allow the player to come back
        // and activate the puzzle again.
        puzzleTriggered = false;
    }


    // =========================================================
    // CALLED WHEN PUZZLE IS SOLVED
    // =========================================================

    public void MarkCompleted()
    {
        puzzleCompleted = true;

        // Keep this true permanently,
        // so the puzzle cannot reopen.
        puzzleTriggered = true;
    }
}