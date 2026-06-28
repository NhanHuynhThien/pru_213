using UnityEngine;

public class StatusEffect : MonoBehaviour
{
    public enum EffectType { Poison, Burn, Stun, Slow, Shield, Buff_Attack, Buff_Speed }

    [Header("Effect Data")]
    public EffectType type;
    public float duration = 3f;
    public float tickInterval = 1f;
    public float tickDamage = 5f;
    public float slowPercent = 0.5f;
    public float buffMultiplier = 1.2f;
    public float shieldAmount = 20f;
    public Color effectColor = Color.green;

    private float timer = 0f;
    private float tickTimer = 0f;
    private IDamageable target;
    private bool isActive = false;
    private float originalSpeed;
    private float originalDamage;

    public event System.Action<StatusEffect> OnEffectEnd;

    public static StatusEffect Apply(GameObject target, EffectType type, float duration, float damage = 0f)
    {
        StatusEffect effect = target.AddComponent<StatusEffect>();
        effect.type = type;
        effect.duration = duration;
        effect.tickDamage = damage;
        effect.target = target.GetComponent<IDamageable>();
        effect.Launch();
        return effect;
    }

    void Launch()
    {
        isActive = true;
        timer = 0f;
        tickTimer = 0f;

        switch (type)
        {
            case EffectType.Slow:
                var controller = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (controller != null)
                {
                    originalSpeed = controller.speed;
                    controller.speed *= slowPercent;
                }
                break;
            case EffectType.Shield:
                break;
            case EffectType.Buff_Attack:
            case EffectType.Buff_Speed:
                break;
        }

        ApplyVisual();
    }

    void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;

        if (timer >= duration)
        {
            End();
            return;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            ApplyTick();
            tickTimer = 0f;
        }
    }

    void ApplyTick()
    {
        switch (type)
        {
            case EffectType.Poison:
            case EffectType.Burn:
                target?.TakeDamage(tickDamage);
                break;
        }
    }

    void ApplyVisual()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                switch (type)
                {
                    case EffectType.Poison:
                        mat.SetColor("_EmissionColor", Color.green * 0.5f);
                        mat.EnableKeyword("_EMISSION");
                        break;
                    case EffectType.Burn:
                        mat.SetColor("_EmissionColor", Color.red * 0.8f);
                        mat.EnableKeyword("_EMISSION");
                        break;
                }
            }
        }
    }

    void ClearVisual()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    void End()
    {
        isActive = false;

        switch (type)
        {
            case EffectType.Slow:
                var controller = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (controller != null) controller.speed = originalSpeed;
                break;
        }

        ClearVisual();
        OnEffectEnd?.Invoke(this);
        Destroy(this);
    }
}
