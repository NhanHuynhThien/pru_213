using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Data")]
    public EnemyData data;
    public int currentTier = 1;

    [Header("State")]
    public EnemyState currentState = EnemyState.Idle;
    public enum EnemyState { Idle, Patrol, Chase, Attack, Stunned, Dead }

    [Header("References")]
    public Transform target;
    public Animator animator;

    [Header("Stats (Runtime)")]
    public float currentHealth;
    public float currentSpeed;
    public float currentDamage;
    public float attackTimer = 0f;

    [Header("Patrol")]
    public float patrolRadius = 10f;
    public float patrolWaitTime = 3f;
    private Vector3 patrolCenter;
    private Vector3 nextPatrolPoint;
    private float patrolWaitTimer = 0f;

    [Header("Detection")]
    public float sightRange = 8f;
    public float chaseRange = 12f;
    public float attackRange = 2f;

    private NavMeshAgent navAgent;
    private bool isDead = false;
    private Vector3 spawnPosition;

    public event System.Action<EnemyController> OnEnemyDeath;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        spawnPosition = transform.position;
        patrolCenter = spawnPosition;
    }

    void Start()
    {
        ApplyTierScaling();
        currentHealth = data.GetScaledHealth(currentTier);
        currentDamage = data.GetScaledDamage(currentTier);
        currentSpeed = data.moveSpeed;

        if (navAgent != null)
        {
            navAgent.speed = currentSpeed;
            navAgent.stoppingDistance = attackRange * 0.8f;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) target = playerObj.transform;

        ChangeState(EnemyState.Idle);
    }

    public void ApplyTierScaling()
    {
        if (data == null) return;
        currentHealth = data.GetScaledHealth(currentTier);
        currentDamage = data.GetScaledDamage(currentTier);
        currentSpeed = data.moveSpeed;

        if (navAgent != null)
        {
            navAgent.speed = currentSpeed;
        }
    }

    void Update()
    {
        if (isDead) return;

        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Stunned:
                break;
        }

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= sightRange && currentState != EnemyState.Attack && currentState != EnemyState.Chase)
            {
                ChangeState(EnemyState.Chase);
            }
            else if (dist > chaseRange && currentState == EnemyState.Chase)
            {
                ChangeState(EnemyState.Patrol);
            }
        }
    }

    void UpdateIdle()
    {
        if (Random.Range(0f, 1f) < 0.005f)
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    void UpdatePatrol()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;

        if (!navAgent.hasPath || navAgent.remainingDistance < 0.5f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                SetRandomPatrolPoint();
                patrolWaitTimer = 0f;
            }
        }
    }

    void UpdateChase()
    {
        if (target == null || navAgent == null || !navAgent.isOnNavMesh) return;

        navAgent.SetDestination(target.position);
        navAgent.speed = currentSpeed * 1.2f;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackRange)
        {
            ChangeState(EnemyState.Attack);
        }
    }

    void UpdateAttack()
    {
        if (target == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(target.position);
        }

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > attackRange * 1.5f)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (attackTimer <= 0f)
        {
            PerformAttack();
            attackTimer = data.attackCooldown;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    void PerformAttack()
    {
        if (target == null) return;

        PlayerCombat pc = target.GetComponent<PlayerCombat>();
        if (pc != null)
        {
            pc.TakeDamage(currentDamage);
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Debug.Log($"[Enemy] {data.enemyName} tấn công gây {currentDamage} damage!");
    }

    void ChangeState(EnemyState newState)
    {
        if (isDead) return;
        currentState = newState;

        if (navAgent == null) return;

        switch (newState)
        {
            case EnemyState.Idle:
                navAgent.isStopped = true;
                if (animator != null) animator.SetFloat("Speed", 0f);
                break;
            case EnemyState.Patrol:
                navAgent.isStopped = false;
                navAgent.speed = currentSpeed * 0.5f;
                SetRandomPatrolPoint();
                break;
            case EnemyState.Chase:
                navAgent.isStopped = false;
                navAgent.speed = currentSpeed * 1.2f;
                if (animator != null) animator.SetFloat("Speed", 1f);
                break;
            case EnemyState.Attack:
                navAgent.isStopped = true;
                if (animator != null) animator.SetFloat("Speed", 0f);
                transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
                break;
            case EnemyState.Stunned:
                navAgent.isStopped = true;
                break;
        }
    }

    void SetRandomPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
        randomDir += patrolCenter;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            nextPatrolPoint = hit.position;
            navAgent.SetDestination(nextPatrolPoint);
        }
    }

    public void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;

        float actualDamage = Mathf.Max(1f, damage - data.defense);
        currentHealth -= actualDamage;

        DamagePopup.Create(transform.position + Vector3.up, (int)actualDamage, isCritical);

        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        if (data.deathEffect != null)
        {
            Instantiate(data.deathEffect, transform.position, Quaternion.identity);
        }

        if (target != null)
        {
            PlayerStats ps = target.GetComponent<PlayerController>()?.stats;
            if (ps != null)
            {
                ps.copperCount += data.copperReward;
                Debug.Log($"[Enemy] Nhận {data.copperReward} đồng! Tổng: {ps.copperCount}");
            }
        }

        OnEnemyDeath?.Invoke(this);

        StartCoroutine(DeathSequence());
    }

    System.Collections.IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(1.5f);

        if (navAgent != null) navAgent.enabled = false;

        float fadeTime = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            if (gameObject != null)
            {
                transform.Translate(Vector3.down * Time.deltaTime * 0.5f);
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
