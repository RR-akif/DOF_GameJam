using UnityEngine;

public class DetectiveCollisionHandler : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(
            "COLLISION WITH: " +
            collision.gameObject.name
        );
    }
}