using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Small helper to show a temporary toast-like notification with fade in/out.
/// Attach to a GameObject and set references in the Inspector.
/// </summary>
public class ExportNotificationManager : MonoBehaviour
{
    [SerializeField] private GameObject notifyGameObj;   // Root object that contains the visual (must have CanvasGroup)
    [SerializeField] private float fadeDuration = 0.5f;  // Time for fade in/out
    [SerializeField] private float visibleDuration = 2f; // Time fully visible before fading out
    [SerializeField] private TMP_Text notifyText;        // Text element to show the message

    private CanvasGroup canvasGroup;                     // Cached CanvasGroup on notifyGameObj
    private Coroutine currentRoutine;                    // Avoid overlapping coroutines

    private void Awake()
    {
        // Cache CanvasGroup and start hidden
        canvasGroup = notifyGameObj.GetComponent<CanvasGroup>();
        notifyGameObj.SetActive(false);
    }

    /// <summary>
    /// Shows a notification message with fade in, wait, and fade out.
    /// If already visible, restarts the sequence.
    /// </summary>
    public void ShowNotification(string messageToShow)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        notifyGameObj.SetActive(true);
        notifyText.text = messageToShow;
        currentRoutine = StartCoroutine(FadeRoutine());
    }

    /// <summary>
    /// Full sequence: fade in -> wait visible -> fade out -> deactivate.
    /// </summary>
    private IEnumerator FadeRoutine()
    {
        // Fade in
        yield return Fade(0, 1, fadeDuration);

        // Keep visible
        yield return new WaitForSeconds(visibleDuration);

        // Fade out
        yield return Fade(1, 0, fadeDuration);

        notifyGameObj.SetActive(false);
    }

    /// <summary>
    /// Generic alpha interpolation for the CanvasGroup.
    /// </summary>
    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
