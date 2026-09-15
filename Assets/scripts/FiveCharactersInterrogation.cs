using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class FiveCharactersInterrogation : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private Transform player;

    // Use one existing character near the middle of the group.
    [SerializeField] private Transform groupCenter;

    [SerializeField] private DeadBodyInvestigation deadBodyInvestigation;

    [Header("Interaction Distance")]
    [SerializeField] private float interactionDistance = 4f;

    [Header("UI Prompts")]
    [SerializeField] private GameObject interrogatePrompt;
    [SerializeField] private GameObject investigateBodyFirstPrompt;
    [SerializeField] private GameObject statementsAddedPrompt;

    [Header("Cameras And Timeline")]
    [SerializeField] private GameObject normalCamera;
    [SerializeField] private GameObject fiveCharactersCamera;
    [SerializeField] private PlayableDirector interrogationTimeline;

    // 3 camera views x 2 seconds
    [SerializeField] private float cameraDuration = 6f;

    [Header("Player Movement")]
    [SerializeField] private Detective2DMovement playerMovementScript;

    [Header("Diary")]
    [SerializeField] private GameObject diaryPanel;

    [SerializeField] private GameObject diaryImage1;
    [SerializeField] private GameObject diaryImage2;
    [SerializeField] private GameObject diaryImage3;
    [SerializeField] private GameObject diaryImage4;
    [SerializeField] private GameObject diaryImage5;
    [SerializeField] private GameObject diaryImage6;
    [SerializeField] private GameObject diaryImage7;

    [Header("Final Decision")]
    [SerializeField] private FinalDecisionManager finalDecisionManager;

    public bool HasFinishedInterrogation { get; private set; }

    private bool interrogationStarted = false;
    private bool interrogationFinished = false;

    private bool diaryAvailable = false;
    private bool diaryOpen = false;

    private int currentPage = 1;

    private void Start()
    {
        HasFinishedInterrogation = false;

        interrogatePrompt.SetActive(false);
        investigateBodyFirstPrompt.SetActive(false);
        statementsAddedPrompt.SetActive(false);

        fiveCharactersCamera.SetActive(false);

        HideAllDiaryPages();
    }

    private void Update()
    {
        HandleApproach();
        HandleDiaryOpening();
        HandleDiaryNavigation();
    }

    private void HandleApproach()
    {
        // Once completed, absolutely nothing appears again.
        if (interrogationFinished)
        {
            interrogatePrompt.SetActive(false);
            investigateBodyFirstPrompt.SetActive(false);
            return;
        }

        if (interrogationStarted)
        {
            interrogatePrompt.SetActive(false);
            investigateBodyFirstPrompt.SetActive(false);
            return;
        }

        float distance = Vector3.Distance(
            player.position,
            groupCenter.position
        );

        if (distance <= interactionDistance)
        {
            // First stage is not complete.
            if (!deadBodyInvestigation.HasCompletedBodyStage)
            {
                interrogatePrompt.SetActive(false);
                investigateBodyFirstPrompt.SetActive(true);
                return;
            }

            investigateBodyFirstPrompt.SetActive(false);
            interrogatePrompt.SetActive(true);

            if (Input.GetKeyDown(KeyCode.I))
            {
                StartCoroutine(StartInterrogation());
            }
        }
        else
        {
            interrogatePrompt.SetActive(false);
            investigateBodyFirstPrompt.SetActive(false);
        }
    }

    private IEnumerator StartInterrogation()
    {
        interrogationStarted = true;

        interrogatePrompt.SetActive(false);
        investigateBodyFirstPrompt.SetActive(false);

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        normalCamera.SetActive(false);
        fiveCharactersCamera.SetActive(true);

        if (interrogationTimeline != null)
        {
            interrogationTimeline.Stop();
            interrogationTimeline.time = 0;
            interrogationTimeline.Evaluate();
            interrogationTimeline.Play();
        }

        // 0-2 sec = angle 1
        // 2-4 sec = angle 2
        // 4-6 sec = angle 3
        yield return new WaitForSeconds(cameraDuration);

        if (interrogationTimeline != null)
        {
            interrogationTimeline.Stop();
        }

        fiveCharactersCamera.SetActive(false);
        normalCamera.SetActive(true);

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        interrogationStarted = false;
        interrogationFinished = true;

        HasFinishedInterrogation = true;

        diaryAvailable = true;

        statementsAddedPrompt.SetActive(true);
    }

    private void HandleDiaryOpening()
    {
        if (!diaryAvailable || diaryOpen)
            return;

        if (Input.GetKeyDown(KeyCode.D))
        {
            OpenDiary();
        }
    }

    private void OpenDiary()
    {
        diaryOpen = true;
        diaryAvailable = false;

        statementsAddedPrompt.SetActive(false);

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        diaryPanel.SetActive(true);

        HideAllDiaryPages();

        currentPage = 1;

        ShowPage(currentPage);
    }

    private void HandleDiaryNavigation()
    {
        if (!diaryOpen)
            return;

        // NEXT PAGE
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentPage < 7)
            {
                currentPage++;
                ShowPage(currentPage);
            }
        }

        // PREVIOUS PAGE
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentPage > 1)
            {
                currentPage--;
                ShowPage(currentPage);
            }
        }

        // Close only when page 7 is visible.
        if (currentPage == 7 &&
            Input.GetKeyDown(KeyCode.Return))
        {
            CloseDiary();
        }
    }

    private void ShowPage(int pageNumber)
    {
        HideAllDiaryPages();

        switch (pageNumber)
        {
            case 1:
                diaryImage1.SetActive(true);
                break;

            case 2:
                diaryImage2.SetActive(true);
                break;

            case 3:
                diaryImage3.SetActive(true);
                break;

            case 4:
                diaryImage4.SetActive(true);
                break;

            case 5:
                diaryImage5.SetActive(true);
                break;

            case 6:
                diaryImage6.SetActive(true);
                break;

            case 7:
                diaryImage7.SetActive(true);
                break;
        }
    }

    private void CloseDiary()
    {
        diaryOpen = false;

        HideAllDiaryPages();

        diaryPanel.SetActive(false);

        // Player immediately becomes movable.
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        if (finalDecisionManager != null)
        {
            finalDecisionManager.StartFinalDecisionSequence();
        }
    }

    private void HideAllDiaryPages()
    {
        if (diaryImage1 != null) diaryImage1.SetActive(false);
        if (diaryImage2 != null) diaryImage2.SetActive(false);
        if (diaryImage3 != null) diaryImage3.SetActive(false);
        if (diaryImage4 != null) diaryImage4.SetActive(false);
        if (diaryImage5 != null) diaryImage5.SetActive(false);
        if (diaryImage6 != null) diaryImage6.SetActive(false);
        if (diaryImage7 != null) diaryImage7.SetActive(false);
    }
}