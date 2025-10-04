using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCollector : MonoBehaviour
{
  
    public int pelotasRecogidas = 0; // contador
    public int totalPelotas = 10;
    public TMP_Text contadorText;
    public TMP_Text mensajeFinalText;

    void Start()
    {
        contadorText.text = "0";
        if (mensajeFinalText != null)
            mensajeFinalText.text = "";
    }


    public void RecogerPelota()
    {
      
        SumarPuntos(1);

        Debug.Log("Pelotas recogidas: " + pelotasRecogidas);
    }

    public void SumarPuntos(int j)
    {
        pelotasRecogidas += j;
        contadorText.text = pelotasRecogidas.ToString(); 

        if(pelotasRecogidas >= totalPelotas)
        {
            if(mensajeFinalText != null)
                mensajeFinalText.text = "🎉 ¡Felicidades! Has recogido todas las pelotas 🎉";

            Debug.Log("¡Juego completado!");
        }
    }
}
