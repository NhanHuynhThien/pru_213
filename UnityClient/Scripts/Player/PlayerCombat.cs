using UnityEngine;

public class PlayerCombat : MonoBehaviour, IDamageable
{
    [Header("References")]
    public PlayerStats stats;
    public PlayerController controller;
    public Animator animator;

    [Header("Combat State")]
    public int currentHealth;
    public float invincibilityTime = 0.5f;
    private float invincibilityTimer = 0f;
    private bool isInvincible = false;
    private bool isDead = false;

    public event System.Action OnHealthChanged;
    public event System.Action OnDeath;

    void Awake()
    {
        if (stats == null)
            stats = ScriptableObject.CreateInstance<PlayerStats>();

        if (controller == null)
            controller = GetComponent<PlayerController>();

        currentHealth = stats.EffectiveMaxHealth;
    }

    void Start()
    {
        currentHealth = stats.EffectiveMaxHealth;
    }

    void Update()
    {
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
            }
        }

        if (stats != null && currentHealth < stats.EffectiveMaxHealth)
        {
            stats.stamina += stats.staminaRegen * Time.deltaTime;
            stats.stamina = Mathf.Min(stats.stamina, stats.maxStamina);
        }
    }

    public void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead || isInvincible) return;

        int dmgVal = Mathf.RoundToInt(damage);
        float actualDamage = Mathf.Max(1f, dmgVal - stats.EffectiveDefense);
        currentHealth -= Mathf.RoundToInt(actualDamage);

        invincibilityTimer = invincibilityTime;
        isInvincible = true;

        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }

        DamagePopup.Create(transform.position + Vector3.up, (int)actualDamage, isCritical);

        OnHealthChanged?.Invoke();

        if (currentHealth <= 0f)
        {
            Die();
        }

        Debug.Log($"[Player Combat] Took {actualDamage} damage. HP: {currentHealth}/{stats.EffectiveMaxHealth}");
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + Mathf.RoundToInt(amount), stats.EffectiveMaxHealth);
        OnHealthChanged?.Invoke();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        Debug.Log("[Player] Đã ngã xuống!");
        OnDeath?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    public void Revive()
    {
        isDead = false;
        currentHealth = stats.EffectiveMaxHealth;
        isInvincible = true;
        invincibilityTimer = 2f;

        if (animator != null)
        {
            animator.SetTrigger("Revive");
        }

        OnHealthChanged?.Invoke();
    }

    public float GetHealthPercent()
    {
        return (float)currentHealth / stats.EffectiveMaxHealth;
    }

    public bool IsAlive => !isDead;
    public bool IsInvincible => isInvincible;
}
