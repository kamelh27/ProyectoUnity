using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AdvancedDragonController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Animator animator;
    private NavMeshAgent agent;

    [Header("Configuración de Vida")]
    public int maxHealth = 200;
    private int currentHealth;

    [Header("Configuración de Detección")]
    public float detectionRange = 20f;
    public float attackRange = 6f;
    public float losePlayerRange = 30f;
    public float flyDetectionRange = 25f; // Rango para usar ataques voladores

    [Header("Configuración de Combate")]
    public float attackCooldown = 2.5f;
    private float lastAttackTime = 0f;
    
    [Header("Daño de Ataques")]
    public int basicAttackDamage = 20;
    public int tailAttackDamage = 25;
    public int fireballShootDamage = 30;
    public int screamDamage = 15;
    
    [Header("Ataques Voladores")]
    public int flyFireballShootDamage = 35;
    public int flyForwardDamage = 40;
    public int flyGlideDamage = 30;

    [Header("Configuración de Movimiento")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 6f;
    public float flySpeed = 8f;

    [Header("Sistema de Combos")]
    public bool useComboSystem = true;
    public float comboChance = 0.7f;
    private bool isInCombo = false;

    [Header("Sistema de Vuelo")]
    public bool canFly = true;
    public float flyHeight = 10f;
    public float flyChance = 0.3f; // 30% de probabilidad de volar
    private bool isFlying = false;
    private bool isTakingOff = false;
    private bool isLanding = false;

    [Header("Sistema de Patrullaje")]
    public bool enablePatrol = true;
    public float patrolRadius = 15f;
    public float patrolWaitTime = 4f;
    private Vector3 startPosition;
    private Vector3 currentPatrolTarget;
    private float patrolWaitTimer = 0f;
    private bool isWaiting = false;

    [Header("Sistema de Alerta")]
    public bool screamOnDetection = true;
    private bool hasDetectedPlayer = false;
    private bool isScreaming = false;

    private enum DragonState
    {
        Idle,
        Patrol,
        Walk,
        Run,
        Defend,
        BasicAttack,
        TailAttack,
        FireballShoot,
        Scream,
        Sleep,
        TakeOff,
        FlyFloat,
        FlyFireballShoot,
        FlyForward,
        FlyGlide,
        Land,
        GetHit,
        Die
    }

    private DragonState currentState = DragonState.Idle;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isDefending = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        startPosition = transform.position;

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
            case DragonState.Run:
                HandleChaseState(distanceToPlayer);
                break;

            case DragonState.BasicAttack:
            case DragonState.TailAttack:
            case DragonState.FireballShoot:
                HandleGroundAttackState(distanceToPlayer);
                break;

            case DragonState.FlyFloat:
            case DragonState.FlyFireballShoot:
            case DragonState.FlyForward:
            case DragonState.FlyGlide:
                HandleFlyingState(distanceToPlayer);
                break;

            case DragonState.Defend:
                HandleDefendState(distanceToPlayer);
                break;

            case DragonState.Sleep:
                HandleSleepState(distanceToPlayer);
                break;
        }
    }

    void HandleIdleState(float distance)
    {
        if (distance <= detectionRange)
        {
            if (!hasDetectedPlayer && screamOnDetection)
            {
                StartCoroutine(ScreamAndEngageCombat(distance));
            }
            else
            {
                EngageCombat(distance);
            }
        }
        else if (enablePatrol)
        {
            ChangeState(DragonState.Patrol);
        }
        else if (Random.value > 0.98f) // 2% probabilidad de dormir
        {
            StartCoroutine(SleepForAWhile());
        }
    }

    void HandlePatrolState(float distance)
    {
        if (distance <= detectionRange)
        {
            isWaiting = false;
            
            if (!hasDetectedPlayer && screamOnDetection)
            {
                StartCoroutine(ScreamAndEngageCombat(distance));
                ChangeState(DragonState.Idle);
            }
            else
            {
                EngageCombat(distance);
            }
            return;
        }

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

        if (agent != null && agent.enabled)
        {
            agent.speed = walkSpeed;
            
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isWaiting = true;
                patrolWaitTimer = patrolWaitTime;
                ChangeState(DragonState.Idle);
            }
        }
    }

    void HandleChaseState(float distance)
    {
        if (distance <= attackRange)
        {
            DecideAttackType(distance);
        }
        else if (distance > losePlayerRange)
        {
            Debug.Log("Jugador perdido, volviendo a patrullar");
            hasDetectedPlayer = false;
            
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
            if (agent != null)
            {
                agent.speed = runSpeed;
            }
            MoveTowardsPlayer();
        }
    }

    void HandleGroundAttackState(float distance)
    {
        if (isScreaming || isAttacking) return;
        
        if (!isAttacking && distance <= attackRange)
        {
            if (Time.time > lastAttackTime + attackCooldown)
            {
                // Decidir si defender o atacar
                if (currentHealth < maxHealth * 0.4f && Random.value > 0.7f)
                {
                    StartCoroutine(DefendMomentarily());
                }
                else
                {
                    PerformGroundAttack();
                }
            }
        }
        else if (distance > attackRange)
        {
            ChangeState(DragonState.Run);
        }
    }

    void HandleFlyingState(float distance)
    {
        if (isLanding || isTakingOff) return;

        if (distance > losePlayerRange)
        {
            StartCoroutine(LandAndPatrol());
        }
        else if (distance <= flyDetectionRange)
        {
            if (!isAttacking && Time.time > lastAttackTime + attackCooldown)
            {
                PerformFlyingAttack();
            }
        }
    }

    void HandleDefendState(float distance)
    {
        if (!isDefending)
        {
            if (distance <= attackRange)
            {
                DecideAttackType(distance);
            }
            else
            {
                ChangeState(DragonState.Run);
            }
        }
    }

    void HandleSleepState(float distance)
    {
        if (distance <= detectionRange)
        {
            // Despertarse si el jugador se acerca
            animator.ResetTrigger("Sleep");
            ChangeState(DragonState.Idle);
            StartCoroutine(ScreamAndEngageCombat(distance));
        }
    }

    void EngageCombat(float distance)
    {
        // Decidir si volar o atacar en tierra
        if (canFly && Random.value < flyChance && distance > attackRange)
        {
            StartCoroutine(TakeOffAndFly());
        }
        else if (distance <= attackRange)
        {
            DecideAttackType(distance);
        }
        else
        {
            ChangeState(DragonState.Run);
        }
    }

    void DecideAttackType(float distance)
    {
        if (canFly && Random.value < flyChance * 0.5f)
        {
            StartCoroutine(TakeOffAndFly());
        }
        else
        {
            ChangeState(DragonState.BasicAttack);
        }
    }

    System.Collections.IEnumerator ScreamAndEngageCombat(float distance)
    {
        hasDetectedPlayer = true;
        isScreaming = true;
        
        Debug.Log("¡DRAGÓN AVANZADO DETECTÓ AL JUGADOR! - Gritando de alerta");
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        
        if (agent != null)
        {
            agent.isStopped = true;
        }
        
        animator.SetTrigger("Scream");
        
        yield return new WaitForSeconds(2.0f);
        DealScreamDamage();
        
        yield return new WaitForSeconds(1.0f);
        
        isScreaming = false;
        EngageCombat(distance);
    }

    System.Collections.IEnumerator DefendMomentarily()
    {
        isDefending = true;
        ChangeState(DragonState.Defend);
        
        if (agent != null)
        {
            agent.isStopped = true;
        }
        
        animator.SetTrigger("Defend");
        Debug.Log("Dragón se defiende");
        
        yield return new WaitForSeconds(2f);
        
        isDefending = false;
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    System.Collections.IEnumerator SleepForAWhile()
    {
        ChangeState(DragonState.Sleep);
        animator.SetTrigger("Sleep");
        Debug.Log("Dragón durmiendo...");
        
        yield return new WaitForSeconds(Random.Range(10f, 20f));
        
        if (currentState == DragonState.Sleep)
        {
            ChangeState(DragonState.Idle);
        }
    }

    System.Collections.IEnumerator TakeOffAndFly()
    {
        isTakingOff = true;
        isFlying = true;
        ChangeState(DragonState.TakeOff);
        
        if (agent != null)
        {
            agent.enabled = false;
        }
        
        animator.SetTrigger("Take Off");
        Debug.Log("Dragón despegando...");
        
        // Animación de despegue
        float takeOffTime = 2f;
        float timer = 0;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * flyHeight;
        
        while (timer < takeOffTime)
        {
            timer += Time.deltaTime;
            float progress = timer / takeOffTime;
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }
        
        isTakingOff = false;
        ChangeState(DragonState.FlyFloat);
    }

    System.Collections.IEnumerator LandAndPatrol()
    {
        isLanding = true;
        ChangeState(DragonState.Land);
        
        animator.SetTrigger("Land");
        Debug.Log("Dragón aterrizando...");
        
        // Animación de aterrizaje
        float landTime = 2f;
        float timer = 0;
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(startPos.x, startPosition.y, startPos.z);
        
        while (timer < landTime)
        {
            timer += Time.deltaTime;
            float progress = timer / landTime;
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }
        
        isFlying = false;
        isLanding = false;
        
        if (agent != null)
        {
            agent.enabled = true;
        }
        
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

    void PerformGroundAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (agent != null)
        {
            agent.isStopped = true;
        }

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));

        if (useComboSystem && !isInCombo && Random.value < comboChance)
        {
            isInCombo = true;
            StartCoroutine(ExecuteGroundCombo());
        }
        else
        {
            ExecuteSingleGroundAttack();
        }
    }

    void PerformFlyingAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        StartCoroutine(ExecuteFlyingAttack());
    }

    System.Collections.IEnumerator ExecuteGroundCombo()
    {
        Debug.Log("¡Dragon inicia COMBO en tierra!");
        
        int comboPattern = Random.Range(0, 3);
        
        switch (comboPattern)
        {
            case 0: // Basic → Tail → Fireball
                yield return StartCoroutine(GroundComboPattern1());
                break;
                
            case 1: // Scream → Basic → Tail
                yield return StartCoroutine(GroundComboPattern2());
                break;
                
            case 2: // Fireball → Basic → Fireball
                yield return StartCoroutine(GroundComboPattern3());
                break;
        }
        
        isInCombo = false;
        yield return new WaitForSeconds(0.5f);
        ResetAttack();
        
        Debug.Log("Combo en tierra terminado!");
    }

    System.Collections.IEnumerator GroundComboPattern1()
    {
        animator.SetTrigger("Basic Attack");
        yield return new WaitForSeconds(0.5f);
        DealBasicDamage();
        yield return new WaitForSeconds(1.0f);
        
        if (!CheckPlayerInRange()) yield break;
        
        animator.SetTrigger("Tail Attack");
        yield return new WaitForSeconds(0.6f);
        DealTailDamage();
        yield return new WaitForSeconds(1.0f);
        
        if (!CheckPlayerInRange()) yield break;
        
        animator.SetTrigger("Fireball Shoot");
        yield return new WaitForSeconds(0.8f);
        DealFireballDamage();
        yield return new WaitForSeconds(1.0f);
    }

    System.Collections.IEnumerator GroundComboPattern2()
    {
        animator.SetTrigger("Scream");
        yield return new WaitForSeconds(1.0f);
        DealScreamDamage();
        yield return new WaitForSeconds(1.5f);
        
        if (!CheckPlayerInRange()) yield break;
        
        animator.SetTrigger("Basic Attack");
        yield return new WaitForSeconds(0.5f);
        DealBasicDamage();
        yield return new WaitForSeconds(1.0f);
        
        if (!CheckPlayerInRange()) yield break;
        
        animator.SetTrigger("Tail Attack");
        yield return new WaitForSeconds(0.6f);
        DealTailDamage();
        yield return new WaitForSeconds(1.0f);
    }

    System.Collections.IEnumerator GroundComboPattern3()
    {
        animator.SetTrigger("Fireball Shoot");
        yield return new WaitForSeconds(0.8f);
        DealFireballDamage();
        yield return new WaitForSeconds(1.2f);
        
        if (!CheckPlayerInRange()) yield break;
        
        animator.SetTrigger("Basic Attack");
        yield return new WaitForSeconds(0.5f);
        DealBasicDamage();
        yield return new WaitForSeconds(1.0f);
        
        if (!CheckPlayerInRange()) yield break;
        
        animator.SetTrigger("Fireball Shoot");
        yield return new WaitForSeconds(0.8f);
        DealFireballDamage();
        yield return new WaitForSeconds(1.0f);
    }

    System.Collections.IEnumerator ExecuteFlyingAttack()
    {
        Debug.Log("¡Dragon ataca desde el aire!");
        
        int attackType = Random.Range(0, 3);
        
        switch (attackType)
        {
            case 0: // Fly Fireball Shoot
                ChangeState(DragonState.FlyFireballShoot);
                animator.SetTrigger("Fly Fireball Shoot");
                yield return new WaitForSeconds(1.0f);
                DealFlyFireballDamage();
                yield return new WaitForSeconds(1.5f);
                break;
                
            case 1: // Fly Forward (ataque de embestida)
                ChangeState(DragonState.FlyForward);
                animator.SetTrigger("Fly Forward");
                yield return StartCoroutine(FlyForwardAttack());
                break;
                
            case 2: // Fly Glide (planeo amenazante)
                ChangeState(DragonState.FlyGlide);
                animator.SetTrigger("Fly Glide");
                yield return new WaitForSeconds(2.0f);
                DealFlyGlideDamage();
                yield return new WaitForSeconds(1.0f);
                break;
        }
        
        ChangeState(DragonState.FlyFloat);
        isAttacking = false;
    }

    System.Collections.IEnumerator FlyForwardAttack()
    {
        Vector3 startPos = transform.position;
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 targetPos = startPos + direction * 15f;
        
        float duration = 1.5f;
        float timer = 0;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            
            // Verificar colisión con jugador
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer <= attackRange * 2f)
            {
                DealFlyForwardDamage();
                break;
            }
            
            yield return null;
        }
        
        yield return new WaitForSeconds(0.5f);
    }

    void ExecuteSingleGroundAttack()
    {
        int attackType = Random.Range(0, 3);
        
        switch (attackType)
        {
            case 0:
                animator.SetTrigger("Basic Attack");
                Invoke("DealBasicDamage", 0.5f);
                break;
            case 1:
                animator.SetTrigger("Tail Attack");
                Invoke("DealTailDamage", 0.6f);
                break;
            case 2:
                animator.SetTrigger("Fireball Shoot");
                Invoke("DealFireballDamage", 0.8f);
                break;
        }

        Invoke("ResetAttack", 2.0f);
    }

    // Métodos de daño
    void DealBasicDamage()
    {
        DealDamageToPlayer(basicAttackDamage, false);
    }

    void DealTailDamage()
    {
        DealDamageToPlayer(tailAttackDamage, false);
    }

    void DealFireballDamage()
    {
        DealDamageToPlayer(fireballShootDamage, false);
    }

    void DealScreamDamage()
    {
        float screamRange = attackRange * 2f;
        DealDamageToPlayer(screamDamage, false, screamRange);
    }

    void DealFlyFireballDamage()
    {
        float range = flyDetectionRange;
        DealDamageToPlayer(flyFireballShootDamage, false, range);
    }

    void DealFlyForwardDamage()
    {
        DealDamageToPlayer(flyForwardDamage, true);
    }

    void DealFlyGlideDamage()
    {
        float range = attackRange * 1.5f;
        DealDamageToPlayer(flyGlideDamage, false, range);
    }

    void DealDamageToPlayer(int damage, bool isHornLikeAttack, float range = -1)
    {
        if (player == null) return;
        
        float actualRange = range > 0 ? range : attackRange;
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= actualRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, isHornLikeAttack, transform.position);
                Debug.Log($"Dragón causó {damage} de daño al jugador!");
            }
        }
    }

    void MoveTowardsPlayer()
    {
        if (agent != null && agent.enabled)
        {
            agent.SetDestination(player.position);
        }
        else if (isFlying)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * flySpeed * Time.deltaTime;
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        }
    }

    bool CheckPlayerInRange()
    {
        if (player == null || isDead) return false;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance > attackRange * 2f)
        {
            Debug.Log("Jugador muy lejos, interrumpiendo combo");
            return false;
        }
        
        return true;
    }

    void ResetAttack()
    {
        isAttacking = false;
        if (agent != null && !isDead && !isFlying)
        {
            agent.isStopped = false;
        }
    }

    void ChangeState(DragonState newState)
    {
        if (currentState == newState || isDead) return;

        currentState = newState;

        animator.ResetTrigger("Basic Attack");
        animator.ResetTrigger("Tail Attack");
        animator.ResetTrigger("Fireball Shoot");
        animator.ResetTrigger("Get Hit");
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Die");
        animator.ResetTrigger("Run");
        animator.ResetTrigger("Scream");
        animator.ResetTrigger("Defend");
        animator.ResetTrigger("Sleep");
        animator.ResetTrigger("Take Off");
        animator.ResetTrigger("Fly Float");
        animator.ResetTrigger("Fly Fireball Shoot");
        animator.ResetTrigger("Fly Forward");
        animator.ResetTrigger("Fly Glide");
        animator.ResetTrigger("Land");

        switch (newState)
        {
            case DragonState.Idle:
                if (agent != null && !isFlying) agent.isStopped = true;
                break;
            case DragonState.Patrol:
            case DragonState.Walk:
                animator.SetTrigger("Walk");
                if (agent != null && !isFlying)
                {
                    agent.speed = walkSpeed;
                    agent.isStopped = false;
                }
                break;
            case DragonState.Run:
                animator.SetTrigger("Run");
                if (agent != null && !isFlying)
                {
                    agent.speed = runSpeed;
                    agent.isStopped = false;
                }
                break;
            case DragonState.FlyFloat:
                animator.SetTrigger("Fly Float");
                break;
        }
    }

    void SetNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;
        randomDirection.y = startPosition.y;

        if (agent != null)
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                currentPatrolTarget = hit.position;
                agent.SetDestination(currentPatrolTarget);
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
        Debug.Log($"Dragon avanzado recibió {damage} de daño. Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            if (!isAttacking && !isFlying)
            {
                animator.SetTrigger("Get Hit");
            }
        }
    }

    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentState = DragonState.Die;
        
        Debug.Log("=== DRAGON AVANZADO MURIENDO ===");
        
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
        
        animator.ResetTrigger("Basic Attack");
        animator.ResetTrigger("Tail Attack");
        animator.ResetTrigger("Fireball Shoot");
        animator.ResetTrigger("Get Hit");
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Run");
        animator.ResetTrigger("Scream");
        animator.ResetTrigger("Take Off");
        animator.ResetTrigger("Land");
        
        animator.SetTrigger("Die");
        Debug.Log("Trigger 'Die' activado");
        
        StartCoroutine(HandleDeath());
    }

    System.Collections.IEnumerator HandleDeath()
    {
        yield return null;
        yield return null;
        
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            Debug.Log($"✅ Animación de muerte reproduciéndose: {clipInfo[0].clip.name}");
            float deathAnimLength = clipInfo[0].clip.length;
            
            yield return new WaitForSeconds(deathAnimLength);
            Debug.Log("Animación de muerte terminada");
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró la animación de muerte");
            yield return new WaitForSeconds(2f);
        }
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("Destruyendo dragón avanzado");
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, flyDetectionRange);

        Gizmos.color = Color.green;
        Vector3 startPos = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawWireSphere(startPos, patrolRadius);

        if (Application.isPlaying && enablePatrol)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentPatrolTarget, 0.5f);
            Gizmos.DrawLine(transform.position, currentPatrolTarget);
        }
    }
}
