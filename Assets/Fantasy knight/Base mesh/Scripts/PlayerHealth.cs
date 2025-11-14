using NUnit.Framework;
using StarterAssets;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración de Vida")]
    public int maxHealth = 100;
    private int currentHealth;
    public bool isDead = false;

    [Header("Referencias")]
    public Animator animator;
    private CharacterController characterController;
    private PlayerAttack playerAttack;

    private MonoBehaviour[] scriptsToDisable;

    [Header("Configuración de Muerte")]
    public float deathAnimationDuration = 2f;
    public bool disableOnDeath = true;

    [Header("Sistema de Empuje (Knockback)")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.5f;
    private bool isBeingKnockedBack = false;
    private Vector3 knockbackDirection;
    private float knockbackTimer = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        if(animator == null)
        {
            animator = GetComponent<Animator>();
        }

        characterController = GetComponent<CharacterController>();
        playerAttack = GetComponent<PlayerAttack>();

        // Obtener todos los scripts del jugador para deshabilitarlos al morir
        scriptsToDisable = GetComponents<MonoBehaviour>();
    }

    void Update()
    {
        if (isBeingKnockedBack && characterController != null)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer > 0)
            {
                // Aplicar movimiento de empuje
                characterController.Move(knockbackDirection * knockbackForce * Time.deltaTime);
            }
            else
            {
                isBeingKnockedBack = false;
            }
        }
    }

    public void TakeDamage(int damage, bool isHornAttack = false, Vector3 attackerPosition = default)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"Jugador recibió {damage} de daño. Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;

            // Si es ataque de cuerno, muerte especial

            if (isHornAttack)
            {
                DieByHornAttack(attackerPosition);
            }
            else
            {
                Die();
            }
        }
        else
        {
            if (animator != null && !isHornAttack)
            {
                // Puedes agregar un trigger de "Hit" si tienes animación de golpe
                // animator.SetTrigger("Hit");
            }

            // Si es ataque de cuerno pero no mata, aplicar knockback
            if (isHornAttack && attackerPosition != default)
            {
                ApplyKnockback(attackerPosition);
            }
        }
    }

    void ApplyKnockback(Vector3 attackerPosition)
    {
        if (characterController == null) return;

        // Calcular dirección del empuje (alejarse del atacante)
        Vector3 direction = (transform.position - attackerPosition).normalized;
        direction.y = 0; // Mantener en el plano horizontal

        knockbackDirection = direction;
        knockbackTimer = knockbackDuration;
        isBeingKnockedBack = true;

        Debug.Log("¡Jugador empujado por el ataque!");

    }

    void DieByHornAttack(Vector3 attackerPosition)
    {
        isDead = true;
        Debug.Log("¡Jugador muerto por ataque de cuerno! - Flying Back Death");

        // Desactivar controles
        DisablePlayerControls();

        // Calcular dirección del empuje para la animación

        if (attackerPosition != default)
        {
            Vector3 direction = (transform.position - attackerPosition).normalized;
            transform.rotation = Quaternion.LookRotation(-direction); // Mirar hacia el atacante
        }
        
        // Activar animación de muerte volando hacia atrás
        if (animator != null)
        {
            animator.SetTrigger("BackDeath");
        }

        // Aplicar empuje fuerte
        if (characterController != null && attackerPosition != null)
        {
            Vector3 direction = (transform.position - attackerPosition).normalized;
            knockbackDirection = direction;
            knockbackForce = 8f; // Fuerza mayor para la muerte
            knockbackTimer = 1f; // Duración mayor
            isBeingKnockedBack = true;
        }

        // Destruír o desactivar después de la animación
        if(disableOnDeath)
        {
            Invoke("DisablePlayer", deathAnimationDuration);
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("¡Jugador muerto!");

        // Desactivar controles
        DisablePlayerControls();

        // Activar animación de muerte normal (si tengo)
        animator.SetTrigger("TwoHandedSwordDeath");

         // Destruir o desactivar después de la animación
        if(disableOnDeath)
        {
            Invoke("DisablePlayer", deathAnimationDuration);
        }

    }


    void  DisablePlayerControls()
    {
        // Desactivar script de ataque
        if(playerAttack != null)
        {
            playerAttack.enabled = false;
        }

        GetComponent<BasicRigidBodyPush>().enabled = false;

        GetComponent<PlayerCollector>().enabled = false;
        GetComponent<ThirdPersonController>().enabled = false;

        // Desactivar otros scripts de control
        // Si tienes un script de movimiento, desactívalo aquí
        // Ejemplo: GetComponent<PlayerMovement>().enabled = false;

        // Desactivar el CharacterController para que no interfiera
        // (pero mantenerlo activo si necesitas el knockback)
    }

    void DisablePlayer()
    {
        // Opción 1: Desactivar el GameObject
        gameObject.SetActive(false);

        // Opción 2: Solo desactivar el renderer (hacer invisible)
        // GetComponent<Renderer>().enabled = false;

        // Opción 3: Destruir el objeto
        // Destroy(gameObject);
    }

      // Método para curar al jugador (opcional)
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        Debug.Log($"Jugador curado. Vida: {currentHealth}/{maxHealth}");
    }

    // Método para obtener la vida actual (para UI)
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
}
