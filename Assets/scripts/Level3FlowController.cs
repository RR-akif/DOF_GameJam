using System.Collections;
using UnityEngine;
using RollTheBall;

public class Level3FlowController : MonoBehaviour
{
    public static Level3FlowController Instance { get; private set; }

    private enum Level3State
    {
        Intro,
        Exploring,
        Puzzle,
        FadingToClue,
        Clue,
        Transporting,
        NinthFloor
    }


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private Detective3DMovement playerMovement;


    // =========================================================
    // MAGIC CUBE
    // =========================================================

    [Header("Magic Cube")]
    [SerializeField] private MagicCubeAutoPuzzleTrigger magicCubeTrigger;


    // =========================================================
    // ROLL THE BALL
    // =========================================================

    [Header("Roll The Ball Puzzle")]
    [SerializeField] private RollTheBallPuzzleController rollTheBallPuzzle;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject cluePanel;
    [SerializeField] private CanvasGroup fadeCanvasGroup;


    // =========================================================
    // NINTH FLOOR
    // =========================================================

    [Header("Ninth Floor")]
    [SerializeField] private Transform ninthFloorSpawn;

    [Tooltip(
        "Controls the ninth-floor objective notification, " +
        "Euler puzzle, clue and DSW sequence."
    )]
    [SerializeField]
    private Level3NinthFloorSequence ninthFloorSequence;


    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Audio")]
    [SerializeField] private AudioSource level3IntroAudio;
    [SerializeField] private AudioSource transportationAudio;


    // =========================================================
    // TIMING
    // =========================================================

    [Header("Fade / Transportation Timing")]

    [SerializeField]
    private float fadeToBlackDuration = 0.75f;

    [SerializeField]
    private float preTeleportDelay = 0.5f;

    [SerializeField]
    private float blackAfterTeleportDuration = 2f;

    [SerializeField]
    private float fadeFromBlackDuration = 0.75f;


    // =========================================================
    // INTERNAL
    // =========================================================

    private Level3State currentState;

    private bool puzzleSolved = false;
    private bool transportationStarted = false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    // =========================================================
    // ENABLE
    // =========================================================

    private void OnEnable()
    {
        if (rollTheBallPuzzle != null)
        {
            rollTheBallPuzzle.OnPuzzleSolved +=
                HandleRollTheBallSolved;

            rollTheBallPuzzle.OnPuzzleCancelled +=
                HandleRollTheBallCancelled;
        }
    }


    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        if (rollTheBallPuzzle != null)
        {
            rollTheBallPuzzle.OnPuzzleSolved -=
                HandleRollTheBallSolved;

            rollTheBallPuzzle.OnPuzzleCancelled -=
                HandleRollTheBallCancelled;
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        currentState = Level3State.Intro;

        puzzleSolved = false;
        transportationStarted = false;


        // -------------------------
        // INTRO IMAGE
        // -------------------------

        if (introPanel != null)
        {
            introPanel.SetActive(true);
        }


        // -------------------------
        // CLUE HIDDEN INITIALLY
        // -------------------------

        if (cluePanel != null)
        {
            cluePanel.SetActive(false);
        }


        // -------------------------
        // FADE INVISIBLE INITIALLY
        // -------------------------

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;

            fadeCanvasGroup.interactable = false;

            fadeCanvasGroup.blocksRaycasts = false;
        }


        // -------------------------
        // DISABLE DEBUG CONTROLS
        // -------------------------

        if (rollTheBallPuzzle != null)
        {
            rollTheBallPuzzle.ShowDebugControls = false;
        }


        // -------------------------
        // PLAYER CANNOT MOVE
        // DURING INTRO
        // -------------------------

        SetPlayerControl(false);


        // -------------------------
        // CURSOR
        // -------------------------

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        // -------------------------
        // INTRO AUDIO
        // -------------------------

