using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FinalDecisionManager : MonoBehaviour
{
    [Header("Final Choice UI")]
    [SerializeField] private GameObject considerationPopup;
    [SerializeField] private GameObject makeChoiceText;

    [Header("Result Images")]
    [SerializeField] private GameObject failureImage;
    [SerializeField] private GameObject successImage;

    [Header("Player Movement")]
    [SerializeField] private Detective2DMovement playerMovementScript;

    [Header("Choice Sequence")]
    [SerializeField] private float popupDelay = 3f;
    [SerializeField] private float popupDuration = 5f;

    [Header("Failure")]
    [SerializeField] private float failureImageDuration = 2f;

    // Put your actual MENU scene name in Inspector.
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Success")]
    [SerializeField] private float successImageDuration = 3f;

    public bool HasPassedFinalChoice { get; private set; }

    private bool sequenceStarted = false;
    private bool choiceEnabled = false;
    private bool decisionMade = false;

    private void Start()
    {
        HasPassedFinalChoice = false;

        considerationPopup.SetActive(false);
        makeChoiceText.SetActive(false);

        failureImage.SetActive(false);
        successImage.SetActive(false);
    }

    private void Update()
    {
        if (!choiceEnabled || decisionMade)
            return;

        // 1 = FAILURE
        if (Input.GetKeyDown(KeyCode.Alpha1) ||
            Input.GetKeyDown(KeyCode.Keypad1))
        {
            StartCoroutine(FailureSequence());
        }

        // 2 = SUCCESS
        else if (Input.GetKeyDown(KeyCode.Alpha2) ||
                 Input.GetKeyDown(KeyCode.Keypad2))
        {
            StartCoroutine(SuccessSequence());
        }
    }

    public void StartFinalDecisionSequence()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;

        StartCoroutine(FinalDecisionSequence());
    }

    private IEnumerator FinalDecisionSequence()
    {
        // Player remains free to move.

        yield return new WaitForSeconds(popupDelay);

        considerationPopup.SetActive(true);

        yield return new WaitForSeconds(popupDuration);

        considerationPopup.SetActive(false);

        // Now ask for the choice.
        makeChoiceText.SetActive(true);

        choiceEnabled = true;
    }

    private IEnumerator FailureSequence()
    {
        decisionMade = true;
        choiceEnabled = false;

        HasPassedFinalChoice = false;

        makeChoiceText.SetActive(false);

        considerationPopup.SetActive(false);
        successImage.SetActive(false);

        failureImage.SetActive(true);

        // Game is over, so stop movement.
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        yield return new WaitForSeconds(failureImageDuration);

        failureImage.SetActive(false);

        // Return to your menu.
        SceneManager.LoadScene(menuSceneName);
    }

    private IEnumerator SuccessSequence()
    {
        decisionMade = true;
        choiceEnabled = false;

        makeChoiceText.SetActive(false);

        considerationPopup.SetActive(false);
        failureImage.SetActive(false);

        successImage.SetActive(true);

        // Player remains movable.
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        yield return new WaitForSeconds(successImageDuration);

        successImage.SetActive(false);

        // NOW the entire first stage is officially successful.
        HasPassedFinalChoice = true;

        Debug.Log("Correct choice. Guard interaction unlocked.");
    }
}