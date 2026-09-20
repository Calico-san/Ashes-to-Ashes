using System.Collections;
using UnityEngine;

public class FadeManager : MonoBehaviour
{
    public CanvasGroup fadeGroup;
    public float fadeDuration = 1.0f;
    public float holdDuration = 0.5f;

    private GameController gameController;

    public void Initialize(GameController controller)
    {
        gameController = controller;
        gameController.DayEnding += HandleDayEnding;
    }

    private bool HandleDayEnding(int day, int hour)
    {
        if (fadeGroup == null) return false;

        gameController.SetSpeed(0);
        StartCoroutine(FadeSequence());
        return true;
    }

    private IEnumerator FadeSequence()
    {
        // 1. Fade to black
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 1f;

        // 2. Change the day while the screen is black
        gameController.AdvanceToNextDay();
        yield return new WaitForSeconds(holdDuration);

        // 3. Fade back to clear
        time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, time / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 0f;
    }
}
