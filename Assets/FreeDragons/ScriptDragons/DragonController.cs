using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DragonController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Animator animator;
    private NavMeshAgent agent;

    [Header("Configuración de Vida")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Configuración de Detección")]
    public float detectionRange = 15f;
    public float attackRange = 6f;
    public float chaseRange = 20f;
    public float losePlayerRange = 25f; // Nueva: rango para perder al jugador

    [Header("Configuración de Combate")]
    public float attackCooldown = 2f;
    private float lastAttackTime = 0f;
    public int basicAttackDamage = 15;
    public int hornAttackDamage = 25;
    public int screamDamage = 10; // Daño por área del grito
    
    [Header("Sistema de Combos")]
    public bool useComboSystem = true;
    public float comboChance = 0.6f; // 60% de probabilidad de hacer combo
    private bool isInCombo = false;
    private int comboStep = 0;

    [Header("Sistema de Patrullaje")]
    public bool enablePatrol = true;
    public float patrolRadius = 10f; // Radio de patrullaje desde la posición inicial
    public float patrolWaitTime = 3f; // Tiempo de espera en cada punto
    private Vector3 startPosition;
    private Vector3 currentPatrolTarget;
    private float patrolWaitTimer = 0f;
    private bool isWaiting = false;
    
    [Header("Sistema de Alerta")]
    public bool screamOnDetection = true; // Gritar al detectar jugador
    private bool hasDetectedPlayer = false; // Para gritar solo una vez
    private bool isScreaming = false;

    [Header("Configuración de Movimiento")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;

    private enum DragonState
    {
        Idle,
        Patrol,      // Nuevo estado
        Walk,
        Run,
        BasicAttack,
        HornAttack,
        Scream,
        GetHit,
        Die
    }

    private DragonState currentState = DragonState.Idle;
    private bool isDead = false;
    private bool isAttacking = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // Guardar posición inicial para patrullaje
        startPosition = transform.position;

        // Buscar al jugador si no está asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        if (agent != null)
        {
            agent.speed = walkSpeed;
        }

        // Iniciar en patrullaje si está habilitado
        if (enablePatrol)
        {
            SetNewPatrolPoint();
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Máquina de estados
        switch (currentState)
        {
            case DragonState.Idle:
                HandleIdleState(distanceToPlayer);
                break;

            case DragonState.Patrol:
                HandlePatrolState(distanceToPlayer);
                break;

            case DragonState.Walk:
                HandleWalkState(distanceToPlayer);
                break;

            case DragonState.Run:
                HandleRunState(distanceToPlayer);
                break;

            case DragonState.BasicAttack:
            case DragonState.HornAttack:
                HandleAttackState(distanceToPlayer);
                break;
        }
    }

    void HandleIdleState(float distance)
    {
        if (distance <= detectionRange)
        {
            // Jugador detectado
            if (!hasDetectedPlayer && screamOnDetection)
            {
                // Primera vez que detecta al jugador - gritar
                StartCoroutine(ScreamAndChase(distance));
            }
            else
            {
                // Ya lo había detectado antes - perseguir directamente
                if (distance <= attackRange)
                {
                    ChangeState(DragonState.BasicAttack);
                }
                else
                {
                    ChangeState(DragonState.Run);
                }
            }
        }
        else if (enablePatrol)
        {
            // No hay jugador cerca - patrullar
            ChangeState(DragonState.Patrol);
        }
    }

    System.Collections.IEnumerator ScreamAndChase(float distance)
    {
        hasDetectedPlayer = true;
        isScreaming = true;
        
        Debug.Log("¡DRAGÓN DETECTÓ AL JUGADOR! - Gritando de alerta");
        
        // Mirar hacia el jugador
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        
        // Detener movimiento y gritar
        if (agent != null)
        {
            agent.isStopped = true;
        }
        
        animator.SetTrigger("Scream");
        
        // Esperar a que termine el grito (2.67 segundos según tu animación)
        yield return new WaitForSeconds(2.7f);
        
        isScreaming = false;
        
        // Después del grito, comenzar persecución
        if (distance <= attackRange)
        {
            ChangeState(DragonState.BasicAttack);
        }
        else
        {
            ChangeState(DragonState.Run);
        }
    }

    void HandlePatrolState(float distance)
    {
        // Si detecta al jugador, dejar de patrullar
        if (distance <= detectionRange)
        {
            isWaiting = false;
            
            if (!hasDetectedPlayer && screamOnDetection)
            {
                // Primera detección - gritar
                StartCoroutine(ScreamAndChase(distance));
                ChangeState(DragonState.Idle); // Cambiar a idle para el grito
            }
            else
            {
                // Ya lo había detectado - perseguir
                ChangeState(DragonState.Run);
            }
            return;
        }

        // Si está esperando en un punto
        if (isWaiting)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0)
            {
                isWaiting = false;
                SetNewPatrolPoint();
            }
            return;
        }

        // Moverse hacia el punto de patrullaje
        if (agent != null && agent.enabled)
        {
            agent.speed = walkSpeed;
            
            // Verificar si llegó al destino
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // Llegó al punto - esperar
                isWaiting = true;
                patrolWaitTimer = patrolWaitTime;
                ChangeState(DragonState.Idle);
            }
        }
    }

    void HandleWalkState(float distance)
    {
        if (distance <= attackRange)
        {
            ChangeState(DragonState.BasicAttack);
        }
        else if (distance > chaseRange)
        {
            ChangeState(DragonState.Idle);
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    void HandleRunState(float distance)
    {
        if (distance <= attackRange)
        {
            ChangeState(DragonState.BasicAttack);
        }
        else if (distance > losePlayerRange)
        {
            // Jugador muy lejos - volver a patrullar y resetear detección
            Debug.Log("Jugador perdido, volviendo a patrullar");
            hasDetectedPlayer = false; // Resetear para que grite la próxima vez
            
            if (enablePatrol)
            {
                SetNewPatrolPoint();
                ChangeState(DragonState.Patrol);
            }
            else
            {
                ChangeState(DragonState.Idle);
            }
        }
        else
        {
            // Perseguir al jugador corriendo
            if (agent != null)
            {
                agent.speed = runSpeed;
            }
            MoveTowardsPlayer();
        }
    }

    void HandleAttackState(float distance)
    {
        // No atacar si está gritando
        if (isScreaming) return;
        
        if (!isAttacking && distance <= attackRange)
        {
            if (Time.time > lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }
        else if (distance > attackRange)
        {
            ChangeState(DragonState.Run);
        }
    }

    void MoveTowardsPlayer()
    {
        if (agent != null && agent.enabled)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            // Si no hay NavMeshAgent, mover directamente
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * (currentState == DragonState.Run ? runSpeed : walkSpeed) * Time.deltaTime;
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        }
    }

    void PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Detener movimiento durante ataque
        if (agent != null)
        {
            agent.isStopped = true;
        }

        // Mirar hacia el jugador
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));

        // Sistema de combos
        if (useComboSystem && !isInCombo && Random.value < comboChance)
        {
            // Iniciar combo
            isInCombo = true;
            comboStep = 0;
            StartCoroutine(ExecuteCombo());
        }
        else
        {
            // Ataque simple aleatorio
            ExecuteSingleAttack();
        }
    }

    System.Collections.IEnumerator ExecuteCombo()
    {
        Debug.Log("¡Dragon inicia COMBO!");
        
        // Elegir un patrón de combo aleatorio
        int comboPattern = Random.Range(0, 4);
        
        switch (comboPattern)
        {
            case 0: // Combo Agresivo: Basic → Basic → Horn
                yield return StartCoroutine(ComboPattern1());
                break;
                
            case 1: // Combo Intimidante: Scream → Horn → Basic
                yield return StartCoroutine(ComboPattern2());
                break;
                
            case 2: // Combo Rápido: Basic → Horn → Basic
                yield return StartCoroutine(ComboPattern3());
                break;
                
            case 3: // Combo Devastador: Horn → Scream → Horn
                yield return StartCoroutine(ComboPattern4());
                break;
        }
        
        isInCombo = false;
        comboStep = 0;
        
        // Resetear después del combo
        yield return new WaitForSeconds(0.5f);
        ResetAttack();
        
        Debug.Log("Combo terminado!");
    }

    // Patrón 1: Agresivo - Basic → Basic → Horn
    System.Collections.IEnumerator ComboPattern1()
    {
        Debug.Log("Combo Patrón 1: Triple Golpe");
        
        // Ataque 1: Basic
        animator.SetTrigger("Basic Attack");
        yield return new WaitForSeconds(0.5f);
        DealBasicDamage();
        yield return new WaitForSeconds(0.8f);
        
        // Verificar si el jugador sigue cerca
        if (!CheckPlayerInRange()) yield break;
        
        // Ataque 2: Basic
        animator.SetTrigger("Basic Attack");
        yield return new WaitForSeconds(0.5f);
        DealBasicDamage();
        yield return new WaitForSeconds(0.8f);
        
        if (!CheckPlayerInRange()) yield break;
        
        // Ataque 3: Horn (finisher)
        animator.SetTrigger("HornAttack");
        yield return new WaitForSeconds(0.7f);
        DealHornDamage();
        yield return new WaitForSeconds(0.8f);
    }

    // Patrón 2: Intimidante - Scream → Horn → Basic
    System.Collections.IEnumerator ComboPattern2()
    {
        Debug.Log("Combo Patrón 2: Grito de Guerra");
        
        // Ataque 1: Scream (stun + daño menor)
        animator.SetTrigger("Scream");
        yield return new WaitForSeconds(1.0f);
        DealScreamDamage();
        yield return new WaitForSeconds(1.5f);
        
        if (!CheckPlayerInRange()) yield break;
        
        // Ataque 2: Horn (aprovechar el stun)
        animator.SetTrigger("HornAttack");
        yield return new WaitForSeconds(0.7f);
        DealHornDamage();
        yield return new WaitForSeconds(0.8f);
        
        if (!CheckPlayerInRange()) yield break;
        
        // Ataque 3: Basic (remate)
        animator.SetTrigger("Basic Attack");
        yield return new WaitForSeconds(0.5f);
        DealBasicDamage();
        yield return new WaitForSeconds(0.8f);
    }

    // Patrón 3: Rápido - Basic → Horn → Basic
    System.Collections.IEnumerator ComboPattern3()
    {
        Debug.Log("Combo Patrón 3: Golpe Rápido");
        
        // Ataque 1: Basic
        animator.SetTrigger("Basic Attack");
        yield return new WaitForSeconds(0.5f);
        DealBasicDamage();
        yield return new WaitForSeconds(0.6f); // Más rápido
        
        if (!CheckPlayerInRange()) yield break;
        
        // Ataque 2: Horn
        animator.SetTrigger("HornAttack");
        yield return new WaitForSeconds(0.7f);
        DealHornDamage();
        yield return new WaitForSeconds(0.6f);
        
        if (!CheckPlayerInRange()) yield break;
        
        // Ataque 3: Basic
        animator.SetTrigger("Basic Attack");
        yield return new WaitForSeconds(0.5f);
        DealBasicDamage();
        yield return new WaitForSeconds(0.8f);
    }

    // Patrón 4: Devastador - Horn → Scream → Horn
    System.Collections.IEnumerator ComboPattern4()
    {
        Debug.Log("Combo Patrón 4: Furia del Dragón");
        
        // Ataque 1: Horn
        animator.SetTrigger("HornAttack");
        yield return new WaitForSeconds(0.7f);
        DealHornDamage();
        yield return new WaitForSeconds(0.8f);
        
        if (!CheckPlayerInRange()) yield break;
        
        // Ataque 2: Scream (intimidar)
        animator.SetTrigger("Scream");
        yield return new WaitForSeconds(1.0f);
        DealScreamDamage();
        yield return new WaitForSeconds(1.5f);
        
        if (!CheckPlayerInRange()) yield break;
        
        // Ataque 3: Horn (finisher poderoso)
        animator.SetTrigger("HornAttack");
        yield return new WaitForSeconds(0.7f);
        DealHornDamage();
        yield return new WaitForSeconds(0.8f);
    }

    void ExecuteSingleAttack()
    {
        // Ataque simple aleatorio (comportamiento original)
        if (Random.value > 0.5f)
        {
            animator.SetTrigger("Basic Attack");
            Invoke("DealBasicDamage", 0.5f);
        }
        else
        {
            animator.SetTrigger("HornAttack");
            Invoke("DealHornDamage", 0.7f);
        }

        // Resetear ataque después de la animación
        Invoke("ResetAttack", 1.5f);
    }

    bool CheckPlayerInRange()
    {
        if (player == null || isDead) return false;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance > attackRange * 1.5f) // Un poco más de margen
        {
            Debug.Log("Jugador muy lejos, interrumpiendo combo");
            return false;
        }
        
        return true;
    }

    void DealBasicDamage()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange)
        {
            // Aplicar daño al jugador si tiene un script de vida
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(basicAttackDamage);
            }
        }
    }

    void DealHornDamage()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(hornAttackDamage);
            }
        }
    }
    
    void DealScreamDamage()
    {
        // El grito tiene un rango mayor
        float screamRange = attackRange * 2f;
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= screamRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(screamDamage);
                Debug.Log($"¡Grito del dragón causa {screamDamage} de daño!");
            }
        }
    }

    void ResetAttack()
    {
        isAttacking = false;
        if (agent != null && !isDead)
        {
            agent.isStopped = false;
        }
    }

    void ChangeState(DragonState newState)
    {
        if (currentState == newState || isDead) return;

        currentState = newState;

        // Resetear triggers
        animator.ResetTrigger("Basic Attack");
        animator.ResetTrigger("Get Hit");
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Die");
        animator.ResetTrigger("Run");
        animator.ResetTrigger("Scream");
        animator.ResetTrigger("HornAttack");

        // Activar animación según el estado
        switch (newState)
        {
            case DragonState.Idle:
                // Idle es el estado por defecto
                if (agent != null) agent.isStopped = true;
                break;
            case DragonState.Patrol:
            case DragonState.Walk:
                animator.SetTrigger("Walk");
                if (agent != null)
                {
                    agent.speed = walkSpeed;
                    agent.isStopped = false;
                }
                break;
            case DragonState.Run:
                animator.SetTrigger("Run");
                if (agent != null)
                {
                    agent.speed = runSpeed;
                    agent.isStopped = false;
                }
                break;
        }
    }

    void SetNewPatrolPoint()
    {
        // Generar un punto aleatorio alrededor de la posición inicial
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;
        randomDirection.y = startPosition.y; // Mantener la misma altura

        // Si hay NavMesh, buscar el punto más cercano en el NavMesh
        if (agent != null)
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                currentPatrolTarget = hit.position;
                agent.SetDestination(currentPatrolTarget);
                Debug.Log($"Nuevo punto de patrullaje: {currentPatrolTarget}");
            }
        }
        else
        {
            currentPatrolTarget = randomDirection;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"Dragon recibió {damage} de daño. Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            // Resetear otros triggers primero
            animator.ResetTrigger("Basic Attack");
            animator.ResetTrigger("HornAttack");
            animator.ResetTrigger("Walk");
            animator.ResetTrigger("Run");
            animator.ResetTrigger("Scream");
            
            // Activar animación de golpe
            animator.SetTrigger("Get Hit");
            Debug.Log("Activando animación Get Hit");
            
            // Detener ataque si estaba atacando
            if (isAttacking)
            {
                CancelInvoke("ResetAttack");
                CancelInvoke("DealBasicDamage");
                CancelInvoke("DealHornDamage");
                isAttacking = false;
                if (agent != null)
                {
                    agent.isStopped = false;
                }
            }
            
            // Opcional: gritar cuando tiene poca vida
            if (currentHealth < maxHealth * 0.3f && Random.value > 0.8f)
            {
                StartCoroutine(ScreamAfterHit());
            }
        }
    }
    
    System.Collections.IEnumerator ScreamAfterHit()
    {
        // Esperar a que termine la animación de golpe
        yield return new WaitForSeconds(1.5f);
        
        if (!isDead && !isAttacking)
        {
            animator.SetTrigger("Scream");
            Debug.Log("Dragon grita de dolor");
        }
    }

    void Die()
    {
        if (isDead) return; // Evitar llamadas múltiples
        
        isDead = true;
        currentState = DragonState.Die;
        
        Debug.Log("=== DRAGON MURIENDO ===");
        
        // PRIMERO: Activar la animación de muerte INMEDIATAMENTE
        // Resetear todos los triggers primero
        animator.ResetTrigger("Basic Attack");
        animator.ResetTrigger("Get Hit");
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Run");
        animator.ResetTrigger("Scream");
        animator.ResetTrigger("HornAttac");
        
        // Activar el trigger de muerte
        animator.SetTrigger("Die");
        Debug.Log("Trigger 'Die' activado");
        
        // SEGUNDO: Detener el movimiento
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // TERCERO: Iniciar coroutine para manejar la muerte
        StartCoroutine(HandleDeath());
    }

    System.Collections.IEnumerator HandleDeath()
    {
        // Esperar 2 frames para que el Animator procese el trigger
        yield return null;
        yield return null;
        
        // Verificar qué animación está reproduciéndose
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            Debug.Log($"✅ Animación de muerte reproduciéndose: {clipInfo[0].clip.name}");
            float deathAnimLength = clipInfo[0].clip.length;
            
            // Esperar a que termine la animación
            yield return new WaitForSeconds(deathAnimLength);
            Debug.Log("Animación de muerte terminada");
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró la animación de muerte");
            yield return new WaitForSeconds(1.5f); // Tiempo por defecto
        }
        
        // Desactivar collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Fade out opcional (puedes agregar un material transparente aquí)
        yield return new WaitForSeconds(0.5f);
        
        // Finalmente destruir el objeto
        Debug.Log("Destruyendo dragón");
        Destroy(gameObject);
    }

    // Visualización en el editor
    void OnDrawGizmosSelected()
    {
        // Rango de detección (amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Rango de ataque (rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Rango de persecución (azul)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Rango para perder al jugador (magenta)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);

        // Radio de patrullaje (verde)
        Gizmos.color = Color.green;
        Vector3 startPos = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawWireSphere(startPos, patrolRadius);

        // Punto de patrullaje actual (cyan)
        if (Application.isPlaying && enablePatrol)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentPatrolTarget, 0.5f);
            Gizmos.DrawLine(transform.position, currentPatrolTarget);
        }
    }
}