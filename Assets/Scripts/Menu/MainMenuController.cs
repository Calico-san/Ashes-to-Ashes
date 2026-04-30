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
    [SerializeField] private string _gameSceneName = "Game";

    public void OnStartNewGame()
    {
        SceneManager.LoadScene(_gameSceneName);
    }

    public void OnContinue()
    {
        // Load slot 0 by default — Mislav can expand to slot picker UI
        if (SaveSystem.SlotExists(0))
        {
            GameSceneLoader.ShouldLoadSave = true;
            GameSceneLoader.LoadSlot       = 0;
            SceneManager.LoadScene(_gameSceneName);
        }
        else
        {
            Debug.Log("[MainMenu] No save file found in slot 0.");
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
