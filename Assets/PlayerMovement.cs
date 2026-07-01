using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _movementSpeed = 4f; // Giảm tốc độ đi bộ để dễ control hơn
    [SerializeField] private float _runMultiplier = 1.5f; // Tốc độ chạy = 6f
    [SerializeField] private float _gravity = -15f;
    [SerializeField] private float _jumpHeight = 2f;

    [Header("Combat & Weapon Settings")]
    [SerializeField] private GameObject _swordPrefab; // Prefab thanh kiếm để gắn vào tay
    [SerializeField] private float _attackDamage = 24f;
    [SerializeField] private float _attackRange = 2.7f;
    [SerializeField] private float _attackRadius = 1.6f;
    [SerializeField] private float _attackCooldown = 0.8f;
    [SerializeField] private Vector3 _swordOffset = new Vector3(0.03f, 0.102f, 0.062f); // Tinh chỉnh vị trí kiếm trong tay
    [SerializeField] private Vector3 _swordRotation = new Vector3(15.362f, -277.364f, -215.845f); // Tinh chỉnh góc xoay kiếm trong tay
    [SerializeField] private Vector3 _swordScale = new Vector3(0.2f, 0.2f, 0.2f); // Tinh chỉnh scale kiếm trong tay

    private CharacterController _characterController;
    private Animator _animator;
    private Vector3 _velocity;
    
    private GameObject _equippedSword;
    private bool _hasSword = false;
    private bool _isAttacking = false;
    private float _attackCooldownTimer = 0f;
    private bool _wasUIOpenLastFrame = false;

    public bool HasSword => _hasSword;
    public string EquippedSwordName => _equippedSword != null ? _equippedSword.name.Replace("(Clone)", "").Trim() : "Không có";

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        EquipWeapon();
    }

    private void Update()
    {
        // Giảm thời gian chờ giữa các đòn đánh
        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -= Time.deltaTime;
        }

        // Ground check
        if (_characterController.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Snaps the character to the ground
        }

        // Kiểm tra xem chuột có đang tương tác với UI hoặc UI đang mở không
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        bool isUIOpen = (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) ||
                        (BlacksmithUI.Instance != null && BlacksmithUI.Instance.IsOpen) ||
                        (UIManager.Instance != null && (
                            (UIManager.Instance.pausePanel != null && UIManager.Instance.pausePanel.activeSelf) ||
                            (UIManager.Instance.upgradePanel != null && UIManager.Instance.upgradePanel.activeSelf) ||
                            (UIManager.Instance.gameOverPanel != null && UIManager.Instance.gameOverPanel.activeSelf) ||
                            (UIManager.Instance.victoryPanel != null && UIManager.Instance.victoryPanel.activeSelf) ||
                            (UIManager.Instance.characterStatsPanel != null && UIManager.Instance.characterStatsPanel.activeSelf)
                        ));

        bool blockAttack = isPointerOverUI || isUIOpen || _wasUIOpenLastFrame;
        _wasUIOpenLastFrame = isUIOpen;

        // Đòn đánh (Chuột trái hoặc phím F)
        if (!blockAttack && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F)) && !_isAttacking && _attackCooldownTimer <= 0f)
        {
            if (_hasSword)
            {
                StartCoroutine(PerformAttack());
            }
            else
            {
                Debug.Log("<color=orange>[Hệ thống]</color> Bạn cần nhặt Kiếm trước khi chiến đấu!");
            }
        }

        // Chỉ cho phép di chuyển khi không đang thực hiện đòn đánh (hoặc di chuyển bình thường)
        HandleMovementAndRotation();

        // Jump logic
        if (Input.GetButtonDown("Jump") && _characterController.isGrounded && !_isAttacking)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        // Apply gravity over time
        if (!_characterController.isGrounded)
        {
            _velocity.y += _gravity * Time.deltaTime;
            if (_velocity.y < -25f) _velocity.y = -25f; // Giới hạn vận tốc rơi để không bị xuyên tường
        }
        // _characterController.Move(_velocity * Time.deltaTime);
    }

    private void HandleMovementAndRotation()
    {
        Vector2 input = ReadMovementInput();
        float x = input.x;
        float z = input.y;

        // Calculate direction relative to camera facing direction
        Vector3 camForward = Vector3.forward;
        Vector3 camRight = Vector3.right;
        if (Camera.main != null)
        {
            camForward = Camera.main.transform.forward;
            camRight = Camera.main.transform.right;
            camForward.y = 0f; // Giữ hướng di chuyển trên mặt đất phẳng
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
        }

        Vector3 move = camRight * x + camForward * z;
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        // Xoay nhân vật mượt mà theo hướng di chuyển (chỉ xoay khi có di chuyển và không đang đánh)
        if (move.magnitude > 0.1f && !_isAttacking)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 12f * Time.deltaTime);
        }

        // Apply running speed multiplier if Left Shift is held
        float currentSpeed = _movementSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= _runMultiplier;
        }

        // Move the player (horizontal) - giảm tốc độ di chuyển khi đang vung kiếm
        float speedMultiplier = _isAttacking ? 0.2f : 1f;
        
        // Gộp cả di chuyển ngang (move) và di chuyển dọc (_velocity) vào một lệnh Move duy nhất để tránh bị kẹt tường
        Vector3 finalMove = (move * currentSpeed * speedMultiplier) + _velocity;
        _characterController.Move(finalMove * Time.deltaTime);

        // Cập nhật hoạt ảnh di chuyển cho Animator
        if (_animator != null)
        {
            float moveInput = new Vector2(x, z).magnitude;
            bool isRunning = moveInput > 0.1f && Input.GetKey(KeyCode.LeftShift);
            
            // Set speed = 1 khi có di chuyển (để play anim Walk/Run chung)
            float targetSpeed = moveInput > 0.1f ? 1.0f : 0.0f;
            _animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);
            
            // Tăng tốc độ phát anim khi chạy để phân biệt với đi bộ
            _animator.speed = isRunning ? 1.5f : 1.0f;
        }
    }

    private Vector2 ReadMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        float keyX = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyX -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyX += 1f;

        float keyZ = 0f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyZ -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyZ += 1f;

        if (Mathf.Abs(keyX) > 0.01f) x = keyX;
        if (Mathf.Abs(keyZ) > 0.01f) z = keyZ;

        Vector2 input = new Vector2(x, z);
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    private IEnumerator PerformAttack()
    {
        _isAttacking = true;
        _attackCooldownTimer = _attackCooldown;

        // Kích hoạt hoạt ảnh chém
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }

        // Chờ đến thời điểm vung kiếm chém xuống (khoảng 0.25 giây trong hoạt ảnh) để gây sát thương
        yield return new WaitForSeconds(0.25f);

        // Tìm kiếm các đối tượng bị chém trúng trong hình cầu phía trước nhân vật
        Vector3 attackPosition = transform.position + transform.forward * _attackRange * 0.5f + Vector3.up * 1.0f;
        Collider[] hitColliders = Physics.OverlapSphere(attackPosition, _attackRadius);
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        foreach (var col in hitColliders)
        {
            // Bỏ qua chính bản thân người chơi
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            // Gây sát thương lên Boss hoặc Enemy có component IDamageable
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                if (damagedTargets.Contains(damageable)) continue;

                try
                {
                    bool isCritical = Random.value < 0.2f; // 20% chí mạng
                    float damage = isCritical ? _attackDamage * 1.5f : _attackDamage;

                    damageable.TakeDamage(damage, isCritical);
                    damagedTargets.Add(damageable);
                    Debug.Log($"<color=red>[Chiến đấu]</color> Đã chém trúng <b>{col.name}</b> gây {damage} sát thương!");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Lỗi Chiến Đấu] Không thể gây sát thương lên {col.name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (enemy != null && IsTargetInsideAttackCone(enemy.transform) && !damagedTargets.Contains(enemy))
            {
                DamageKnownTarget(enemy, enemy.name, damagedTargets);
            }
        }

        foreach (BossController boss in FindObjectsByType<BossController>(FindObjectsSortMode.None))
        {
            if (boss != null && IsTargetInsideAttackCone(boss.transform) && !damagedTargets.Contains(boss))
            {
                DamageKnownTarget(boss, boss.name, damagedTargets);
            }
        }

        // Chờ phần còn lại của hoạt ảnh kết thúc
        yield return new WaitForSeconds(_attackCooldown - 0.25f);
        _isAttacking = false;
    }

    private bool IsTargetInsideAttackCone(Transform target)
    {
        if (target == null || target == transform || target.IsChildOf(transform)) return false;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > _attackRange * _attackRange) return false;
        if (toTarget.sqrMagnitude < 0.05f) return true;

        return Vector3.Angle(transform.forward, toTarget.normalized) <= 100f;
    }

    private void DamageKnownTarget(IDamageable target, string targetName, HashSet<IDamageable> damagedTargets)
    {
        if (target == null || damagedTargets.Contains(target)) return;

        bool isCritical = Random.value < 0.2f;
        float damage = isCritical ? _attackDamage * 1.5f : _attackDamage;
        target.TakeDamage(damage, isCritical);
        damagedTargets.Add(target);

        Debug.Log($"[PlayerMovement] Chem trung {targetName}, gay {damage} damage.");
    }

    // Nhận diện va chạm để nhặt vật phẩm
    private void OnTriggerEnter(Collider other)
    {
        LootItem loot = other.GetComponentInParent<LootItem>(); // Tìm component LootItem ở đối tượng va chạm hoặc các đối tượng cha của nó
        if (loot != null)
        {
            // Kiểm tra xem vật phẩm đã hết thời gian chờ để nẩy và ổn định trên đất chưa
            if (!loot.CanPickup()) return;
            
            // Hiển thị chữ nổi màu vàng "Nhặt: [Tên Vật Phẩm]!" trên đầu người chơi
            DamagePopup.Create(transform.position + Vector3.up * 1.8f, $"Nhặt: {loot.itemName}!", new Color(1f, 0.85f, 0f));
            
            Debug.Log($"<color=gold>[Loot]</color> Bạn đã nhặt thành công vật phẩm: <b>{loot.itemName}</b>!");
            
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null && pc.stats != null)
            {
                string nameLower = loot.itemName.ToLower();
                if (nameLower.Contains("thiếc") || nameLower.Contains("ruda") || nameLower.Contains("tin"))
                {
                    pc.stats.tinCount += 1;
                    Debug.Log($"<color=gold>[Hệ thống]</color> +1 Thiếc! Tổng số Thiếc hiện có: {pc.stats.tinCount}");
                }
                else if (nameLower.Contains("đồng thau") || nameLower.Contains("ingot") || nameLower.Contains("thỏi") || nameLower.Contains("thoi"))
                {
                    pc.stats.bronzeIngot += 1;
                    Debug.Log($"<color=gold>[Hệ thống]</color> +1 Thỏi Đồng Thau! Tổng số hiện có: {pc.stats.bronzeIngot}");
                }
                else if (nameLower.Contains("linh khí") || nameLower.Contains("linh khi") || nameLower.Contains("stone") || nameLower.Contains("crystal"))
                {
                    pc.stats.spiritualStone += 1;
                    Debug.Log($"<color=gold>[Hệ thống]</color> +1 Đá Linh Khí! Tổng số hiện có: {pc.stats.spiritualStone}");
                }
                else if (nameLower.Contains("đồng") || nameLower.Contains("metal") || nameLower.Contains("copper") || nameLower.Contains("ore"))
                {
                    pc.stats.copperCount += 5;
                    Debug.Log($"<color=gold>[Hệ thống]</color> +5 Đồng! Tổng số Đồng hiện có: {pc.stats.copperCount}");
                }
                else if (nameLower.Contains("mai") || nameLower.Contains("shell") || nameLower.Contains("rùa"))
                {
                    pc.stats.turtleShell += 1;
                    Debug.Log($"<color=gold>[Hệ thống]</color> +1 Mai Linh Quy! Tổng số hiện có: {pc.stats.turtleShell}");
                }
                else
                {
                    // Mặc định nếu là vũ khí/kiếm thì trang bị
                    if (loot.isWeapon && loot.weaponPrefab != null)
                    {
                        EquipWeapon(loot.weaponPrefab, loot.equipOffset, loot.equipRotation, loot.equipScale);
                    }
                    else
                    {
                        EquipWeapon();
                    }
                }
            }
            else
            {
                // Fallback nếu thiếu PlayerController
                if (loot.isWeapon && loot.weaponPrefab != null)
                {
                    EquipWeapon(loot.weaponPrefab, loot.equipOffset, loot.equipRotation, loot.equipScale);
                }
                else
                {
                    EquipWeapon();
                }
            }

            // Biến mất khỏi màn hình sau khi nhặt
            Destroy(other.gameObject);
        }
    }

    private void EquipWeapon()
    {
        EquipWeapon(_swordPrefab, _swordOffset, _swordRotation, _swordScale);
    }

    public void EquipWeapon(GameObject weaponPrefabToEquip, Vector3 offset, Vector3 rotation, Vector3 scale)
    {
        _hasSword = true;

        if (weaponPrefabToEquip == null)
        {
            Debug.LogWarning("[PlayerMovement] Chưa gán prefab vũ khí.");
            return;
        }

        // Tìm xương tay phải của nhân vật
        if (_animator != null)
        {
            Transform rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand != null)
            {
                // Nếu đã có kiếm trên tay trước đó, xóa đi
                if (_equippedSword != null)
                {
                    Destroy(_equippedSword);
                }

                // Gắn kiếm vào tay phải
                _equippedSword = Instantiate(weaponPrefabToEquip, rightHand);
                _equippedSword.transform.localPosition = offset;
                _equippedSword.transform.localRotation = Quaternion.Euler(rotation);
                _equippedSword.transform.localScale = scale;
                
                // Vô hiệu hóa Collider của kiếm khi cầm trên tay để tránh va chạm vật lý lỗi
                Collider swordCol = _equippedSword.GetComponent<Collider>();
                if (swordCol != null)
                {
                    swordCol.enabled = false;
                }
                
                // Vô hiệu hóa script LootItem của kiếm được sinh ra trên tay
                LootItem swordLoot = _equippedSword.GetComponent<LootItem>();
                if (swordLoot != null)
                {
                    swordLoot.enabled = false;
                }

                Debug.Log($"<color=green>[Vũ khí]</color> Đã trang bị {weaponPrefabToEquip.name} vào tay phải!");
            }
            else
            {
                Debug.LogError("[PlayerMovement] Không tìm thấy xương tay phải (Right Hand) của nhân vật!");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ hình cầu biểu thị tầm đánh trong Editor
        Gizmos.color = Color.red;
        Vector3 attackPosition = transform.position + transform.forward * _attackRange * 0.5f + Vector3.up * 1.0f;
        Gizmos.DrawWireSphere(attackPosition, _attackRadius);
    }
}
