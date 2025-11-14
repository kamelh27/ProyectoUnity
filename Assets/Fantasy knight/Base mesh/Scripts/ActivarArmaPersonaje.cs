using UnityEngine;

public class ActivarArmaPersonaje : MonoBehaviour
{
    public CogerArmas cogerArmas;
    public int numeroArma;
    // public Animator animaciones;

    // private void OnTriggerEnter(Collider other)
    // {
    //     if(other.CompareTag("Player"))
    //     {
    //        
    //     }
    // }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cogerArmas = GameObject.FindGameObjectWithTag("Player").GetComponent<CogerArmas>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            cogerArmas.ActivarArmar(numeroArma);
            // animaciones.SetBool("Arma", true);
            Destroy(gameObject);
        }
    }
}
