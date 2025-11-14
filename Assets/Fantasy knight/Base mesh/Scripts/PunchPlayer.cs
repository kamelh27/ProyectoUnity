using UnityEngine;

public class PunchPlayer : MonoBehaviour
{

    public KeyCode attackPunch = KeyCode.V;
    // Tiempo entre ataques
    public float attackCooldown = 0.5f;
    private float lastAttackTime = 0f;

    // Aquí puedes enlazar una animación o efecto
    public Animator animator;

    void Update()
    {
        // Detecta la tecla (ejemplo: barra espaciadora)
        if (Input.GetKeyDown(attackPunch) && Time.time > lastAttackTime + attackCooldown)
        {
            AttackPunch();
            lastAttackTime = Time.time;
        }
    }

    void AttackPunch()
    {
        Debug.Log("¡Ataque ejecutado!");

        // Si tienes un Animator con un trigger llamado "Attack"
        if (animator != null)
        {
            animator.SetTrigger("Punch");
        }

        // Aquí podrías añadir lógica de daño, colisiones, etc.
    }
    
    
}
