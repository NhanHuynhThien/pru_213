using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerStats stats;
    public Transform cameraTransform;
    public CharacterController characterController;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public float maxLookUp = 80f;
    public float maxLookDown = -80f;

    [Header("Combat Settings")]
    public Transform attackPoint;
    public float attackRange = 2f;
    public float attackRadius = 1f;

    private Vector3 velocity;
    private Vector3 moveDirection;
    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;
    private bool isJumping = false;
    private bool isSprinting = false;
    private bool isAttacking = false;
    private bool isGrounded = true;
    private float attackTimer = 0f;
    private float summonCooldown = 0f;
    private const float SUMMON_COOLDOWN = 5f;

    public bool IsMoving { get; private set; }
    public bool IsGrounded => isGrounded;
    public bool IsAttacking => isAttacking;

    void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (stats == null)
            stats = ScriptableObject.CreateInstance<PlayerStats>();
    }

    void Start()
    {
        stats.Reset();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        walkSpeed = stats.moveSpeed;
        sprintSpeed = stats.sprintSpeed;
        jumpHeight = stats.jumpForce * 0.25f;
    }

    void Update()
    {
        GroundCheck();
        HandleMovement();
        HandleJump();
        HandleAttackCooldown();

        if (isAttacking && attackTimer <= 0f)
        {
            isAttacking = false;
        }

        if (Input.GetKeyDown(KeyCode.F) && !isAttacking && attackTimer <= 0f)
        {
            Attack();
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping)
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && isGrounded)
        {
            if (stats != null && stats.stamina > 5f)
                isSprinting = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isSprinting = false;
        }

        if (isSprinting && stats != null)
        {
            stats.stamina -= 10f * Time.deltaTime;
            if (stats.stamina <= 0f)
            {
                stats.stamina = 0f;
                isSprinting = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) && stats != null && stats.canSummonArrows && summonCooldown <= 0f)
        {
            SummonArrows();
        }

        if (summonCooldown > 0f)
            summonCooldown -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.U))
        {
            UpgradeSystem us = UpgradeSystem.Instance;
            if (us != null)
            {
                us.TryStartUpgrade();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    transform.LookAt(new Vector3(hit.point.x, transform.position.y, hit.point.z));
                }
            }
        }
    }

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(
            transform.position + Vector3.down * groundCheckDistance,
            groundCheckDistance,
            groundMask
        );

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            isJumping = false;
        }
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        IsMoving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = (forward * v + right * h) * speed;
        }
        else
        {
            moveDirection = new Vector3(h, 0f, v) * speed;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        if (h != 0f || v != 0f)
        {
            float targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(0f, targetRotation, 0f),
                10f * Time.deltaTime
            );
        }
    }

    void HandleJump()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        characterController.Move(velocity * Time.deltaTime);
    }

    void Jump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        isJumping = true;
        isGrounded = false;
    }

    public void Attack()
    {
        if (isAttacking) return;
        isAttacking = true;
        attackTimer = 1f / stats.attackSpeed;

        if (stats != null)
        {
            Transform ap = attackPoint != null ? attackPoint : transform;

            Collider[] hits = Physics.OverlapSphere(ap.position + ap.forward * attackRange * 0.5f, attackRadius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Enemy") || hit.CompareTag("Boss"))
                {
                    IDamageable dmg = hit.GetComponent<IDamageable>();
                    if (dmg != null)
                    {
                        float dmgVal = stats.EffectiveAttackDamage;
                        bool isCrit = Random.value < stats.criticalChance;
                        if (isCrit) dmgVal *= stats.criticalMultiplier;

                        dmg.TakeDamage(Mathf.RoundToInt(dmgVal), isCrit);

                        if (isCrit)
                        {
                            DamagePopup.Create(hit.transform.position + Vector3.up, (int)dmgVal, true);
                        }
                    }
                }
            }

            StartCoroutine(AttackAnimation());
        }
    }

    System.Collections.IEnumerator AttackAnimation()
    {
        yield return new WaitForSeconds(0.2f);
    }

    void HandleAttackCooldown()
    {
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;
    }

    public void SummonArrows()
    {
        if (stats == null || !stats.canSummonArrows) return;
        if (summonCooldown > 0f) return;

        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null && !combat.IsAlive) return;

        summonCooldown = SUMMON_COOLDOWN;

        int count = 12;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f + dir * 0.5f;
            Projectile proj = Projectile.Spawn(
                spawnPos,
                dir,
                stats.EffectiveAttackDamage * 2f,
                Projectile.ProjectileType.SummonedArrow,
                gameObject
            );
        }

        stats.summonedArrowCount++;
        Debug.Log($"[Player] Trieu hoi {count} mui ten dong!");
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position + attackPoint.forward * attackRange * 0.5f, attackRadius);
        }
    }
}

public interface IDamageable
{
    void TakeDamage(float damage, bool isCritical = false);
}
