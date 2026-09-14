using UnityEngine;

public class Detective2DMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        // Left / Right
        if (Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal = -1f;
        }

        if (Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.RightArrow))
        {
            horizontal = 1f;
        }

        // Forward / Backward
        if (Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.UpArrow))
        {
            vertical = 1f;
        }

        if (Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.DownArrow))
        {
            vertical = -1f;
        }

        // No input
        if (horizontal == 0f && vertical == 0f)
            return;

        /*
         * IMPORTANT:
         * Use the detective's CURRENT forward and right directions,
         * instead of fixed world X/Z.
         */
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // Keep movement completely horizontal
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement =
            forward * vertical +
            right * horizontal;

        movement.Normalize();

        // Move
        transform.position +=
            movement * moveSpeed * Time.deltaTime;

        // Rotate toward the direction we are moving
        Quaternion targetRotation =
            Quaternion.LookRotation(movement, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}