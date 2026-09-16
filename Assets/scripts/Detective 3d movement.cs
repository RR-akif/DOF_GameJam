using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Detective3DMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float inputDeadZone = 0.15f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7.0f;

    private Rigidbody rb;
    private Transform cameraTransform;

    private float horizontalInput;
    private float verticalInput;
    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Prevent physics from rotating the player.
        // Rotation is controlled manually below.
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // WASD + Arrow Keys
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Remove very small unwanted input
        if (Mathf.Abs(horizontalInput) < inputDeadZone)
            horizontalInput = 0f;

        if (Mathf.Abs(verticalInput) < inputDeadZone)
            verticalInput = 0f;

        // Jump ANYWHERE.
        // No grounded check is required.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        Vector3 inputDirection = GetCameraRelativeInputDirection();

        Vector3 targetVelocity = inputDirection * moveSpeed;

        // Horizontal movement.
        // Keep the current Y velocity so gravity/jumping continues normally.
        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            rb.linearVelocity.y,
            targetVelocity.z
        );

        // Rotate player toward movement direction
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(inputDirection, Vector3.up);

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                )
            );
        }

        // Jump
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                jumpForce,
                rb.linearVelocity.z
            );

            jumpRequested = false;
        }
    }

    private Vector3 GetCameraRelativeInputDirection()
    {
        Vector3 rawInput =
            new Vector3(horizontalInput, 0f, verticalInput);

        rawInput = Vector3.ClampMagnitude(rawInput, 1f);

        if (cameraTransform == null)
        {
            return rawInput;
        }

        // Ignore camera's vertical tilt.
        // Movement remains on horizontal X-Z plane.
        Vector3 camForward =
            Vector3.ProjectOnPlane(
                cameraTransform.forward,
                Vector3.up
            ).normalized;

        Vector3 camRight =
            Vector3.ProjectOnPlane(
                cameraTransform.right,
                Vector3.up
            ).normalized;

        Vector3 direction =
            camForward * verticalInput +
            camRight * horizontalInput;

        return Vector3.ClampMagnitude(direction, 1f);
    }
}