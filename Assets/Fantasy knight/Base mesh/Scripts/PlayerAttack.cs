using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Detección de enemigo")]
    public float attackRange = 2f;
    public LayerMask enemyLayer;

    [Header("Daño de ataques")]
    public int punchDamage = 10;
    public int swordDamage = 20;
    public int powerAttackDamage = 30;

    // Teclas
    public KeyCode attackPunch = KeyCode.V;
    public KeyCode attackSword = KeyCode.R;
    public KeyCode attackKey = KeyCode.T;

    // Tiempo entre ataques
    public float attackCooldown = 0.5f;
    public float lastAttackTime = 0f;
    public Animator animator;

    void Update()
    {
        if (Input.GetKeyDown(attackPunch) && Time.time > lastAttackTime + attackCooldown)
        {
            AttackPunch();
            lastAttackTime = Time.time;
        }

        if (Input.GetKeyDown(attackKey) && Time.time > lastAttackTime + attackCooldown)
        {
            AttackArma();
            lastAttackTime = Time.time;
        }

        if (Input.GetKeyDown(attackSword) && Time.time > lastAttackTime + attackCooldown)
        {
            AttackSword();
            lastAttackTime = Time.time;
        }
    }

    void AttackPunch()
    {
        if (animator != null)
        {
            animator.SetTrigger("Punch");
            // Aplicar daño después de un pequeño delay para que coincida con la animación
            Invoke("ApplyPunchDamage", 0.3f);
        }
    }

    void AttackArma()
    {
        if (animator != null)
        {
            animator.SetTrigger("AttackPower");
            Invoke("ApplyPowerDamage", 0.4f);
        }
    }

    void AttackSword()
    {
        if (animator != null)
        {
            animator.SetTrigger("Sword");
            Invoke("ApplySwordDamage", 0.35f);
        }
    }

    void ApplyPunchDamage()
    {
        ApplyDamageToEnemy(punchDamage);
    }

    void ApplyPowerDamage()
    {
        ApplyDamageToEnemy(powerAttackDamage);
    }

    void ApplySwordDamage()
    {
        ApplyDamageToEnemy(swordDamage);
    }

    void ApplyDamageToEnemy(int damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        foreach (Collider hit in hits)
        {
            // Intentar obtener el DragonController
            DragonController dragon = hit.GetComponent<DragonController>();
            if (dragon != null)
            {
                dragon.TakeDamage(damage);
                Debug.Log($"Golpeaste al dragón con {damage} de daño!");
                continue;
            }

            // Si tienes otros enemigos con DragonController
            DragonController enemy = hit.GetComponent<DragonController>();
            if (enemy != null)
            {
                // enemy.TakeDamage(damage, EnemyController.DamageType.Golpe);
                Debug.Log($"Golpeaste a un enemigo con {damage} de daño!");
            }
        }
    }

    // Visualizar el rango de ataque en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

