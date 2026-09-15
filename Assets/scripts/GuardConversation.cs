using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class GuardConversation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform guard;

    [Header("Progress Requirements")]
    [SerializeField] private DeadBodyInvestigation deadBodyInvestigation;
    [SerializeField] private FiveCharactersInterrogation fiveCharactersInterrogation;
    [SerializeField] private FinalDecisionManager finalDecisionManager;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;

    [Header("UI")]
    [SerializeField] private GameObject talkPrompt;
    [SerializeField] private GameObject videoUI;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Player Movement")]
    [SerializeField] private Detective2DMovement playerMovementScript;

    [Header("Maze Puzzle")]
    [SerializeField] private MazePuzzleController mazePuzzle;

    [Header("Level 2 Completion")]
    [SerializeField] private GameObject level2CompleteImage;
    [SerializeField] private AudioSource level2CompleteMusic;
    [SerializeField] private float level2CompleteDuration = 3f;

    [Header("Next Scene")]
    [SerializeField] private string level3SceneName = "Level3";

    private bool conversationStarted = false;
    private bool conversationFinished = false;

    private bool mazeStarted = false;
    private bool mazeSolved = false;

    private bool levelCompletionStarted = false;


    private void Start()
    {
        if (talkPrompt != null)
        {
            talkPrompt.SetActive(false);
        }

        if (videoUI != null)
        {
            videoUI.SetActive(false);
        }

        if (level2CompleteImage != null)
        {
            level2CompleteImage.SetActive(false);
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        // Subscribe to maze success
        if (mazePuzzle != null)
        {
            mazePuzzle.OnPuzzleSuccess += OnMazeSolved;
        }
    }


    private void Update()
    {
        HandleTalkPrompt();

        // Press Enter to skip the guard conversation video.
        if (conversationStarted &&
            Input.GetKeyDown(KeyCode.Return))
        {
            EndConversation();
        }
    }


    // ======================================================
    // GUARD INTERACTION
    // ======================================================

    private void HandleTalkPrompt()
    {
        if (conversationStarted || conversationFinished)
        {
            if (talkPrompt != null)
            {
                talkPrompt.SetActive(false);
            }

            return;
        }

        // Guard remains locked until all previous stages
        // have been completed successfully.
        if (!PreviousStagesCompleted())
        {
            if (talkPrompt != null)
            {
                talkPrompt.SetActive(false);
            }

            return;
        }

        if (player == null || guard == null)
            return;

        float distance = Vector3.Distance(
            player.position,
            guard.position
        );

        if (distance <= interactionDistance)
        {
            if (talkPrompt != null)
            {
                talkPrompt.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                StartConversation();
            }
        }
        else
        {
            if (talkPrompt != null)
            {
                talkPrompt.SetActive(false);
            }
        }
    }


    // ======================================================
    // CHECK PREVIOUS LEVEL-2 PROGRESS
    // ======================================================

    private bool PreviousStagesCompleted()
    {
        if (deadBodyInvestigation == null ||
            fiveCharactersInterrogation == null ||
            finalDecisionManager == null)
        {
            return false;
        }

        return
            deadBodyInvestigation.HasCompletedBodyStage &&
            fiveCharactersInterrogation.HasFinishedInterrogation &&
            finalDecisionManager.HasPassedFinalChoice;
    }


    // ======================================================
    // START GUARD VIDEO
    // ======================================================

    private void StartConversation()
    {
        if (conversationStarted || conversationFinished)
            return;

        conversationStarted = true;

        if (talkPrompt != null)
        {
            talkPrompt.SetActive(false);
        }

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        if (videoUI != null)
        {
            videoUI.SetActive(true);
        }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
    }


    // ======================================================
    // VIDEO FINISHED NATURALLY
    // ======================================================

    private void OnVideoFinished(VideoPlayer vp)
    {
        EndConversation();
    }


    // ======================================================
    // END VIDEO -> START MAZE
    // ======================================================

    private void EndConversation()
    {
        if (!conversationStarted)
            return;

        conversationStarted = false;
        conversationFinished = true;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (videoUI != null)
        {
            videoUI.SetActive(false);
        }

        StartMazePuzzle();
    }


    // ======================================================
    // START MAZE
    // ======================================================

    // private void StartMazePuzzle()
    // {
    //     if (mazeStarted)
    //         return;

    //     mazeStarted = true;

    //     // Keep detective movement disabled while maze is open.
    //     if (playerMovementScript != null)
    //     {
    //         playerMovementScript.enabled = false;
    //     }

    //     if (mazePuzzle != null)
    //     {
    //         mazePuzzle.StartPuzzle();
    //     }
    //     else
    //     {
    //         Debug.LogError(
    //             "Maze Puzzle Controller is not assigned in GuardConversation."
    //         );
    //     }
    // }
    private void StartMazePuzzle()
    {
        if (mazeStarted)
            return;

        mazeStarted = true;

        Debug.Log("1. StartMazePuzzle() called");

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        if (mazePuzzle != null)
        {
            Debug.Log("2. MazePuzzle reference found");

            mazePuzzle.StartPuzzle();

            Debug.Log("3. StartPuzzle() was called");
        }
        else
        {
            Debug.LogError("MazePuzzle reference is NULL");
        }
    }


    // ======================================================
    // MAZE SUCCESS
    // ======================================================

    private void OnMazeSolved()
    {
        if (mazeSolved || levelCompletionStarted)
            return;

        mazeSolved = true;

        StartCoroutine(Level2CompleteSequence());
    }


    // ======================================================
    // LEVEL 2 COMPLETE
    // ======================================================

    private IEnumerator Level2CompleteSequence()
    {
        levelCompletionStarted = true;

        // Keep detective disabled during completion screen.
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        // Show completion image.
        if (level2CompleteImage != null)
        {
            level2CompleteImage.SetActive(true);
        }

        // Play completion music.
        if (level2CompleteMusic != null)
        {
            level2CompleteMusic.Stop();
            level2CompleteMusic.Play();
        }

        // Keep image/music for 3 seconds.
        yield return new WaitForSeconds(level2CompleteDuration);

        if (level2CompleteMusic != null)
        {
            level2CompleteMusic.Stop();
        }

        if (!string.IsNullOrEmpty(level3SceneName))
        {
            SceneManager.LoadScene(level3SceneName);
        }
        else
        {
            Debug.LogError(
                "Level 3 Scene Name is empty in GuardConversation."
            );
        }
    }


    // ======================================================
    // CLEANUP
    // ======================================================

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        if (mazePuzzle != null)
        {
            mazePuzzle.OnPuzzleSuccess -= OnMazeSolved;
        }
    }
}