using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Detective1DMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float inputDeadZone = 0.15f;

    private Rigidbody rb;
    private float verticalInput;

    private void Awake()
    {
    rb = GetComponent<Rigidbody>();

    // Freeze all 3 rotation axes in code
    rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        // Read input only (W/S or Up/Down Arrow keys)
        // Returns 0 when no key is pressed, 1 for Forward (W/Up), -1 for Backward (S/Down)
        verticalInput = Input.GetAxisRaw("Vertical");

        // Ignore small stray values (e.g. controller stick drift) so the
        // player never moves unless a key/stick is actually pushed past this point
        if (Mathf.Abs(verticalInput) < inputDeadZone)
        {
            verticalInput = 0f;
        }
    }

    private void FixedUpdate()
    {
        // Move using physics so colliders work properly without tunneling/clipping
        Vector3 targetVelocity = transform.forward * verticalInput * moveSpeed;

        // Retain the existing Y velocity to allow normal gravity application
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }
}
