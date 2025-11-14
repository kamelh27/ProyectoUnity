using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CogerArmas: MonoBehaviour
{
    public GameObject[] armas;
    public Animator animaciones;


    public void ActivarArmar(int numero)
    {
        for (int i = 0; i < armas.Length; i++)
        {
            armas[i].SetActive(false);
        }

        armas[numero].SetActive(true);
        animaciones.SetBool("Arma", true);
    }

    void EndAnimator ()
    {
        animaciones.SetBool("Arma", false);
    }

    
}
