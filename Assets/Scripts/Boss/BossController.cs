using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class BossController : MonoBehaviour, IDamageable
{
    [Header("Boss Data")]
    public BossData data;
    public int requiredTier = 1;

    [Header("State")]
    public BossState currentState = BossState.Spawning;
    public enum BossState { Spawning, Idle, Chasing, Attacking, PhaseTransition, Casting, Stunned, Dead }

    [Header("References")]
    public Transform target;
    public Animator animator;
    public CharacterController charController;

    [Header("Stats")]
    public float currentHealth;
    public float maxHealth;
    public int currentPhase = 1;
    private float phaseThreshold = 0.5f;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float chaseSpeed = 4f;
    public float attackRange = 3f;
    public float detectionRange = 18f; // Tam phat hien Player de duoi theo.
    private Vector3 velocity;
    public float gravity = -15f;

    [Header("Combat")]
    public float attackDamage = 25f;
    public float attackCooldown = 2f;
    public float attackTimer = 0f;

    [Header("Abilities")]
    public float specialCooldown = 8f;
    public float specialTimer = 0f;
    public float dashSpeed = 12f;
    public float aoeRadius = 5f;
    public float aoeDamage = 40f;
    private bool isDashing = false;
    private Vector3 dashDirection;

    [Header("Phase Transition")]
    public float transitionDuration = 2f;
    private bool isTransitioning = false;

    private bool isDead = false;
    public bool IsDead => isDead;
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;
    private float animTimer = 0f;
    private float targetSearchTimer = 0f;

    public event System.Action<BossController> OnBossDeath;
    public event System.Action<int> OnPhaseChanged;

    void Awake()
    {
        if (charController == null)
            charController = GetComponent<CharacterController>();
    }

    void Start()
    {
        // Tắt Animator tạm thời nếu chưa gán Animator Controller trong Unity để tránh spam log lag máy
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            animator = null;
        }

        if (data != null)
        {
            maxHealth = data.GetScaledHealth(requiredTier);
            currentHealth = maxHealth;
            moveSpeed = data.moveSpeed;
            chaseSpeed = data.moveSpeed * 1.3f;
            attackDamage = data.attackDamage;
            attackRange = data.attackRange;
            attackCooldown = data.attackCooldown;
            dashSpeed = data.dashSpeed;
            aoeRadius = data.aoeRadius;
            aoeDamage = data.aoeDamage;
        }
        else
        {
            maxHealth = 200f; // Mặc định 200 HP để chém 5-6 phát mới chết
            currentHealth = maxHealth;
            moveSpeed = 2.5f;
            chaseSpeed = 3.5f;
            attackDamage = 15f;
        }

        FindPlayerTarget();

        StartCoroutine(SpawnSequence());
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

    void Update()
    {
        if (isDead) return;

        targetSearchTimer -= Time.deltaTime;
        if (target == null || targetSearchTimer <= 0f)
        {
            FindPlayerTarget();
            targetSearchTimer = 0.5f;
        }

        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f) isInvincible = false;
        }

        if (isTransitioning) return;

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (specialTimer > 0f) specialTimer -= Time.deltaTime;

        switch (currentState)
        {
            case BossState.Spawning:
                animTimer += Time.deltaTime;
                break;
            case BossState.Idle:
                UpdateIdle();
                break;
            case BossState.Chasing:
                UpdateChase();
                break;
            case BossState.Attacking:
                UpdateAttack();
                break;
            case BossState.Casting:
                break;
            case BossState.Stunned:
                break;
        }

        bool canSwitchCombatState =
            currentState != BossState.Spawning &&
            currentState != BossState.Dead &&
            currentState != BossState.Casting &&
            currentState != BossState.Stunned &&
            currentState != BossState.PhaseTransition;

        if (canSwitchCombatState && target != null)
        {
            float dist = GetDistanceToTarget(target.position);
            if (dist <= attackRange && attackTimer <= 0f)
            {
                ChangeState(BossState.Attacking);
            }
            else if (currentState == BossState.Chasing)
            {
                // Nếu đang đuổi theo mà người chơi chạy quá xa (1.5 lần tầm phát hiện), quay lại Idle
                if (dist > detectionRange * 1.5f && currentState != BossState.Casting && currentState != BossState.Stunned)
                {
                    ChangeState(BossState.Idle);
                }
            }
            else if (currentState == BossState.Idle)
            {
                // Chỉ bắt đầu đuổi theo khi người chơi đi vào phạm vi phát hiện (detectionRange)
                if (dist <= detectionRange && dist > attackRange)
                {
                    ChangeState(BossState.Chasing);
                }
            }
        }

        // Áp dụng trọng lực liên tục cho Boss ở mọi trạng thái hoạt động (tránh lỗi lơ lửng trên không)
        if (currentState != BossState.Dead && charController != null)
        {
            if (!charController.isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
            }
            else
            {
                velocity.y = -2f;
            }
            charController.Move(velocity * Time.deltaTime);
        }
    }

    IEnumerator SpawnSequence()
    {
        if (animator != null) animator.SetTrigger("Spawn");
        if (data?.spawnEffect != null)
            Instantiate(data.spawnEffect, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(2f);
        ChangeState(BossState.Idle);
    }

    void UpdateIdle()
    {
        if (animator != null) animator.SetFloat("Speed", 0f);

        // Tầm phát hiện quái được xử lý tập trung trong hàm Update() ở trên
    }

    void UpdateChase()
    {
        if (target == null || charController == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0f;

        charController.Move(dir * chaseSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);

        if (animator != null) animator.SetFloat("Speed", 1f);

        // Tuyệt chiêu chỉ kích hoạt khi Boss đang đuổi theo chiến đấu với người chơi
        if (specialTimer <= 0f)
        {
            TrySpecialAbility();
        }
    }

    void UpdateAttack()
    {
        if (animator != null) animator.SetFloat("Speed", 0f);

        if (target == null)
        {
            ChangeState(BossState.Idle);
            return;
        }

        Vector3 lookPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = lookPosition - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction.normalized),
                8f * Time.deltaTime
            );
        }

        float dist = GetDistanceToTarget(target.position);
        if (dist > attackRange * 1.25f)
        {
            ChangeState(BossState.Chasing);
            return;
        }

        if (attackTimer <= 0f)
        {
            PerformAttack();
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
            pc.TakeDamage(attackDamage);
        }
        else
        {
            Debug.LogWarning("[Boss] Khong tim thay PlayerCombat tren Player nen khong tru HP duoc.");
        }

        if (animator != null) animator.SetTrigger("Attack");
        attackTimer = attackCooldown;
        ChangeState(BossState.Chasing);
    }

    void TrySpecialAbility()
    {
        int roll = Random.Range(0, 3);

        switch (roll)
        {
            case 0:
                StartCoroutine(DashAttack());
                break;
            case 1:
                StartCoroutine(AOEAttack());
                break;
            case 2:
                StartCoroutine(SummonMinions());
                break;
        }

        specialTimer = data?.specialAbilityCooldown ?? 8f;
    }

    IEnumerator DashAttack()
    {
        ChangeState(BossState.Casting);
        if (animator != null) animator.SetTrigger("Special");

        yield return new WaitForSeconds(0.5f);

        if (target != null)
        {
            dashDirection = (target.position - transform.position).normalized;
            dashDirection.y = 0f;
            isDashing = true;
        }

        float dashDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < dashDuration && isDashing)
        {
            if (charController != null)
                charController.Move(dashDirection * dashSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        isDashing = false;

        if (target != null)
        {
            float dist = GetDistanceToTarget(target.position);
            if (dist < 2f)
            {
                PlayerCombat pc = target.GetComponent<PlayerCombat>();
                if (pc == null) pc = target.GetComponentInParent<PlayerCombat>();
                if (pc == null) pc = target.GetComponentInChildren<PlayerCombat>();
                pc?.TakeDamage(attackDamage * 1.5f);
            }
        }

        yield return new WaitForSeconds(0.3f);
        ChangeState(BossState.Idle);
    }

    IEnumerator AOEAttack()
    {
        ChangeState(BossState.Casting);
        if (animator != null) animator.SetTrigger("Special");

        yield return new WaitForSeconds(0.8f);

        if (data?.phaseTransitionEffect != null)
            Instantiate(data.phaseTransitionEffect, transform.position, Quaternion.identity);

        if (target != null)
        {
            float dist = GetDistanceToTarget(target.position);
            if (dist <= aoeRadius)
            {
                PlayerCombat pc = target.GetComponent<PlayerCombat>();
                if (pc == null) pc = target.GetComponentInParent<PlayerCombat>();
                if (pc == null) pc = target.GetComponentInChildren<PlayerCombat>();
                pc?.TakeDamage(aoeDamage);
            }
        }
        yield return new WaitForSeconds(0.5f);
        ChangeState(BossState.Idle);
    }

    IEnumerator SummonMinions()
    {
        ChangeState(BossState.Casting);
        if (animator != null) animator.SetTrigger("Special");

        yield return new WaitForSeconds(0.5f);

        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 3f;
                offset.y = 0f;
                Vector3 spawnPos = transform.position + offset;

                if (spawner.enemyPrefabs.Count > 0)
                {
                    EnemyData minionData = spawner.enemyPrefabs[Random.Range(0, spawner.enemyPrefabs.Count)];
                    GameObject minionObj = Instantiate(minionData.modelPrefab, spawnPos, Quaternion.identity);
                    EnemyController minion = minionObj.GetComponent<EnemyController>();
                    if (minion != null)
                    {
                        minion.data = minionData;
                        minion.currentTier = requiredTier;
                        minion.ApplyTierScaling();
                        if (target != null) minion.target = target;
                    }
                    spawner.activeEnemiesList.Add(minion);
                }
            }
        }
        yield return new WaitForSeconds(0.5f);
        ChangeState(BossState.Idle);
    }

    void ChangeState(BossState newState)
    {
        if (isDead) return;
        currentState = newState;
    }

    private float GetDistanceToTarget(Vector3 targetPos)
    {
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;
        return toTarget.magnitude;
    }

    public void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead || isInvincible) return;

        float actualDamage = Mathf.Max(1f, damage - (data?.defense ?? 0f));
        currentHealth -= actualDamage;

        // Cập nhật thanh máu Boss lên giao diện UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBossHealth(data != null ? data.bossName : "Wolf Boss", GetHealthPercent());
        }

        DamagePopup.Create(transform.position + Vector3.up * 2f, (int)actualDamage, isCritical);

        if (animator != null) animator.SetTrigger("Hurt");

        CheckPhaseTransition();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void CheckPhaseTransition()
    {
        float healthPercent = currentHealth / maxHealth;

        if (data != null && data.totalPhases >= 2)
        {
            for (int i = 0; i < data.phaseHealthThresholds.Length; i++)
            {
                if (healthPercent <= data.phaseHealthThresholds[i] && currentPhase < i + 2)
                {
                    StartCoroutine(PhaseTransition(i + 2));
                    break;
                }
            }
        }
    }

    IEnumerator PhaseTransition(int newPhase)
    {
        isTransitioning = true;
        currentPhase = newPhase;
        isInvincible = true;
        invincibilityTimer = transitionDuration;

        ChangeState(BossState.PhaseTransition);

        if (animator != null) animator.SetTrigger("PhaseChange");

        if (data?.phaseTransitionEffect != null)
            Instantiate(data.phaseTransitionEffect, transform.position, Quaternion.identity);

        Debug.Log($"[Boss] Phase {newPhase}!");

        OnPhaseChanged?.Invoke(newPhase);

        yield return new WaitForSeconds(transitionDuration);

        isTransitioning = false;
        ChangeState(BossState.Idle);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Ẩn thanh máu Boss khi Boss chết
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideBossHealth();
        }

        ChangeState(BossState.Dead);

        if (animator != null) animator.SetTrigger("Death");

        if (data?.deathEffect != null)
            Instantiate(data.deathEffect, transform.position, Quaternion.identity);

        Debug.Log($"[Boss] {data?.bossName} đã bị đánh bại!");

        OnBossDeath?.Invoke(this);

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(2f);

        charController?.Move(Vector3.down * 2f * Time.deltaTime);
        yield return new WaitForSeconds(1f);

        Destroy(gameObject);
    }

    public float GetHealthPercent() => maxHealth > 0 ? currentHealth / maxHealth : 0f;
}
