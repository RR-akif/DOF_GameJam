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
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private Transform groundCheckPoint; // empty child at the character's feet

    private Rigidbody rb;
    private Transform cameraTransform;

    private float horizontalInput;
    private float verticalInput;
    private bool jumpRequested;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Freeze all 3 rotation axes in code (we handle rotation manually via MoveRotation)
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // Read movement input (WASD / Arrow keys)
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Ignore small stray values (e.g. controller stick drift)
        if (Mathf.Abs(horizontalInput) < inputDeadZone) horizontalInput = 0f;
        if (Mathf.Abs(verticalInput) < inputDeadZone) verticalInput = 0f;

        // Ground check (done in Update so a Space press isn't missed between physics steps)
        if (groundCheckPoint != null)
        {
            isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckDistance, groundLayer);
        }

        // Queue a jump request; actual force applied in FixedUpdate
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        Vector3 inputDirection = GetCameraRelativeInputDirection();

        Vector3 targetVelocity = inputDirection * moveSpeed;

        // Preserve existing Y velocity so gravity/jumps aren't overwritten
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        // Rotate character to face movement direction
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        // Apply jump as an instantaneous velocity change
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpRequested = false;
        }
    }

    private Vector3 GetCameraRelativeInputDirection()
    {
        Vector3 rawInput = new Vector3(horizontalInput, 0f, verticalInput);
        rawInput = Vector3.ClampMagnitude(rawInput, 1f);

        if (cameraTransform == null)
        {
            // Fallback: world-axis movement if no camera found
            return rawInput;
        }

        // Flatten camera forward/right onto the horizontal plane so pitch doesn't affect movement
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        Vector3 direction = (camForward * verticalInput + camRight * horizontalInput);
        return Vector3.ClampMagnitude(direction, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the ground check sphere in the editor
        if (groundCheckPoint == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckDistance);
    }
}