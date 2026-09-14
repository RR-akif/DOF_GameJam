using UnityEngine;
using UnityEngine.Video;
using CircuitPuzzle;

public class PoliceConversation : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject talkPrompt;
    [SerializeField] private GameObject conversationCanvas;

    [Header("Conversation Video")]
    [SerializeField] private VideoPlayer conversationVideoPlayer;

    [Header("Player")]
    [SerializeField] private MonoBehaviour playerMovementScript;

    [Header("Circuit Puzzle")]
    [SerializeField] private PuzzlePopup puzzlePopup;

    private bool playerNearby = false;
    private bool conversationPlaying = false;

    // Prevents this conversation from being started again
    // after the video has already led into the puzzle.
    private bool interactionCompleted = false;

    private void Start()
    {
        // Talk prompt is hidden at the beginning.
        if (talkPrompt != null)
        {
            talkPrompt.SetActive(false);
        }

        // Conversation-video canvas is hidden initially.
        if (conversationCanvas != null)
        {
            conversationCanvas.SetActive(false);
        }

        // Detect natural end of conversation video.
        if (conversationVideoPlayer != null)
        {
            conversationVideoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private void Update()
    {
        // Press T to start conversation,
        // but only while near police and before this interaction is completed.
        if (playerNearby &&
            !conversationPlaying &&
            !interactionCompleted &&
            Input.GetKeyDown(KeyCode.T))
        {
            StartConversation();
        }

        // Enter skips only the conversation video.
        if (conversationPlaying &&
            (Input.GetKeyDown(KeyCode.Return) ||
             Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            EndConversationAndOpenPuzzle();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;

            if (!conversationPlaying && !interactionCompleted)
            {
                talkPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;

            if (talkPrompt != null)
            {
                talkPrompt.SetActive(false);
            }
        }
    }

    private void StartConversation()
    {
        conversationPlaying = true;

        // Hide "Press T to Talk".
        if (talkPrompt != null)
        {
            talkPrompt.SetActive(false);
        }

        // Stop detective movement while video is playing.
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        // Show full-screen conversation canvas.
        if (conversationCanvas != null)
        {
            conversationCanvas.SetActive(true);
        }

        // Play conversation video from beginning.
        if (conversationVideoPlayer != null)
        {
            conversationVideoPlayer.time = 0;
            conversationVideoPlayer.Play();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        EndConversationAndOpenPuzzle();
    }

    private void EndConversationAndOpenPuzzle()
    {
        if (!conversationPlaying)
            return;

        conversationPlaying = false;
        interactionCompleted = true;

        // Stop video.
        if (conversationVideoPlayer != null)
        {
            conversationVideoPlayer.Stop();
        }

        // Hide video screen.
        if (conversationCanvas != null)
        {
            conversationCanvas.SetActive(false);
        }

        // IMPORTANT:
        // Do NOT re-enable player movement here.
        // Player must solve the puzzle first.

        // Open the circuit puzzle.
        if (puzzlePopup != null)
        {
            puzzlePopup.Open();
        }
        else
        {
            Debug.LogError("PuzzlePopup has not been assigned!");
        }
    }

    private void OnDestroy()
    {
        if (conversationVideoPlayer != null)
        {
            conversationVideoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}