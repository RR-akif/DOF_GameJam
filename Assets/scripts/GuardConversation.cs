using UnityEngine;
using UnityEngine.Video;

public class GuardConversation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform guard;

    [Header("Progress Requirements")]
    [SerializeField] private DeadBodyInvestigation deadBodyInvestigation;

    [SerializeField]
    private FiveCharactersInterrogation fiveCharactersInterrogation;

    [SerializeField]
    private FinalDecisionManager finalDecisionManager;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;

    [Header("UI")]
    [SerializeField] private GameObject talkPrompt;
    [SerializeField] private GameObject videoUI;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Player Movement")]
    [SerializeField] private Detective2DMovement playerMovementScript;

    private bool conversationStarted = false;
    private bool conversationFinished = false;

    private void Start()
    {
        talkPrompt.SetActive(false);
        videoUI.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;

            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private void Update()
    {
        HandleTalkPrompt();

        // ENTER skips/ends the conversation video.
        if (conversationStarted &&
            Input.GetKeyDown(KeyCode.Return))
        {
            EndConversation();
        }
    }

    private void HandleTalkPrompt()
    {
        if (conversationStarted || conversationFinished)
        {
            talkPrompt.SetActive(false);
            return;
        }

        // Nothing appears until all previous stages
        // are successfully completed.
        if (!PreviousStagesCompleted())
        {
            talkPrompt.SetActive(false);
            return;
        }

        float distance = Vector3.Distance(
            player.position,
            guard.position
        );

        if (distance <= interactionDistance)
        {
            talkPrompt.SetActive(true);

            if (Input.GetKeyDown(KeyCode.T))
            {
                StartConversation();
            }
        }
        else
        {
            talkPrompt.SetActive(false);
        }
    }

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

    private void StartConversation()
    {
        conversationStarted = true;

        talkPrompt.SetActive(false);

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        videoUI.SetActive(true);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        EndConversation();
    }

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

        videoUI.SetActive(false);

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}