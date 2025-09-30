using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ExportNotificationManager : MonoBehaviour
{
    [SerializeField] private GameObject notifyGameObj;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float visibleDuration = 2f;

    private CanvasGroup canvasGroup;
    private Coroutine currentRoutine;

    private void Awake()
    {
        canvasGroup = notifyGameObj.GetComponent<CanvasGroup>();
        notifyGameObj.SetActive(false);
    }

    public void ShowNotification(string messageToShow)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        notifyGameObj.SetActive(true);
        currentRoutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        // Fade in
        yield return Fade(0, 1, fadeDuration);

        // Wait visible
        yield return new WaitForSeconds(visibleDuration);

        // Fade out
        yield return Fade(1, 0, fadeDuration);

        notifyGameObj.SetActive(false);
    }

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
