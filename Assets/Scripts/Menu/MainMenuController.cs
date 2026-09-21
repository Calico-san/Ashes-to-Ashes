using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private const float MenuActionDelay = 0.1f;

    [Header("Scene Names")]
    [SerializeField] private string _backstorySceneName = "Backstory";
    [SerializeField] private string _gameSceneName = "AshesToAshes";

    private bool _actionPending;

    public void OnStartNewGame()
    {
        if (_actionPending)
            return;

        GameSceneLoader.ShouldLoadSave = false;
        BeginLoadScene(_backstorySceneName);
    }

    public void OnContinue()
    {
        int latest = SaveSystem.GetLatestSlot();

        if (latest >= 0)
        {
            if (_actionPending)
                return;

            GameSceneLoader.ShouldLoadSave = true;
            GameSceneLoader.LoadSlot = latest;

            BeginLoadScene(_gameSceneName);
        }
        else
        {
            Debug.Log("[MainMenu] No save files found.");
        }
    }

    public void OnQuit()
    {
        if (_actionPending)
            return;

        _actionPending = true;
        StartCoroutine(QuitAfterClick());
    }

    private void BeginLoadScene(string sceneName)
    {
        _actionPending = true;
        StartCoroutine(LoadSceneAfterClick(sceneName));
    }

    private IEnumerator LoadSceneAfterClick(string sceneName)
    {
        yield return new WaitForSecondsRealtime(MenuActionDelay);
        SceneManager.LoadScene(sceneName);
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