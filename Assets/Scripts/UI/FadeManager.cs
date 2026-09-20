using System.Collections;
using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public CanvasGroup fadeGroup;
    public float fadeDuration = 1.0f;
    public float holdDuration = 0.5f;

    [ContextMenu("Test Full Fade Sequence")]
    public void FadeToBlackAndBack()
    {
        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        // 1. Fade to Black
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 1f;

        // 2. Wait while screen is full black
        yield return new WaitForSeconds(holdDuration);

        // 3. Fade back to Clear
        time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, time / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 0f;
    }

    //public CanvasGroup fadeGroup;
    //public float duration = 1.0f;

    //[ContextMenu("Fade To Black")]
    //public void FadeToBlack()
    //{
    //    StartCoroutine(FadeRoutine(1.0f));
    //}

    //[ContextMenu("Fade To Clear")]
    //public void FadeToClear()
    //{
    //    StartCoroutine(FadeRoutine(0.0f));
    //}

    //private IEnumerator FadeRoutine(float targetAlpha)
    //{
    //    float startAlpha = fadeGroup.alpha;
    //    float time = 0f;

    //    while (time < duration)
    //    {
    //        time += Time.deltaTime;
    //        fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
    //        yield return null;
    //    }

    //    fadeGroup.alpha = targetAlpha;
    //}
}