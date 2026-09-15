using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class FiveCharactersInterrogation : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private Transform player;

    // Use one of the five characters near the center
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

    // 3 shots × 2 seconds
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


    // ======================================================
    // INTERNAL STATE
    // ======================================================

    private bool interrogationStarted = false;

    private bool interrogationFinished = false;

    private bool diaryAvailable = false;

    private bool diaryOpen = false;

    private int currentPage = 1;


    // ======================================================
    // START
    // ======================================================

    private void Start()
    {
        interrogatePrompt.SetActive(false);

        investigateBodyFirstPrompt.SetActive(false);

        statementsAddedPrompt.SetActive(false);

        fiveCharactersCamera.SetActive(false);

        HideAllDiaryPages();
    }


    // ======================================================
    // UPDATE
    // ======================================================

    private void Update()
    {
        HandleApproach();

        HandleDiaryOpening();

        HandleDiaryNavigation();
    }


    // ======================================================
    // CHECK DISTANCE TO FIVE CHARACTERS
    // ======================================================

    private void HandleApproach()
    {
        // --------------------------------------------------
        // INTERROGATION ALREADY COMPLETED
        // --------------------------------------------------

        // Nothing should ever appear again.
        if (interrogationFinished)
        {
            interrogatePrompt.SetActive(false);

            investigateBodyFirstPrompt.SetActive(false);

            return;
        }


        // --------------------------------------------------
        // CINEMATIC CURRENTLY RUNNING
        // --------------------------------------------------

        if (interrogationStarted)
        {
            interrogatePrompt.SetActive(false);

            investigateBodyFirstPrompt.SetActive(false);

            return;
        }


        // --------------------------------------------------
        // CALCULATE DISTANCE
        // --------------------------------------------------

        float distance = Vector3.Distance(
            player.position,
            groupCenter.position
        );


        // --------------------------------------------------
        // PLAYER IS CLOSE
        // --------------------------------------------------

        if (distance <= interactionDistance)
        {
            // Body NOT investigated
            if (!deadBodyInvestigation.HasInvestigatedBody)
            {
                interrogatePrompt.SetActive(false);

                investigateBodyFirstPrompt.SetActive(true);

                return;
            }


            // Body HAS been investigated
            investigateBodyFirstPrompt.SetActive(false);

            interrogatePrompt.SetActive(true);


            // Press I
            if (Input.GetKeyDown(KeyCode.I))
            {
                StartCoroutine(StartInterrogation());
            }
        }


        // --------------------------------------------------
        // PLAYER IS FAR AWAY
        // --------------------------------------------------

        else
        {
            interrogatePrompt.SetActive(false);

            investigateBodyFirstPrompt.SetActive(false);
        }
    }


    // ======================================================
    // INTERROGATION CINEMATIC
    // ======================================================

    private IEnumerator StartInterrogation()
    {
        interrogationStarted = true;

        interrogatePrompt.SetActive(false);

        investigateBodyFirstPrompt.SetActive(false);


        // Disable movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }


        // Turn gameplay camera off
        normalCamera.SetActive(false);


        // Turn interrogation camera on
        fiveCharactersCamera.SetActive(true);


        // Restart Timeline
        if (interrogationTimeline != null)
        {
            interrogationTimeline.Stop();

            interrogationTimeline.time = 0;

            interrogationTimeline.Evaluate();

            interrogationTimeline.Play();
        }


        // Timeline:
        //
        // 0 - 2 sec = Angle 1
        // 2 - 4 sec = Angle 2
        // 4 - 6 sec = Angle 3

        yield return new WaitForSeconds(cameraDuration);


        // Stop Timeline
        if (interrogationTimeline != null)
        {
            interrogationTimeline.Stop();
        }


        // Disable interrogation camera
        fiveCharactersCamera.SetActive(false);


        // Restore normal camera
        normalCamera.SetActive(true);


        // Restore movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }


        // IMPORTANT:
        // This interaction can NEVER happen again.
        interrogationStarted = false;

        interrogationFinished = true;


        // Allow new diary
        diaryAvailable = true;


        // Show:
        //
        // "Five statements are added to your diary.
        // Press D to open the diary."
        statementsAddedPrompt.SetActive(true);
    }


    // ======================================================
    // OPEN DIARY
    // ======================================================

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


        // Disable movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }


        diaryPanel.SetActive(true);


        HideAllDiaryPages();


        // Always start at Page 1
        currentPage = 1;


        ShowPage(currentPage);
    }


    // ======================================================
    // SEVEN-PAGE DIARY NAVIGATION
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
            if (currentPage < 7)
            {
                currentPage++;

                ShowPage(currentPage);
            }
        }


        // --------------------------------------------------
        // LEFT ARROW -> PREVIOUS PAGE
        // --------------------------------------------------

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentPage > 1)
            {
                currentPage--;

                ShowPage(currentPage);
            }
        }


        // --------------------------------------------------
        // ENTER -> CLOSE ONLY ON PAGE 7
        // --------------------------------------------------

        if (currentPage == 7 &&
            Input.GetKeyDown(KeyCode.Return))
        {
            CloseDiary();
        }
    }


    // ======================================================
    // SHOW PAGE
    // ======================================================

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


    // ======================================================
    // HIDE ALL PAGES
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


    // ======================================================
    // CLOSE DIARY
    // ======================================================

    private void CloseDiary()
    {
        diaryOpen = false;


        HideAllDiaryPages();


        diaryPanel.SetActive(false);


        // Restore movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }
    }
}