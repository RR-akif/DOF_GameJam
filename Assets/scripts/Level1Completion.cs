using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CircuitPuzzle;

public class Level1Completion : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private PuzzlePopup puzzlePopup;

    [Header("Completion Screen")]
    [SerializeField] private GameObject levelCompleteCanvas;

    [Header("Next Level")]
    [SerializeField] private string level2SceneName = "Level2";

    [Header("Timing")]
    [SerializeField] private float displayTime = 4f;

    private bool transitionStarted = false;

    public void PuzzleSolved()
    {
        if (transitionStarted)
            return;

        transitionStarted = true;

        StartCoroutine(LevelCompleteSequence());
    }

    private IEnumerator LevelCompleteSequence()
    {
        // Close puzzle immediately
        if (puzzlePopup != null)
        {
            puzzlePopup.Close();
        }

        // Make sure time is running again
        Time.timeScale = 1f;

        // Show completion image
        if (levelCompleteCanvas != null)
        {
            levelCompleteCanvas.SetActive(true);
        }

        // Wait while image is visible
        yield return new WaitForSecondsRealtime(displayTime);

        // Load Level 2
        SceneManager.LoadScene(level2SceneName);
    }
}