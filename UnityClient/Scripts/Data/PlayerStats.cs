using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Player Stats", menuName = "LoaThanh/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Identity")]
    public string playerName = "Cao Thuc";
    public int playerID;

    [Header("Tier & Class")]
    public int currentTier = 1;
    public string tierName = "Gap Cham";
    public string className = "Nghệ Nhân";

    [Header("Base Stats")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpForce = 8f;
    public float attackDamage = 15f;
    public float attackSpeed = 1f;
    public float criticalChance = 0.05f;
    public float criticalMultiplier = 1.5f;
    public float defense = 5f;
    public float stamina = 100f;
    public float maxStamina = 100f;
    public float staminaRegen = 10f;

    [Header("Resources")]
    public int copperCount = 0;
    public int tinCount = 0;
    public int bronzeIngot = 0;
    public int turtleShell = 0;

    [Header("Tier Bonuses")]
    public float tierHealthBonus = 0f;
    public float tierDamageBonus = 0f;
    public float tierDefenseBonus = 0f;

    [Header("Tier 4 - No Than Special")]
    public bool canSummonArrows = false;
    public int summonedArrowCount = 0;
    public float consecrationProgress = 0f;

    public int EffectiveMaxHealth => maxHealth + Mathf.RoundToInt(tierHealthBonus);
    public int EffectiveAttackDamage => Mathf.RoundToInt(attackDamage * (1f + tierDamageBonus));
    public float EffectiveDefense => defense * (1f + tierDefenseBonus);

    public void Reset()
    {
        currentHealth = EffectiveMaxHealth;
        stamina = maxStamina;
    }

    public void ApplyTierBonuses(int tier)
    {
        currentTier = tier;
        switch (tier)
        {
            case 1:
                tierName = "Giáp Chàm";
                tierHealthBonus = 0f;
                tierDamageBonus = 0f;
                tierDefenseBonus = 0f;
                break;
            case 2:
                tierName = "Giáp Đồng";
                tierHealthBonus = 50f;
                tierDamageBonus = 0.15f;
                tierDefenseBonus = 0.1f;
                break;
            case 3:
                tierName = "Giáp Linh Quy";
                tierHealthBonus = 120f;
                tierDamageBonus = 0.3f;
                tierDefenseBonus = 0.25f;
                canSummonArrows = true;
                break;
            case 4:
                tierName = "Thần Vương";
                tierHealthBonus = 250f;
                tierDamageBonus = 0.5f;
                tierDefenseBonus = 0.4f;
                canSummonArrows = true;
                break;
        }
        currentHealth = EffectiveMaxHealth;
    }

    public PlayerStats Clone()
    {
        PlayerStats clone = CreateInstance<PlayerStats>();
        clone.playerName = this.playerName;
        clone.playerID = this.playerID;
        clone.currentTier = this.currentTier;
        clone.tierName = this.tierName;
        clone.className = this.className;
        clone.maxHealth = this.maxHealth;
        clone.currentHealth = this.currentHealth;
        clone.moveSpeed = this.moveSpeed;
        clone.sprintSpeed = this.sprintSpeed;
        clone.jumpForce = this.jumpForce;
        clone.attackDamage = this.attackDamage;
        clone.attackSpeed = this.attackSpeed;
        clone.criticalChance = this.criticalChance;
        clone.criticalMultiplier = this.criticalMultiplier;
        clone.defense = this.defense;
        clone.stamina = this.stamina;
        clone.maxStamina = this.maxStamina;
        clone.staminaRegen = this.staminaRegen;
        clone.copperCount = this.copperCount;
        clone.tinCount = this.tinCount;
        clone.bronzeIngot = this.bronzeIngot;
        clone.turtleShell = this.turtleShell;
        clone.tierHealthBonus = this.tierHealthBonus;
        clone.tierDamageBonus = this.tierDamageBonus;
        clone.tierDefenseBonus = this.tierDefenseBonus;
        clone.canSummonArrows = this.canSummonArrows;
        clone.summonedArrowCount = this.summonedArrowCount;
        clone.consecrationProgress = this.consecrationProgress;
        return clone;
    }
}
