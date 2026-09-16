using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject tutorialPanel;
    public GameObject aboutUsPanel;

    private bool isStartingGame = false;

    // PLAY
    public void PlayGame()
    {
        if (isStartingGame)
            return;

        StartCoroutine(StartGameAfterDelay());
    }

    IEnumerator StartGameAfterDelay()
    {
        isStartingGame = true;

        // Wait 2 seconds after pressing PLAY
        yield return new WaitForSeconds(2f);

        // Open IntroScene
        SceneManager.LoadScene("IntroScene");
    }

    // TUTORIAL
    public void OpenTutorial()
    {
        tutorialPanel.SetActive(true);
        aboutUsPanel.SetActive(false);
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
    }

    // ABOUT US
    public void OpenAboutUs()
    {
        aboutUsPanel.SetActive(true);
        tutorialPanel.SetActive(false);
    }

    public void CloseAboutUs()
    {
        aboutUsPanel.SetActive(false);
    }

    // EXIT
    public void ExitGame()
    {
        Application.Quit();
    }
}