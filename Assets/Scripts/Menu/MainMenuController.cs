using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a GameObject in the MainMenu scene.
/// Wire UI buttons to these methods via the Inspector.
///
/// Build Settings scene order:
///   0 — MainMenu
///   1 — Game
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private const float MenuActionDelay = 0.1f;

    [Header("Scene Names")]
    [SerializeField] private string _gameSceneName = "AshesToAshes";

    private bool _actionPending;

    public void OnStartNewGame()
    {
        if (_actionPending) return;

        GameSceneLoader.ShouldLoadSave = false;
        BeginLoadGame();
    }

    public void OnContinue()
    {
        int latest = SaveSystem.GetLatestSlot();
        if (latest >= 0)
        {
            if (_actionPending) return;

            GameSceneLoader.ShouldLoadSave = true;
            GameSceneLoader.LoadSlot       = latest;
            BeginLoadGame();
        }
        else
        {
            Debug.Log("[MainMenu] No save files found.");
            // Optionally: show feedback text in UI
        }
    }

    public void OnQuit()
    {
        if (_actionPending) return;

        _actionPending = true;
        StartCoroutine(QuitAfterClick());
    }

    private void BeginLoadGame()
    {
        _actionPending = true;
        StartCoroutine(LoadGameAfterClick());
    }

    private IEnumerator LoadGameAfterClick()
    {
        // The button's next Inspector listener plays the click sound. Keep the
        // menu scene alive briefly so its scene-local AudioSource can emit it.
        yield return new WaitForSecondsRealtime(MenuActionDelay);
        SceneManager.LoadScene(_gameSceneName);
    }

    private IEnumerator QuitAfterClick()
    {
        yield return new WaitForSecondsRealtime(MenuActionDelay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
