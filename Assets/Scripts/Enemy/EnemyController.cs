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
    public float sightRange = 4f;
    public float chaseRange = 6f;
    public float attackRange = 2f;

    private NavMeshAgent navAgent;
    private bool isDead = false;
    private Vector3 spawnPosition;
    private Vector3 directPatrolPoint;
    private float directPatrolTimer = 0f;
    private float targetSearchTimer = 0f;

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
        if (data != null)
        {
            currentHealth = data.GetScaledHealth(currentTier);
            currentDamage = data.GetScaledDamage(currentTier);
            currentSpeed = data.moveSpeed;
        }
        else
        {
            currentHealth = 80f;
            currentDamage = 8f;
            currentSpeed = 3f;
        }

        if (navAgent != null)
        {
            navAgent.speed = currentSpeed;
            navAgent.stoppingDistance = attackRange * 0.8f;
        }

        if (data != null)
        {
            sightRange = data.sightRange;
            chaseRange = data.chaseRange;
            attackRange = data.attackRange;
        }

        FindPlayerTarget();
        ChangeState(EnemyState.Patrol);
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

        targetSearchTimer -= Time.deltaTime;
        if (target == null || targetSearchTimer <= 0f)
        {
            FindPlayerTarget();
            targetSearchTimer = 0.5f;
        }

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
            float dist = GetDistanceToTarget(target.position);
            if (dist <= chaseRange && currentState != EnemyState.Attack && currentState != EnemyState.Chase)
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
        ChangeState(EnemyState.Patrol);
    }

    void UpdatePatrol()
    {
        if (!CanUseNavAgent())
        {
            DirectPatrol();
            return;
        }

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
        if (target == null) return;

        if (CanUseNavAgent())
        {
            navAgent.SetDestination(target.position);
            navAgent.speed = currentSpeed * 1.2f;
        }
        else
        {
            MoveDirectlyToTarget();
        }

        float dist = GetDistanceToTarget(target.position);
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

        if (CanUseNavAgent())
        {
            navAgent.SetDestination(target.position);
        }
        else
        {
            LookAtTarget();
        }

        float dist = GetDistanceToTarget(target.position);
        if (dist > attackRange * 1.5f)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (attackTimer <= 0f)
        {
            PerformAttack();
            attackTimer = data != null ? data.attackCooldown : 1.5f;
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
        if (pc == null) pc = target.GetComponentInParent<PlayerCombat>();
        if (pc == null) pc = target.GetComponentInChildren<PlayerCombat>();
        if (pc != null)
        {
            pc.TakeDamage(currentDamage);
        }
        else
        {
            Debug.LogWarning("[Enemy] Khong tim thay PlayerCombat tren Player nen khong tru HP duoc.");
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Debug.Log($"[Enemy] {(data != null ? data.enemyName : name)} tan cong gay {currentDamage} damage!");
    }

    void ChangeState(EnemyState newState)
    {
        if (isDead) return;
        currentState = newState;

        switch (newState)
        {
            case EnemyState.Idle:
                if (CanUseNavAgent()) navAgent.isStopped = true;
                if (animator != null) animator.SetFloat("Speed", 0f);
                break;
            case EnemyState.Patrol:
                if (CanUseNavAgent())
                {
                    navAgent.isStopped = false;
                    navAgent.speed = currentSpeed * 0.5f;
                    SetRandomPatrolPoint();
                }
                break;
            case EnemyState.Chase:
                if (CanUseNavAgent())
                {
                    navAgent.isStopped = false;
                    navAgent.speed = currentSpeed * 1.2f;
                }
                if (animator != null) animator.SetFloat("Speed", 1f);
                break;
            case EnemyState.Attack:
                if (CanUseNavAgent()) navAgent.isStopped = true;
                if (animator != null) animator.SetFloat("Speed", 0f);
                LookAtTarget();
                break;
            case EnemyState.Stunned:
                if (CanUseNavAgent()) navAgent.isStopped = true;
                break;
        }
    }

    void SetRandomPatrolPoint()
    {
        if (!CanUseNavAgent())
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            directPatrolPoint = patrolCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);
            directPatrolPoint.y = transform.position.y;
            return;
        }

        Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
        randomDir += patrolCenter;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            nextPatrolPoint = hit.position;
            navAgent.SetDestination(nextPatrolPoint);
        }
    }

    private bool CanUseNavAgent()
    {
        return navAgent != null && navAgent.enabled && navAgent.isOnNavMesh;
    }

    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }

        PlayerCombat playerCombat = FindFirstObjectByType<PlayerCombat>();
        if (playerCombat != null)
        {
            target = playerCombat.transform;
        }
    }

    private void DirectPatrol()
    {
        directPatrolTimer -= Time.deltaTime;
        Vector3 toPoint = directPatrolPoint - transform.position;
        toPoint.y = 0f;

        if (directPatrolTimer <= 0f || toPoint.sqrMagnitude < 0.4f)
        {
            SetRandomPatrolPoint();
            directPatrolTimer = patrolWaitTime;
            return;
        }

        float speed = Mathf.Max(1f, currentSpeed * 0.45f);
        transform.position += toPoint.normalized * speed * Time.deltaTime;

        if (toPoint.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(toPoint.normalized),
                6f * Time.deltaTime
            );
        }

        if (animator != null) animator.SetFloat("Speed", 0.5f);
    }

    private void MoveDirectlyToTarget()
    {
        if (target == null) return;

        Vector3 targetPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude <= 0.001f) return;

        float speed = Mathf.Max(1f, currentSpeed * 1.2f);
        transform.position += direction.normalized * speed * Time.deltaTime;
        LookAtTarget();
    }

    private void LookAtTarget()
    {
        if (target == null) return;

        Vector3 lookPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = lookPosition - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    private float GetDistanceToTarget(Vector3 targetPos)
    {
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;
        return toTarget.magnitude;
    }

    public void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;

        float actualDamage = Mathf.Max(1f, damage - (data != null ? data.defense : 0f));
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

        if (data != null && data.deathEffect != null)
        {
            Instantiate(data.deathEffect, transform.position, Quaternion.identity);
        }

        if (target != null && data != null)
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
