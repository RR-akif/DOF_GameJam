using UnityEngine;
using System.Collections;

public class DeadBodyInvestigation : MonoBehaviour
{
    [Header("Player And Dead Body")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform deadBody;
    [SerializeField] private float investigationDistance = 3f;

    [Header("UI Prompts")]
    [SerializeField] private GameObject investigatePrompt;
    [SerializeField] private GameObject diaryPrompt;

    [Header("Diary")]
    [SerializeField] private GameObject diaryPanel;

    [SerializeField] private GameObject diaryImage1;
    [SerializeField] private GameObject diaryImage2;
    [SerializeField] private GameObject diaryImage3;
    [SerializeField] private GameObject diaryImage4;
    [SerializeField] private GameObject diaryImage5;
    [SerializeField] private GameObject diaryImage6;
    [SerializeField] private GameObject diaryImage7;

    [Header("After Body Popup")]
    [SerializeField] private GameObject afterInvestigationPopup;

    [SerializeField] private float afterPopupDelay = 2f;
    [SerializeField] private float afterPopupDuration = 5f;

    [Header("Cameras")]
    [SerializeField] private GameObject normalCamera;
    [SerializeField] private GameObject bodyCamera;
    [SerializeField] private float bodyCameraDuration = 3f;

    [Header("Player Movement")]
    [SerializeField] private Detective2DMovement playerMovementScript;


    // ======================================================
    // PUBLIC STATE
    // ======================================================

    // FiveCharactersInterrogation checks this.
    public bool HasInvestigatedBody { get; private set; }


    // ======================================================
    // INTERNAL STATE
    // ======================================================

    private bool investigationStarted = false;
    private bool investigationFinished = false;

    private bool diaryReady = false;
    private bool diaryOpen = false;

    private int currentDiaryPage = 1;


    // ======================================================
    // START
    // ======================================================

    private void Start()
    {
        HasInvestigatedBody = false;

        // Hide prompts
        investigatePrompt.SetActive(false);
        diaryPrompt.SetActive(false);

        // Hide diary
        diaryPanel.SetActive(false);

        HideAllDiaryPages();

        // Hide popup
        afterInvestigationPopup.SetActive(false);

        // Camera setup
        normalCamera.SetActive(true);
        bodyCamera.SetActive(false);
    }


    // ======================================================
    // UPDATE
    // ======================================================

    private void Update()
    {
        HandleInvestigation();

        HandleDiaryOpening();

        HandleDiaryNavigation();
    }


    // ======================================================
    // CHECK DISTANCE TO DEAD BODY
    // ======================================================

    private void HandleInvestigation()
    {
        if (investigationStarted || investigationFinished)
        {
            investigatePrompt.SetActive(false);
            return;
        }

        float distance = Vector3.Distance(
            player.position,
            deadBody.position
        );

        if (distance <= investigationDistance)
        {
            investigatePrompt.SetActive(true);

            if (Input.GetKeyDown(KeyCode.I))
            {
                StartCoroutine(InvestigateBody());
            }
        }
        else
        {
            investigatePrompt.SetActive(false);
        }
    }


    // ======================================================
    // DEAD BODY INVESTIGATION
    // ======================================================

    private IEnumerator InvestigateBody()
    {
        investigationStarted = true;

        investigatePrompt.SetActive(false);

        // Disable movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        // Switch to dead body camera
        normalCamera.SetActive(false);
        bodyCamera.SetActive(true);

        // Stay for 3 seconds
        yield return new WaitForSeconds(bodyCameraDuration);

        // Return to normal camera
        bodyCamera.SetActive(false);
        normalCamera.SetActive(true);

        // Restore movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        // Mark body as investigated
        HasInvestigatedBody = true;

        investigationStarted = false;
        investigationFinished = true;

        diaryReady = true;

        // Show:
        // "Two items are added to your diary.
        // Press D to open the diary."
        diaryPrompt.SetActive(true);
    }


    // ======================================================
    // OPEN FIRST DIARY
    // ======================================================

    private void HandleDiaryOpening()
    {
        if (!diaryReady || diaryOpen)
            return;

        if (Input.GetKeyDown(KeyCode.D))
        {
            OpenDiary();
        }
    }


    private void OpenDiary()
    {
        diaryOpen = true;

        diaryPrompt.SetActive(false);

        // Disable player movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        diaryPanel.SetActive(true);

        HideAllDiaryPages();

        // Always start from page 1
        currentDiaryPage = 1;

        ShowDiaryPage(currentDiaryPage);
    }


    // ======================================================
    // FIRST DIARY NAVIGATION
    // ======================================================

    private void HandleDiaryNavigation()
    {
        if (!diaryOpen)
            return;


        // --------------------------------------------------
        // RIGHT ARROW -> NEXT PAGE
        // --------------------------------------------------

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentDiaryPage < 2)
            {
                currentDiaryPage++;

                ShowDiaryPage(currentDiaryPage);
            }
        }


        // --------------------------------------------------
        // LEFT ARROW -> PREVIOUS PAGE
        // --------------------------------------------------

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentDiaryPage > 1)
            {
                currentDiaryPage--;

                ShowDiaryPage(currentDiaryPage);
            }
        }


        // --------------------------------------------------
        // ENTER -> CLOSE ONLY ON PAGE 2
        // --------------------------------------------------

        if (currentDiaryPage == 2 &&
            Input.GetKeyDown(KeyCode.Return))
        {
            CloseDiary();
        }
    }


    // ======================================================
    // SHOW FIRST DIARY PAGE
    // ======================================================

    private void ShowDiaryPage(int pageNumber)
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
        }
    }


    // ======================================================
    // CLOSE FIRST DIARY
    // ======================================================

    private void CloseDiary()
    {
        diaryOpen = false;

        // First diary should not open again
        diaryReady = false;

        HideAllDiaryPages();

        diaryPanel.SetActive(false);

        // Restore player movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        // Wait 2 seconds, then show AfterBodyPopup
        StartCoroutine(ShowAfterInvestigationPopup());
    }


    // ======================================================
    // AFTER BODY POPUP
    // ======================================================

    private IEnumerator ShowAfterInvestigationPopup()
    {
        // Wait two seconds after Enter
        yield return new WaitForSeconds(afterPopupDelay);

        afterInvestigationPopup.SetActive(true);

        // Keep popup visible
        yield return new WaitForSeconds(afterPopupDuration);

        afterInvestigationPopup.SetActive(false);
    }


    // ======================================================
    // HIDE ALL DIARY PAGES
    // ======================================================

    private void HideAllDiaryPages()
    {
        if (diaryImage1 != null)
            diaryImage1.SetActive(false);

        if (diaryImage2 != null)
            diaryImage2.SetActive(false);

        if (diaryImage3 != null)
            diaryImage3.SetActive(false);

        if (diaryImage4 != null)
            diaryImage4.SetActive(false);

        if (diaryImage5 != null)
            diaryImage5.SetActive(false);

        if (diaryImage6 != null)
            diaryImage6.SetActive(false);

        if (diaryImage7 != null)
            diaryImage7.SetActive(false);
    }
}