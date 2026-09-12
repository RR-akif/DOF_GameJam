using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Detective1DMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;

    private Rigidbody rb;
    private float verticalInput;

    private void Awake()
    {
        // Get the Rigidbody component attached to the player
        rb = GetComponent<Rigidbody>();

        // Freeze rotation so the player doesn't tip over on collision
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        // Read input only (W/S or Up/Down Arrow keys)
        // Returns 0 when no key is pressed, 1 for Forward (W/Up), -1 for Backward (S/Down)
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        // Move using physics so colliders work properly without tunneling/clipping
        Vector3 targetVelocity = transform.forward * verticalInput * moveSpeed;
        
        // Retain the existing Y velocity to allow normal gravity application
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }
}