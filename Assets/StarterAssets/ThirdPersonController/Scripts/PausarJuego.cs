using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausarJuego : MonoBehaviour
{
    public GameObject menuPausa;
    public bool isGamePaused = false;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            isGamePaused = !isGamePaused;
            PauseGame();
            Reanudar();
        }
    }

    public void Reanudar()
    {
        Time.timeScale = 1;
        menuPausa.SetActive(false);
        
    }


    public void PauseGame()
    {
      
        Time.timeScale = 0;
        menuPausa.SetActive(true);
       
    }
}
