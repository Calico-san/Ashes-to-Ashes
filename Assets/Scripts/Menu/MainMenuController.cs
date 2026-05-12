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
    [Header("Scene Names")]
    [SerializeField] private string _gameSceneName = "AshesToAshes";

    public void OnStartNewGame()
    {
        GameSceneLoader.ShouldLoadSave = false;
        SceneManager.LoadScene(_gameSceneName);
    }

    public void OnContinue()
    {
        int latest = SaveSystem.GetLatestSlot();
        if (latest >= 0)
        {
            GameSceneLoader.ShouldLoadSave = true;
            GameSceneLoader.LoadSlot       = latest;
            SceneManager.LoadScene(_gameSceneName);
        }
        else
        {
            Debug.Log("[MainMenu] No save files found.");
            // Optionally: show feedback text in UI
        }
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
