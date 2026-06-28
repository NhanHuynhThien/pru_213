using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "LoaThanh/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "U Minh Quỷ";
    public int enemyID;
    public EnemyType type = EnemyType.Basic;

    public enum EnemyType { Basic, Elite, MiniBoss, Boss }

    [Header("Stats")]
    public int maxHealth = 50;
    public float moveSpeed = 3f;
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public float sightRange = 8f;
    public float chaseRange = 12f;
    public float defense = 2f;

    [Header("Tier Scaling (1-4)")]
    public float tier1Multiplier = 1f;
    public float tier2Multiplier = 1.5f;
    public float tier3Multiplier = 2f;
    public float tier4Multiplier = 3f;

    [Header("Rewards")]
    public int copperReward = 5;
    public int expReward = 10;
    public float spawnWeight = 1f;

    [Header("Visuals")]
    public GameObject modelPrefab;
    public GameObject deathEffect;

    public int GetScaledHealth(int tier)
    {
        float mult = tier switch
        {
            1 => tier1Multiplier,
            2 => tier2Multiplier,
            3 => tier3Multiplier,
            4 => tier4Multiplier,
            _ => 1f
        };
        return Mathf.RoundToInt(maxHealth * mult);
    }

    public float GetScaledDamage(int tier)
    {
        float mult = tier switch
        {
            1 => tier1Multiplier,
            2 => tier2Multiplier,
            3 => tier3Multiplier,
            4 => tier4Multiplier,
            _ => 1f
        };
        return attackDamage * mult;
    }
}
