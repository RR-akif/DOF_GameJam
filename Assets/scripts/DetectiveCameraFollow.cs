using UnityEngine;

public class DetectiveCameraFollow : MonoBehaviour
{
    public Transform target;

    public float height = 3f;
    public float distance = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Put camera behind the target
        Vector3 cameraPosition =
            target.position
            - target.forward * distance
            + Vector3.up * height;

        transform.position = cameraPosition;

        // Look toward the target
        transform.LookAt(target.position);
    }
}