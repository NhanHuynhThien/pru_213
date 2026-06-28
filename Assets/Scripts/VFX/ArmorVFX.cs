using UnityEngine;

public class ArmorVFX : MonoBehaviour
{
    [Header("Tier VFX Settings")]
    public int currentTier = 1;

    [Header("Tier 1 - Giáp Chàm")]
    public Color tier1Color = new Color(0.5f, 0.35f, 0.2f);
    public float tier1Emission = 0f;

    [Header("Tier 2 - Giáp Đồng")]
    public Color tier2Color = new Color(0.72f, 0.45f, 0.2f);
    public float tier2Emission = 0.3f;
    public float tier2GlowPulse = 0.5f;

    [Header("Tier 3 - Giáp Linh Quy")]
    public Color tier3Color = new Color(0.2f, 0.6f, 0.4f);
    public float tier3Emission = 0.6f;
    public Color tier3Glow = new Color(0.3f, 1f, 0.6f, 0.8f);
    public float tier3PulseSpeed = 2f;

    [Header("Tier 4 - Thần Vương")]
    public Color tier4Color = new Color(1f, 0.85f, 0.2f);
    public float tier4Emission = 1.2f;
    public Color tier4Glow = new Color(1f, 0.9f, 0.3f, 1f);
    public float tier4SpiralSpeed = 3f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private float elapsedTime = 0f;
    private int currentAppliedTier = 0;

    [Header("Particle Systems")]
    public ParticleSystem tier2Particles;
    public ParticleSystem tier3Particles;
    public ParticleSystem tier4Particles;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        ApplyTierVFX(currentTier);
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        switch (currentTier)
        {
            case 2:
                UpdateTier2VFX();
                break;
            case 3:
                UpdateTier3VFX();
                break;
            case 4:
                UpdateTier4VFX();
                break;
        }
    }

    public void ApplyTierVFX(int tier)
    {
        currentTier = tier;
        currentAppliedTier = tier;

        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(propBlock);

            switch (tier)
            {
                case 1:
                    propBlock.SetColor("_Color", tier1Color);
                    propBlock.SetColor("_EmissionColor", tier1Color * tier1Emission);
                    propBlock.SetFloat("_Glossiness", 0.3f);
                    break;
                case 2:
                    propBlock.SetColor("_Color", tier2Color);
                    propBlock.SetColor("_EmissionColor", tier2Color * tier2Emission);
                    propBlock.SetFloat("_Glossiness", 0.6f);
                    break;
                case 3:
                    propBlock.SetColor("_Color", tier3Color);
                    propBlock.SetColor("_EmissionColor", tier3Glow * tier3Emission);
                    propBlock.SetFloat("_Glossiness", 0.7f);
                    break;
                case 4:
                    propBlock.SetColor("_Color", tier4Color);
                    propBlock.SetColor("_EmissionColor", tier4Glow * tier4Emission);
                    propBlock.SetFloat("_Glossiness", 0.9f);
                    propBlock.SetFloat("_Metallic", 1f);
                    break;
            }

            renderer.SetPropertyBlock(propBlock);
        }

        if (tier2Particles != null) tier2Particles.gameObject.SetActive(tier >= 2);
        if (tier3Particles != null) tier3Particles.gameObject.SetActive(tier >= 3);
        if (tier4Particles != null) tier4Particles.gameObject.SetActive(tier >= 4);

        Debug.Log($"[ArmorVFX] Applied Tier {tier} visual effects.");
    }

    void UpdateTier2VFX()
    {
        float pulse = (Mathf.Sin(elapsedTime * tier2GlowPulse * Mathf.PI) + 1f) * 0.5f;

        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", tier2Color * (tier2Emission + pulse * 0.2f));
            renderer.SetPropertyBlock(propBlock);
        }
    }

    void UpdateTier3VFX()
    {
        float pulse = (Mathf.Sin(elapsedTime * tier3PulseSpeed) + 1f) * 0.5f;

        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", tier3Glow * (tier3Emission + pulse * 0.4f));
            renderer.SetPropertyBlock(propBlock);
        }
    }

    void UpdateTier4VFX()
    {
        float spiral = (Mathf.Sin(elapsedTime * tier4SpiralSpeed) + 1f) * 0.5f;

        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", tier4Glow * (tier4Emission + spiral * 0.3f));
            renderer.SetPropertyBlock(propBlock);
        }
    }

    public void TriggerUpgradeAnimation()
    {
        StartCoroutine(UpgradeAnimationCoroutine());
    }

    System.Collections.IEnumerator UpgradeAnimationCoroutine()
    {
        ParticleManager.Instance?.PlayUpgradeEffect(transform, currentTier);
        ParticleManager.Instance?.PlayConsecrationEffect(transform);

        float duration = 1.5f;
        float elapsed = 0f;
        float intensity = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            intensity = Mathf.Lerp(0f, 3f, t) * (t < 0.5f ? 1f : (1f - t) * 2f);

            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_EmissionColor", Color.white * intensity);
                renderer.SetPropertyBlock(propBlock);
            }

            yield return null;
        }

        ApplyTierVFX(currentTier);
    }
}
