using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("AshesToAshes");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit button pressed");
    }
  
}
