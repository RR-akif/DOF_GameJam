using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Level3FinalEndingController : MonoBehaviour
{
    // =========================================================
    // ENDING UI
    // =========================================================

    [Header("Ending UI")]

    [SerializeField]
    private GameObject mastermindQuestionPanel;

    [SerializeField]
    private GameObject successPanel;

    [SerializeField]
    private GameObject failurePanel;


    // =========================================================
    // FINAL VIDEO
    // =========================================================

    [Header("Final Video")]

    [SerializeField]
    private GameObject videoScreen;

    [SerializeField]
    private VideoPlayer videoPlayer;

    [SerializeField]
    private VideoClip finalConversationClip;

    [Tooltip("AudioSource used for the embedded audio of the final video.")]
    [SerializeField]
    private AudioSource videoAudioSource;

    [Tooltip("If checked, Enter can skip the final video.")]
    [SerializeField]
    private bool allowEnterToSkipVideo = true;


    // =========================================================
    // SUCCESS / FAILURE AUDIO
    // =========================================================

    [Header("Ending Audio")]

    [Tooltip("The SuccessMenuMusic GameObject.")]
    [SerializeField]
    private GameObject successMenuMusicObject;

    [Tooltip("PersistentSuccessMusic component on SuccessMenuMusic.")]
    [SerializeField]
    private PersistentSuccessMusic successMenuMusic;

    [Tooltip("Failure sound used only for the final wrong answer.")]
    [SerializeField]
    private AudioSource finalFailureAudio;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]

    [Tooltip("Drag Detective3DMovement here.")]
    [SerializeField]
    private MonoBehaviour playerMovement;


    // =========================================================
    // TIMING
    // =========================================================

    [Header("Timing")]

    [SerializeField]
    private float successImageDuration = 4f;

    [SerializeField]
    private float failureImageDuration = 4f;


    // =========================================================
    // MAIN MENU
    // =========================================================

    [Header("Scene")]

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private bool endingStarted = false;
    private bool waitingForChoice = false;
    private bool videoFinished = false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (mastermindQuestionPanel != null)
        {
            mastermindQuestionPanel.SetActive(false);
        }

        if (successPanel != null)
        {
            successPanel.SetActive(false);
        }

        if (failurePanel != null)
        {
            failurePanel.SetActive(false);
        }

        if (videoScreen != null)
        {
            videoScreen.SetActive(false);
        }

        if (finalFailureAudio != null)
        {
            finalFailureAudio.Stop();
            finalFailureAudio.loop = false;
        }

        if (videoAudioSource != null)
        {
            videoAudioSource.Stop();
            videoAudioSource.mute = false;
            videoAudioSource.loop = false;
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!waitingForChoice)
            return;


        // ENTER = CORRECT ANSWER
        if (EnterPressed())
        {
            waitingForChoice = false;

            StartCoroutine(
                CorrectEnding()
            );

            return;
        }


        // SPACE = WRONG ANSWER
        if (SpacePressed())
        {
            waitingForChoice = false;

            StartCoroutine(
                WrongEnding()
            );

            return;
        }
    }


    // =========================================================
    // BEGIN ENDING
    // =========================================================

    public void BeginEnding()
    {
        if (endingStarted)
            return;


        endingStarted = true;


        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        if (mastermindQuestionPanel != null)
        {
            mastermindQuestionPanel.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "Final Ending: Mastermind Question Panel is not assigned."
            );
        }


        // Prevent the same Enter used on the Rafi clue
        // from immediately selecting the correct ending.
        waitingForChoice = false;

        StartCoroutine(
            EnableChoiceInputAfterEnterReleased()
        );
    }


    // =========================================================
    // WAIT BEFORE ACCEPTING FINAL CHOICE
    // =========================================================

    private IEnumerator EnableChoiceInputAfterEnterReleased()
    {
        yield return null;


        while (EnterHeld())
        {
            yield return null;
        }


        yield return null;


        waitingForChoice = true;
    }


    // =========================================================
    // CORRECT ENDING
    // =========================================================

    private IEnumerator CorrectEnding()
    {
        if (mastermindQuestionPanel != null)
        {
            mastermindQuestionPanel.SetActive(false);
        }


        // Wait for Enter release
        yield return null;


        while (EnterHeld())
        {
            yield return null;
        }


        yield return null;


        // =====================================================
        // 1. PLAY FINAL VIDEO
        // =====================================================

        yield return StartCoroutine(
            PlayFinalVideo()
        );


        // =====================================================
        // 2. START SUCCESS / MENU MUSIC
        // =====================================================

        if (successMenuMusicObject != null)
        {
            successMenuMusicObject.SetActive(true);
        }


        if (successMenuMusic != null)
        {
            successMenuMusic.StartMusic();
        }


        // =====================================================
        // 3. SHOW SUCCESS IMAGE IMMEDIATELY
        // =====================================================

        if (successPanel != null)
        {
            successPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "Final Ending: Success Panel is not assigned."
            );
        }


        // No camera shot.
        // No gameplay camera return.
        // Just keep success image visible.

        yield return new WaitForSecondsRealtime(
            successImageDuration
        );


        // =====================================================
        // 4. LOAD MAIN MENU
        // =====================================================

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }


    // =========================================================
    // WRONG ENDING
    // =========================================================

    private IEnumerator WrongEnding()
    {
        if (mastermindQuestionPanel != null)
        {
            mastermindQuestionPanel.SetActive(false);
        }


        if (failurePanel != null)
        {
            failurePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "Final Ending: Failure Panel is not assigned."
            );
        }


        if (finalFailureAudio != null)
        {
            finalFailureAudio.Stop();

            finalFailureAudio.loop = false;

            finalFailureAudio.time = 0f;

            finalFailureAudio.Play();
        }
        else
        {
            Debug.LogWarning(
                "Final Ending: Final Failure Audio is not assigned."
            );
        }


        yield return new WaitForSecondsRealtime(
            failureImageDuration
        );


        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }


    // =========================================================
    // FINAL VIDEO
    // =========================================================

    private IEnumerator PlayFinalVideo()
    {
        if (videoPlayer == null)
        {
            Debug.LogError(
                "Final Ending: VideoPlayer is not assigned."
            );

            yield break;
        }


        if (finalConversationClip == null)
        {
            Debug.LogError(
                "Final Ending: Final Conversation Clip is not assigned."
            );

            yield break;
        }


        if (videoScreen == null)
        {
            Debug.LogError(
                "Final Ending: Video Screen is not assigned."
            );

            yield break;
        }


        // Wait for Enter release
        yield return null;


        while (EnterHeld())
        {
            yield return null;
        }


        yield return null;


        // -----------------------------------------------------
        // SHOW VIDEO SCREEN
        // -----------------------------------------------------

        videoScreen.SetActive(true);


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        // -----------------------------------------------------
        // PREPARE VIDEO
        // -----------------------------------------------------

        videoFinished = false;


        videoPlayer.Stop();


        videoPlayer.clip =
            finalConversationClip;


        // -----------------------------------------------------
        // AUDIO ROUTING
        // -----------------------------------------------------

        videoPlayer.audioOutputMode =
            VideoAudioOutputMode.AudioSource;


        if (videoAudioSource != null)
        {
            videoAudioSource.Stop();

            videoAudioSource.mute =
                false;

            videoAudioSource.volume =
                1f;

            videoAudioSource.loop =
                false;


            videoPlayer.EnableAudioTrack(
                0,
                true
            );


            videoPlayer.SetTargetAudioSource(
                0,
                videoAudioSource
            );
        }


        // -----------------------------------------------------
        // COMPLETION EVENT
        // -----------------------------------------------------

        videoPlayer.loopPointReached -=
            HandleVideoFinished;

        videoPlayer.loopPointReached +=
            HandleVideoFinished;


        // -----------------------------------------------------
        // PREPARE
        // -----------------------------------------------------

        videoPlayer.Prepare();


        float prepareTimer = 0f;

        const float prepareTimeout = 10f;


        while (!videoPlayer.isPrepared)
        {
            prepareTimer +=
                Time.unscaledDeltaTime;


            if (prepareTimer >= prepareTimeout)
            {
                Debug.LogError(
                    "Final Ending: Video preparation timed out."
                );


                videoPlayer.loopPointReached -=
                    HandleVideoFinished;


                videoScreen.SetActive(false);


                yield break;
            }


            yield return null;
        }


        // -----------------------------------------------------
        // PLAY
        // -----------------------------------------------------

        videoPlayer.Play();


        // -----------------------------------------------------
        // WAIT UNTIL VIDEO FINISHES
        // -----------------------------------------------------

        while (!videoFinished)
        {
            if (
                allowEnterToSkipVideo &&
                EnterPressed()
            )
            {
                videoFinished = true;
            }


            yield return null;
        }


        // -----------------------------------------------------
        // CLEANUP
        // -----------------------------------------------------

        videoPlayer.loopPointReached -=
            HandleVideoFinished;


        videoPlayer.Stop();


        if (videoAudioSource != null)
        {
            videoAudioSource.Stop();
        }


        videoScreen.SetActive(false);


        while (EnterHeld())
        {
            yield return null;
        }


        yield return null;
    }


    // =========================================================
    // VIDEO COMPLETION CALLBACK
    // =========================================================

    private void HandleVideoFinished(
        VideoPlayer source)
    {
        videoFinished =
            true;
    }


    // =========================================================
    // ENTER PRESSED
    // =========================================================

    private bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM

        return
            Keyboard.current != null &&
            (
                Keyboard.current.enterKey
                    .wasPressedThisFrame
                ||
                Keyboard.current.numpadEnterKey
                    .wasPressedThisFrame
            );

#else

        return
            Input.GetKeyDown(
                KeyCode.Return
            )
            ||
            Input.GetKeyDown(
                KeyCode.KeypadEnter
            );

#endif
    }


    // =========================================================
    // ENTER HELD
    // =========================================================

    private bool EnterHeld()
    {
#if ENABLE_INPUT_SYSTEM

        return
            Keyboard.current != null &&
            (
                Keyboard.current.enterKey
                    .isPressed
                ||
                Keyboard.current.numpadEnterKey
                    .isPressed
            );

#else

        return
            Input.GetKey(
                KeyCode.Return
            )
            ||
            Input.GetKey(
                KeyCode.KeypadEnter
            );

#endif
    }


    // =========================================================
    // SPACE PRESSED
    // =========================================================

    private bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM

        return
            Keyboard.current != null &&
            Keyboard.current.spaceKey
                .wasPressedThisFrame;

#else

        return
            Input.GetKeyDown(
                KeyCode.Space
            );

#endif
    }
}