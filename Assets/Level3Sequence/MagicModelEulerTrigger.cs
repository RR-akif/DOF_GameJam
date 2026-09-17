using UnityEngine;

public class MagicModelEulerTrigger : MonoBehaviour
{
    [SerializeField] private Level3NinthFloorSequence sequence;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (sequence != null)
            sequence.TryOpenEulerPuzzle();
    }
}