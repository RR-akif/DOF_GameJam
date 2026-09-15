using UnityEngine;
using System.Collections;

public class Level2PopupManager : MonoBehaviour
{
    [Header("First Popup")]
    [SerializeField] private GameObject firstPopup;

    [SerializeField] private float firstPopupDelay = 2f;
    [SerializeField] private float firstPopupDuration = 5f;

    private void Start()
    {
        // Make sure it isn't visible at the beginning
        firstPopup.SetActive(false);

        StartCoroutine(ShowFirstPopup());
    }

    private IEnumerator ShowFirstPopup()
    {
        // Wait 2 seconds after Level 2 starts
        yield return new WaitForSeconds(firstPopupDelay);

        // Show popup
        firstPopup.SetActive(true);

        // Keep it visible for 5 seconds
        yield return new WaitForSeconds(firstPopupDuration);

        // Hide popup
        firstPopup.SetActive(false);
    }
}