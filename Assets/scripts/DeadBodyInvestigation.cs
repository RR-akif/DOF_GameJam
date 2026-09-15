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

    // Body camera investigation has happened.
    public bool HasInvestigatedBody { get; private set; }

    // Body diary has also been completed.
    public bool HasCompletedBodyStage { get; private set; }

    private bool investigationStarted = false;
    private bool investigationFinished = false;

    private bool diaryReady = false;
    private bool diaryOpen = false;

    private int currentDiaryPage = 1;

    private void Start()
    {
        HasInvestigatedBody = false;
        HasCompletedBodyStage = false;

        investigatePrompt.SetActive(false);
        diaryPrompt.SetActive(false);

        diaryPanel.SetActive(false);

        HideAllDiaryPages();

        afterInvestigationPopup.SetActive(false);

        normalCamera.SetActive(true);
        bodyCamera.SetActive(false);
    }

    private void Update()
    {
        HandleInvestigation();
        HandleDiaryOpening();
        HandleDiaryNavigation();
    }

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

    private IEnumerator InvestigateBody()
    {
        investigationStarted = true;

        investigatePrompt.SetActive(false);

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        normalCamera.SetActive(false);
        bodyCamera.SetActive(true);

        yield return new WaitForSeconds(bodyCameraDuration);

        bodyCamera.SetActive(false);
        normalCamera.SetActive(true);

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        HasInvestigatedBody = true;

        investigationStarted = false;
        investigationFinished = true;

        diaryReady = true;

        diaryPrompt.SetActive(true);
    }

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

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        diaryPanel.SetActive(true);

        HideAllDiaryPages();

        currentDiaryPage = 1;

        ShowDiaryPage(currentDiaryPage);
    }

    private void HandleDiaryNavigation()
    {
        if (!diaryOpen)
            return;

        // RIGHT = next page
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentDiaryPage < 2)
            {
                currentDiaryPage++;
                ShowDiaryPage(currentDiaryPage);
            }
        }

        // LEFT = previous page
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentDiaryPage > 1)
            {
                currentDiaryPage--;
                ShowDiaryPage(currentDiaryPage);
            }
        }

        // First diary closes only on page 2
        if (currentDiaryPage == 2 &&
            Input.GetKeyDown(KeyCode.Return))
        {
            CloseDiary();
        }
    }

    private void ShowDiaryPage(int pageNumber)
    {
        HideAllDiaryPages();

        if (pageNumber == 1)
        {
            diaryImage1.SetActive(true);
        }
        else if (pageNumber == 2)
        {
            diaryImage2.SetActive(true);
        }
    }

    private void CloseDiary()
    {
        diaryOpen = false;
        diaryReady = false;

        HideAllDiaryPages();

        diaryPanel.SetActive(false);

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        // Entire body-investigation stage is now complete.
        HasCompletedBodyStage = true;

        StartCoroutine(ShowAfterInvestigationPopup());
    }

    private IEnumerator ShowAfterInvestigationPopup()
    {
        yield return new WaitForSeconds(afterPopupDelay);

        afterInvestigationPopup.SetActive(true);

        yield return new WaitForSeconds(afterPopupDuration);

        afterInvestigationPopup.SetActive(false);
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