using UnityEngine;
using System.Collections.Generic;

public class SkinManager : MonoBehaviour
{
    public List<SkinBase> availableSkins;
    public Transform characterRoot;
    public ParticleManager particleManager;

    public int currentTier = 1;
    private GameObject _currentModel;
    private ArmorVFX _armorVFX;

    void Start()
    {
        ApplySkinByTier(currentTier);
    }

    public void ApplySkinByTier(int tier)
    {
        tier = Mathf.Clamp(tier, 1, 4);
        currentTier = tier;

        SkinBase skinData = availableSkins?.Find(s => s.Tier == tier);

        if (_currentModel != null)
        {
            Destroy(_currentModel);
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

            Debug.Log($"[SkinManager] Đã trang bị: Tier {tier} - {skinData.SkinName}");
        }
        else
        {
            Debug.LogWarning($"[SkinManager] Không tìm thấy Skin cho Tier {tier}");
        }

        UpdatePlayerStats(tier, skinData);
    }

    void UpdatePlayerStats(int tier, SkinBase skin)
    {
        PlayerController pc = GetComponent<PlayerController>();
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

            PlayerCombat combat = GetComponent<PlayerCombat>();
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
