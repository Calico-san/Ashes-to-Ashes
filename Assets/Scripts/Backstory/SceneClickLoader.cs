using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SceneClickLoader : MonoBehaviour
{
    [FormerlySerializedAs("gameSceneName")]
    [SerializeField] private string nextSceneName = "AshesToAshes";

    [FormerlySerializedAs("earthquakeAudio")]
    [SerializeField] private AudioSource timedAudioSource;

    private bool canContinue;
    private bool isLoading;

    private void Update()
    {
        if (!canContinue || isLoading)
            return;

        if (WasAnyPointerPressed())
        {
            isLoading = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void EnableClickToContinue()
    {
        canContinue = true;
    }

    public void PlayTimedAudio()
    {
        if (timedAudioSource != null)
            timedAudioSource.Play();
    }

    private bool WasAnyPointerPressed()
    {
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.middleButton.wasPressedThisFrame)
            {
                return true;
            }
        }

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }
}