        if (level3IntroAudio != null)
        {
            level3IntroAudio.Stop();
            level3IntroAudio.Play();
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // ENTER ON INTRO IMAGE
        // =====================================================

        if (currentState == Level3State.Intro)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                CloseIntroduction();
            }

            return;
        }


        // =====================================================
        // ENTER ON CLUE
        // =====================================================

        if (currentState == Level3State.Clue)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                BeginTransportation();
            }

            return;
        }
    }


    // =========================================================
    // CLOSE INTRO
    // =========================================================

    private void CloseIntroduction()
    {
        if (currentState != Level3State.Intro)
            return;


        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }


        if (level3IntroAudio != null)
        {
            level3IntroAudio.Stop();
        }


        currentState = Level3State.Exploring;


        SetPlayerControl(true);


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    // =========================================================
    // MAGIC CUBE STARTS PUZZLE
    // =========================================================

    public void StartRollTheBallPuzzle()
    {
        if (currentState != Level3State.Exploring)
            return;


        if (puzzleSolved)
            return;


        if (rollTheBallPuzzle == null)
        {
            Debug.LogError(
                "Level3FlowController: " +
                "RollTheBallPuzzleController is not assigned."
            );

            return;
        }


        currentState = Level3State.Puzzle;


        // Stop player movement.
        SetPlayerControl(false);


        // Show cursor for puzzle interaction.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        // Make sure debug controls never appear.
        rollTheBallPuzzle.ShowDebugControls = false;


        // Open puzzle.
        rollTheBallPuzzle.StartPuzzle();
    }


    // =========================================================
    // PUZZLE CANCELLED / CLOSED
    // =========================================================

    private void HandleRollTheBallCancelled()
    {
        // If puzzle was already solved,
        // ignore cancellation completely.
        if (puzzleSolved)
            return;


        currentState = Level3State.Exploring;


        // Give player movement back.
        SetPlayerControl(true);


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        // IMPORTANT:
        // Allow Magic Cube to trigger again.
        if (magicCubeTrigger != null)
        {
            magicCubeTrigger.ResetAfterCancel();
        }
    }


    // =========================================================
    // PUZZLE SOLVED
    // =========================================================

    private void HandleRollTheBallSolved()
    {
        if (puzzleSolved)
            return;


        puzzleSolved = true;


        // Never allow Magic Cube to open puzzle again.
        if (magicCubeTrigger != null)
        {
            magicCubeTrigger.MarkCompleted();
        }


        SetPlayerControl(false);


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        // Puzzle has been solved.
        // Now fade the actual game world to black.
        StartCoroutine(
            FadeToBlackAndShowClue()
        );
    }


    // =========================================================
    // FADE TO BLACK THEN SHOW CLUE
    // =========================================================

    private IEnumerator FadeToBlackAndShowClue()
    {
        currentState = Level3State.FadingToClue;


        // Fade:
        // visible world -> black
        yield return StartCoroutine(
            Fade(
                0f,
                1f,
                fadeToBlackDuration
            )
        );


        // Screen is now completely black.
        currentState = Level3State.Clue;


        // Show clue above the black FadePanel.
        if (cluePanel != null)
        {
            cluePanel.SetActive(true);
        }
    }


    // =========================================================
    // PLAYER PRESSES ENTER ON CLUE
    // =========================================================

    private void BeginTransportation()
    {
        if (currentState != Level3State.Clue)
            return;


        if (transportationStarted)
            return;


        transportationStarted = true;


        // Remove clue.
        if (cluePanel != null)
        {
            cluePanel.SetActive(false);
        }


        // IMPORTANT:
        // FadePanel is still Alpha = 1 here.
        // Therefore the screen remains black.


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        StartCoroutine(
            TransportationSequence()
        );
    }


    // =========================================================
    // TRANSPORTATION SEQUENCE
    // =========================================================

    private IEnumerator TransportationSequence()
    {
        currentState = Level3State.Transporting;


        SetPlayerControl(false);


        // =====================================================
        // TRANSPORTATION AUDIO
        // =====================================================

        if (transportationAudio != null)
        {
            transportationAudio.Stop();
            transportationAudio.Play();
        }


        // Small dramatic pause.
        yield return new WaitForSecondsRealtime(
            preTeleportDelay
        );


        // =====================================================
        // TELEPORT WHILE SCREEN IS BLACK
        // =====================================================

        TeleportPlayerToNinthFloor();


        // =====================================================
        // REMAIN BLACK
        // =====================================================

        yield return new WaitForSecondsRealtime(
            blackAfterTeleportDuration
        );


        // =====================================================
        // FADE BLACK -> NINTH FLOOR
        // =====================================================

        yield return StartCoroutine(
            Fade(
                1f,
                0f,
                fadeFromBlackDuration
            )
        );


        // =====================================================
        // NINTH FLOOR GAMEPLAY
        // =====================================================

        currentState = Level3State.NinthFloor;


        // -----------------------------------------------------
        // IMPORTANT:
        // The ninth floor is now completely visible.
        // Notify the dedicated ninth-floor sequence manager.
        //
        // This:
        // 1. Marks the ninth floor as reached.
        // 2. Shows the top-right objective notification.
        // 3. Plays the notification sound once.
        // 4. Allows the Euler trigger to work.
        // -----------------------------------------------------

        if (ninthFloorSequence != null)
        {
            ninthFloorSequence.NinthFloorReached();
        }
        else
        {
            Debug.LogWarning(
                "Level3FlowController: " +
                "NinthFloorSequence is not assigned."
            );
        }


        // Give player movement back.
        SetPlayerControl(true);


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    // =========================================================
    // TELEPORT PLAYER
    // =========================================================

    private void TeleportPlayerToNinthFloor()
    {
        if (player == null)
        {
            Debug.LogError(
                "Level3FlowController: Detective is not assigned."
            );

            return;
        }


        if (ninthFloorSpawn == null)
        {
            Debug.LogError(
                "Level3FlowController: NinthFloorSpawn is not assigned."
            );

            return;
        }


        if (playerRigidbody != null)
        {
            // Stop existing motion.
            playerRigidbody.linearVelocity =
                Vector3.zero;

            playerRigidbody.angularVelocity =
                Vector3.zero;


            // Move player.
            playerRigidbody.position =
                ninthFloorSpawn.position;


            // Face direction of spawn object.
            playerRigidbody.rotation =
                ninthFloorSpawn.rotation;


            // Ensure no movement remains.
            playerRigidbody.linearVelocity =
                Vector3.zero;

            playerRigidbody.angularVelocity =
                Vector3.zero;
        }

        else
        {
            player.position =
                ninthFloorSpawn.position;

            player.rotation =
                ninthFloorSpawn.rotation;
        }


        Physics.SyncTransforms();
    }


    // =========================================================
    // FADE FUNCTION
    // =========================================================

    private IEnumerator Fade(
        float from,
        float to,
        float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }


        fadeCanvasGroup.blocksRaycasts = true;


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );


            fadeCanvasGroup.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    progress
                );


            yield return null;
        }


        fadeCanvasGroup.alpha = to;


        // When FadePanel becomes invisible,
        // it should no longer block UI/game clicks.
        if (to <= 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }


    // =========================================================
    // PLAYER CONTROL
    // =========================================================

    private void SetPlayerControl(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = enabled;
        }


        // Your Detective3DMovement directly controls
        // Rigidbody velocity.
        //
        // Therefore when disabling the movement script,
        // immediately stop horizontal movement as well.

        if (!enabled &&
            playerRigidbody != null)
        {
            Vector3 currentVelocity =
                playerRigidbody.linearVelocity;


            playerRigidbody.linearVelocity =
                new Vector3(
                    0f,
                    currentVelocity.y,
                    0f
                );
        }
    }
}