using UnityEngine;

[CreateAssetMenu(fileName = "New Boss Data", menuName = "LoaThanh/Boss Data")]
public class BossData : ScriptableObject
{
    [Header("Boss Identity")]
    public string bossName = "Trùm U Minh";
    public int bossID;
    public int requiredTier = 1;

    [Header("Combat Stats")]
    public int maxHealth = 500;
    public float moveSpeed = 2.5f;
    public float attackDamage = 25f;
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    public float defense = 10f;

    [Header("Phases")]
    public int totalPhases = 2;
    public float[] phaseHealthThresholds = new float[] { 0.5f };

    [Header("Abilities")]
    public float specialAbilityCooldown = 8f;
    public float dashSpeed = 12f;
    public float aoeRadius = 5f;
    public float aoeDamage = 40f;

    [Header("Story Info")]
    [TextArea(2, 4)]
    public string loreDescription = "Một thực thể tà ác từ lòng đất.";
    public string bossQuote = "Kneel before the darkness!";

    [Header("Visuals")]
    public GameObject modelPrefab;
    public GameObject phaseTransitionEffect;
    public GameObject deathEffect;
    public GameObject spawnEffect;

    public float GetScaledHealth(int gameTier)
    {
        float tierMult = gameTier switch
        {
            1 => 1f,
            2 => 1.8f,
            3 => 2.5f,
            4 => 4f,
            _ => 1f
        };
        return maxHealth * tierMult;
    }
}
