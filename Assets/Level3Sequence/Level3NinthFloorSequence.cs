using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using DegreesOfFreedom.EulerPath;

public class Level3NinthFloorSequence :
    MonoBehaviour,
    IEulerPuzzleKeepRunning
{
    // =========================================================
    // EULER PUZZLE
    // =========================================================

    [Header("Euler Puzzle")]
    [SerializeField]
    private EulerPathPuzzleController eulerPuzzle;


    // =========================================================
    // 9TH FLOOR NOTIFICATION
    // =========================================================

    [Header("9th Floor Notification")]

    [SerializeField]
    private GameObject objectiveNotification;

    [Tooltip("AudioSource used ONLY for the ninth-floor notification.")]
    [SerializeField]
    private AudioSource notificationAudio;

    [SerializeField]
    private float notificationDuration = 5f;


    // =========================================================
    // CLUE SCREEN
    // =========================================================

    [Header("Clue Screen")]

    [SerializeField]
    private GameObject clueScreen;


    // =========================================================
    // DARK SCREEN
    // =========================================================

    [Header("Dark Screen")]

    [SerializeField]
    private GameObject darkScreen;

    [SerializeField]
    private CanvasGroup darkScreenCanvasGroup;

    [SerializeField]
    private TMP_Text darkScreenText;


    // =========================================================
    // DSW CINEMATIC
    // =========================================================

    [Header("DSW Cinematic")]

    [SerializeField]
    private PlayableDirector dswDirector;

    [SerializeField]
    private GameObject dswCinematicCamera;

    [Tooltip("AudioSource used ONLY for the DSW cinematic music.")]
    [SerializeField]
    private AudioSource dswCinematicMusic;

    [Tooltip("Total duration of the DSW cinematic.")]
    [SerializeField]
    private float cinematicDuration = 9f;

    [Tooltip("Music fades out during the final part of the cinematic.")]
    [SerializeField]
    private float cinematicAudioFadeOutDuration = 2f;

    [Tooltip(
        "Gap after the cinematic before the final dark-screen message."
    )]
    [SerializeField]
    private float postCinematicDelay = 2f;


    // =========================================================
    // PLAYER CONTROL
    // =========================================================

    [Header("Player Control")]

    [Tooltip("Drag Detective3DMovement here.")]
    [SerializeField]
    private MonoBehaviour playerMovement;


    // =========================================================
    // FADE SETTINGS
    // =========================================================

    [Header("Fade Settings")]

    [SerializeField]
    private float fadeDuration = 0.5f;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private bool reachedNinthFloor = false;
    private bool notificationPlayed = false;
    private bool eulerSolved = false;

    private bool waitingForClueEnter = false;
    private bool waitingForFinalEnter = false;

    private bool postPuzzleSequenceRunning = false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (objectiveNotification != null)
            objectiveNotification.SetActive(false);

        if (clueScreen != null)
            clueScreen.SetActive(false);

        if (darkScreen != null)
            darkScreen.SetActive(false);

        if (darkScreenCanvasGroup != null)
            darkScreenCanvasGroup.alpha = 0f;

        if (dswCinematicCamera != null)
            dswCinematicCamera.SetActive(false);

        if (notificationAudio != null)
            notificationAudio.Stop();

        if (dswCinematicMusic != null)
            dswCinematicMusic.Stop();
    }


    // =========================================================
    // ENABLE / DISABLE
    // =========================================================

    private void OnEnable()
    {
        if (eulerPuzzle != null)
        {
            eulerPuzzle.OnPuzzleSolved +=
                HandleEulerPuzzleSolved;
        }
    }

    private void OnDisable()
    {
        if (eulerPuzzle != null)
        {
            eulerPuzzle.OnPuzzleSolved -=
                HandleEulerPuzzleSolved;
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Euler currently owns the keyboard/mouse.
        if (EulerPathPuzzleController.IsMainGameInputLocked)
            return;


        // -----------------------------------------------------
        // ENTER ON CLUE IMAGE
        // -----------------------------------------------------

        if (waitingForClueEnter && EnterPressed())
        {
            waitingForClueEnter = false;

            if (!postPuzzleSequenceRunning)
            {
                StartCoroutine(
                    StartPostPuzzleSequence()
                );
            }

            return;
        }


        // -----------------------------------------------------
        // ENTER ON FINAL DARK-SCREEN MESSAGE
        // -----------------------------------------------------

        if (waitingForFinalEnter && EnterPressed())
        {
            waitingForFinalEnter = false;

            StartCoroutine(
                FinishEntireSequence()
            );

            return;
        }
    }


    // =========================================================
    // NINTH FLOOR REACHED
    // =========================================================

    public void NinthFloorReached()
    {
        if (reachedNinthFloor)
            return;

        reachedNinthFloor = true;

        StartCoroutine(
            ShowObjectiveNotification()
        );
    }


    // =========================================================
    // OBJECTIVE NOTIFICATION
    // =========================================================

    private IEnumerator ShowObjectiveNotification()
    {
        if (objectiveNotification != null)
        {
            objectiveNotification.SetActive(true);
        }


        // Play notification sound only once.
        if (!notificationPlayed)
        {
            notificationPlayed = true;

            if (notificationAudio != null)
            {
                notificationAudio.Stop();
                notificationAudio.Play();
            }
        }


        yield return new WaitForSecondsRealtime(
            notificationDuration
        );


        if (objectiveNotification != null)
        {
            objectiveNotification.SetActive(false);
        }
    }


    // =========================================================
    // TRY TO OPEN EULER PUZZLE
    // =========================================================

    public void TryOpenEulerPuzzle()
    {
        if (!reachedNinthFloor)
        {
            Debug.LogWarning(
                "Euler puzzle blocked: ninth floor has not been reached."
            );

            return;
        }


        if (eulerSolved)
            return;


        if (postPuzzleSequenceRunning)
            return;


        if (eulerPuzzle == null)
        {
            Debug.LogError(
                "Level3NinthFloorSequence: Euler puzzle is not assigned."
            );

            return;
        }


        if (!eulerPuzzle.IsOpen)
        {
            eulerPuzzle.StartPuzzle();
        }
    }


    // =========================================================
    // EULER PUZZLE SOLVED
    // =========================================================

    private void HandleEulerPuzzleSolved()
    {
        if (eulerSolved)
            return;


        eulerSolved = true;


        // Stop player movement.
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }


        // Show clue image.
        if (clueScreen != null)
        {
            clueScreen.SetActive(true);
        }


        // Wait for Enter.
        waitingForClueEnter = true;
    }


    // =========================================================
    // POST-EULER SEQUENCE
    // =========================================================

    private IEnumerator StartPostPuzzleSequence()
    {
        postPuzzleSequenceRunning = true;


        // Hide clue.
        if (clueScreen != null)
        {
            clueScreen.SetActive(false);
        }


        // Wait until Euler has fully released its input lock.
        while (
            EulerPathPuzzleController
                .IsMainGameInputLocked
        )
        {
            yield return null;
        }


        // Keep Detective frozen.
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }


        // =====================================================
        // PART 1
        // "APPEARANCE OF DSW SIR"
        // =====================================================

        yield return FadeToBlack(
            "Appearance of DSW sir"
        );


        // Keep message visible for 3 seconds.
        yield return new WaitForSecondsRealtime(
            3f
        );


        // Reveal scene again.
        yield return FadeFromBlack();


        // =====================================================
        // PART 2
        // DSW CINEMATIC
        // =====================================================

        // Turn cinematic camera ON.
        if (dswCinematicCamera != null)
        {
            dswCinematicCamera.SetActive(true);
        }


        // Start cinematic music.
        if (dswCinematicMusic != null)
        {
            dswCinematicMusic.Stop();
            dswCinematicMusic.Play();
        }


        // Start Timeline.
        if (dswDirector != null)
        {
            dswDirector.time = 0;
            dswDirector.Play();
        }


        // =====================================================
        // PLAY NORMAL-VOLUME PORTION
        // =====================================================

        float safeFadeDuration =
            Mathf.Clamp(
                cinematicAudioFadeOutDuration,
                0f,
                cinematicDuration
            );


        float normalMusicDuration =
            Mathf.Max(
                0f,
                cinematicDuration - safeFadeDuration
            );


        // Example:
        //
        // Cinematic = 9 sec
        // Fade = 2 sec
        //
        // Normal music = first 7 sec
        // Fade music   = final 2 sec

        if (normalMusicDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                normalMusicDuration
            );
        }


        // =====================================================
        // FADE MUSIC DURING FINAL PART OF CINEMATIC
        // =====================================================

        if (safeFadeDuration > 0f)
        {
            if (dswCinematicMusic != null)
            {
                yield return StartCoroutine(
                    FadeOutAudio(
                        dswCinematicMusic,
                        safeFadeDuration
                    )
                );
            }
            else
            {
                yield return new WaitForSecondsRealtime(
                    safeFadeDuration
                );
            }
        }
        else
        {
            if (dswCinematicMusic != null)
            {
                dswCinematicMusic.Stop();
            }
        }


        // =====================================================
        // END DSW CINEMATIC
        // =====================================================

        if (dswDirector != null)
        {
            dswDirector.Stop();
        }


        // Make absolutely sure audio has stopped.
        if (dswCinematicMusic != null &&
            dswCinematicMusic.isPlaying)
        {
            dswCinematicMusic.Stop();
        }


        // Disable cinematic camera.
        // Your Level3 follow camera becomes Live again.
        if (dswCinematicCamera != null)
        {
            dswCinematicCamera.SetActive(false);
        }


        // =====================================================
        // 2-SECOND GAP
        // =====================================================

        // During this gap:
        //
        // Level3FollowCamera = visible
        // DSW camera         = OFF
        // Dark screen        = OFF
        // Detective movement = disabled
        //
        // Nothing needs to be pressed.

        yield return new WaitForSecondsRealtime(
            postCinematicDelay
        );


        // =====================================================
        // PART 3
        // FINAL DARK-SCREEN MESSAGE
        // =====================================================

        yield return FadeToBlack(
            "Found a video clip from DSW.\n\n" +
            "Message - There was no one on the 9th floor " +
            "at that night.\n\n" +
            "Press Enter to continue"
        );


        // Stay here until player presses Enter.
        waitingForFinalEnter = true;
    }


    // =========================================================
    // FADE OUT CINEMATIC AUDIO
    // =========================================================

    private IEnumerator FadeOutAudio(
        AudioSource audioSource,
        float duration)
    {
        if (audioSource == null)
            yield break;


        if (!audioSource.isPlaying)
            yield break;


        // Remember whatever volume you configured
        // in the Inspector.
        float originalVolume =
            audioSource.volume;


        // If duration is zero, stop immediately.
        if (duration <= 0f)
        {
            audioSource.Stop();

            audioSource.volume =
                originalVolume;

            yield break;
        }


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );


            // Smoothly go:
            //
            // original volume
            //       ↓
            //       ↓
            //       ↓
            //       0

            audioSource.volume =
                Mathf.Lerp(
                    originalVolume,
                    0f,
                    progress
                );


            yield return null;
        }


        // Ensure complete silence.
        audioSource.volume = 0f;


        // Now stop the AudioSource.
        audioSource.Stop();


        // IMPORTANT:
        // Restore its Inspector volume so if this
        // AudioSource is ever played again it won't
        // start silently.
        audioSource.volume =
            originalVolume;
    }


    // =========================================================
    // FINISH ENTIRE SEQUENCE
    // =========================================================

    private IEnumerator FinishEntireSequence()
    {
        // Remove final dark screen.
        yield return FadeFromBlack();


        // Give Detective movement back.
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }


        postPuzzleSequenceRunning = false;
    }


    // =========================================================
    // FADE TO BLACK
    // =========================================================

    private IEnumerator FadeToBlack(
        string message)
    {
        if (
            darkScreen == null ||
            darkScreenCanvasGroup == null
        )
        {
            yield break;
        }


        // Turn the dark screen on.
        darkScreen.SetActive(true);


        // Put requested text on it.
        if (darkScreenText != null)
        {
            darkScreenText.text = message;
        }


        // Start transparent.
        darkScreenCanvasGroup.alpha = 0f;


        float elapsed = 0f;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;


            darkScreenCanvasGroup.alpha =
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );


            yield return null;
        }


        // Ensure completely black.
        darkScreenCanvasGroup.alpha = 1f;
    }


    // =========================================================
    // FADE FROM BLACK
    // =========================================================

    private IEnumerator FadeFromBlack()
    {
        if (
            darkScreen == null ||
            darkScreenCanvasGroup == null
        )
        {
            yield break;
        }


        float elapsed = 0f;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;


            darkScreenCanvasGroup.alpha =
                1f -
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );


            yield return null;
        }


        darkScreenCanvasGroup.alpha = 0f;


        // Disable whole dark-screen object.
        darkScreen.SetActive(false);
    }


    // =========================================================
    // ENTER KEY CHECK
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
            Input.GetKeyDown(KeyCode.Return)
            ||
            Input.GetKeyDown(KeyCode.KeypadEnter);

#endif
    }
}