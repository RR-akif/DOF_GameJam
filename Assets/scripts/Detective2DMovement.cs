using UnityEngine;

public class Detective2DMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 8f;

    private void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        // -----------------------------------
        // INPUT
        // -----------------------------------

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

        // -----------------------------------
        // LOCAL AXES
        // -----------------------------------

        Vector3 localForward = transform.forward;
        Vector3 localRight = transform.right;

        // Prevent vertical movement
        localForward.y = 0f;
        localRight.y = 0f;

        localForward.Normalize();
        localRight.Normalize();

        // -----------------------------------
        // MOVEMENT DIRECTION
        // -----------------------------------

        Vector3 moveDirection =
            (localForward * vertical) +
            (localRight * horizontal);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // -----------------------------------
        // MOVE
        // -----------------------------------

        transform.position +=
            moveDirection * moveSpeed * Time.deltaTime;

        // -----------------------------------
        // ROTATE TOWARD MOVEMENT
        // -----------------------------------

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
        }
    }
}