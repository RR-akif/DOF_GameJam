using UnityEngine;

/// <summary>Compatibility shim for the removed menu in previously generated scenes.</summary>
public sealed class MazePuzzleTestButton : MonoBehaviour
{
    private void Awake()
    {
        // This was the named menu root created by the previous setup command.
        // Hide the whole obsolete menu, including its text and buttons.
        if (gameObject.name == "TemporaryTestUI") gameObject.SetActive(false);
    }

    public void OpenMaze()
    {
        if (MazePuzzleController.Instance == null)
        {
            Debug.LogError("Place the generated MazePuzzlePopup prefab in this test scene.", this);
            return;
        }
        MazePuzzleController.Instance.StartPuzzle();
    }
}
