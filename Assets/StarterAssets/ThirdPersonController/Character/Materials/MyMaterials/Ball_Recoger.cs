using UnityEngine;


public class Ball_Recoger : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        if( other.CompareTag("Player"))
        {
            var collector = other.GetComponent<PlayerCollector>();
            collector?.RecogerPelota();
            
            Debug.Log("Pelota recogida por el jugador");
            Destroy(gameObject);
            
        }
    }

   
}
