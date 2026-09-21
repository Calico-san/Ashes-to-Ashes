using UnityEngine;
using UnityEngine.SceneManagement;

public class EruptionLoader : MonoBehaviour
{
    private void Start()
    {
        Time.timeScale = 1f;
        Invoke(nameof(LoadEnding), 20f);
    }

    private void LoadEnding()
    {
        SceneManager.LoadScene("EndingEruption");
    }
}
