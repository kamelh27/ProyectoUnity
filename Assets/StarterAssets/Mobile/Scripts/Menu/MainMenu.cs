using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    public void StartPlaying(string Game)
    {
        SceneManager.LoadScene(Game);
    }

    public void Leave()
    {
        Application.Quit();
        Debug.Log("Salir");
    }

    
}
