using UnityEngine;
using System.Collections.Generic;

public class SkinManager : MonoBehaviour
{
    public List<SkinBase> availableSkins;
    public Transform characterRoot;
    public ParticleManager particleManager;

    public int currentTier = 0;
    private GameObject _currentModel;
    private ArmorVFX _armorVFX;

    void Start()
    {
        PlayerController pc = GetComponentInParent<PlayerController>();
        if (pc != null && pc.stats != null)
        {
            currentTier = pc.stats.currentTier;
        }
        ApplySkinByTier(currentTier);
    }

    public void ApplySkinByTier(int tier)
    {
        Debug.Log($"[SkinManager] ApplySkinByTier gọi với tier: {tier}. StackTrace: {System.Environment.StackTrace}");
        tier = Mathf.Clamp(tier, 0, 4);
        currentTier = tier;

        SkinBase skinData = availableSkins?.Find(s => s.Tier == tier);

        if (tier > 0)
        {
            // Xóa sạch tất cả các mô hình con đang có dưới characterRoot (bao gồm cả mô hình mặc định kéo sẵn)
            if (characterRoot != null)
            {
                foreach (Transform child in characterRoot)
                {
                    Destroy(child.gameObject);
                }
            }

            // Tự động tìm và xóa các mô hình mặc định cũ (Root và Mesh) nằm ngoài characterRoot để tránh bị đè 2 nhân vật
            foreach (Transform child in transform)
            {
                if (child != characterRoot)
                {
                    string nameLower = child.name.ToLower();
                    if (nameLower == "root" || nameLower.Contains("node") || nameLower.Contains("mesh") || child.GetComponent<Renderer>() != null || child.GetComponent<SkinnedMeshRenderer>() != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            if (skinData?.ModelPrefab != null)
            {
                _currentModel = Instantiate(skinData.ModelPrefab, characterRoot);
                _currentModel.transform.localPosition = Vector3.zero;
                _currentModel.transform.localRotation = Quaternion.identity;

                _armorVFX = _currentModel.GetComponent<ArmorVFX>();
                if (_armorVFX != null)
                {
                    _armorVFX.ApplyTierVFX(tier);
                }

                if (skinData.equipEffect != null)
                {
                    Instantiate(skinData.equipEffect, characterRoot.position, Quaternion.identity);
                }

                // Cập nhật lại các tham chiếu Animator của người chơi
                Animator newAnimator = _currentModel.GetComponentInChildren<Animator>();
                if (newAnimator != null)
                {
                    // Đồng bộ Controller từ Animator cha sang Animator mới để tránh bị T-pose/khóa di chuyển
                    Animator parentAnim = GetComponent<Animator>();
                    if (parentAnim != null && parentAnim.runtimeAnimatorController != null)
                    {
                        newAnimator.runtimeAnimatorController = parentAnim.runtimeAnimatorController;
                    }

                    PlayerController pc = GetComponentInParent<PlayerController>();
                    if (pc != null) pc.animator = newAnimator;

                    PlayerCombat combat = GetComponentInParent<PlayerCombat>();
                    if (combat != null) combat.animator = newAnimator;

                    PlayerMovement pm = GetComponentInParent<PlayerMovement>();
                    if (pm != null) pm.SetAnimator(newAnimator);
                }

                Debug.Log($"[SkinManager] Đã trang bị: Tier {tier} - {skinData.SkinName}");
            }
            else
            {
                Debug.LogWarning($"[SkinManager] Không tìm thấy Skin cho Tier {tier}");
            }
        }
        else
        {
            Debug.Log("[SkinManager] Sử dụng mô hình mặc định (Tier 0) - Căn chỉnh vị trí về tâm Vector3.zero");
            // Đưa mô hình mặc định về tâm (Vector3.zero) để tránh bị lệch vị trí/chui xuống đất do thiết lập lệch trong Editor
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        UpdatePlayerStats(tier, skinData);
    }

    void UpdatePlayerStats(int tier, SkinBase skin)
    {
        PlayerController pc = GetComponentInParent<PlayerController>();
        if (pc != null && pc.stats != null)
        {
            pc.stats.ApplyTierBonuses(tier);

            if (skin != null)
            {
                pc.stats.maxHealth += skin.BonusHealth;
                pc.stats.moveSpeed += skin.BonusSpeed;
                pc.stats.attackDamage += Mathf.RoundToInt(skin.BonusDamage);
                pc.stats.defense += skin.BonusDefense;
            }

            PlayerCombat combat = GetComponentInParent<PlayerCombat>();
            if (combat != null)
            {
                combat.currentHealth = pc.stats.EffectiveMaxHealth;
            }
        }
    }

    public void UpgradeToNextTier()
    {
        if (currentTier < 4)
        {
            int nextTier = currentTier + 1;

            if (_armorVFX != null)
            {
                _armorVFX.currentTier = nextTier;
                _armorVFX.TriggerUpgradeAnimation();
            }

            ApplySkinByTier(nextTier);
        }
        else
        {
            Debug.Log("[SkinManager] Đã đạt Tier tối đa - Thần Vương!");
        }
    }

    public SkinBase GetCurrentSkin()
    {
        return availableSkins?.Find(s => s.Tier == currentTier);
    }

    public SkinBase GetSkinByTier(int tier)
    {
        return availableSkins?.Find(s => s.Tier == tier);
    }
}
