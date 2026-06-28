using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("Boss Data")]
    public BossData[] bossDatas;

    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public Transform arenaCenter;
    public float spawnDelay = 3f;

    [Header("Boss Prefabs (Assign manually for now)")]
    public GameObject[] bossPrefabs;

    private BossController activeBoss;
    private bool bossSpawned = false;
    private int currentTier;

    void Start()
    {
        if (GameManager.Instance != null)
            currentTier = GameManager.Instance.currentBossTier;

        if (GameManager.Instance != null)
        {
            GameManager.OnGameStateChanged += HandleStateChange;
        }
    }

    void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleStateChange;
    }

    void HandleStateChange(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing && !bossSpawned)
        {
            TrySpawnBoss();
        }
    }

    public void TrySpawnBoss()
    {
        if (bossSpawned) return;

        int tier = GameManager.Instance != null ? GameManager.Instance.currentBossTier : currentTier;
        BossData data = null;

        if (bossDatas != null)
        {
            foreach (var d in bossDatas)
            {
                if (d.requiredTier == tier)
                {
                    data = d;
                    break;
                }
            }
        }

        StartCoroutine(SpawnBossSequence(data, tier));
    }

    System.Collections.IEnumerator SpawnBossSequence(BossData data, int tier)
    {
        bossSpawned = true;
        yield return new WaitForSeconds(spawnDelay);

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position :
                          (arenaCenter != null ? arenaCenter.position : transform.position);

        GameObject prefab = null;
        if (bossPrefabs != null && bossPrefabs.Length > 0)
        {
            int idx = Mathf.Clamp(tier - 1, 0, bossPrefabs.Length - 1);
            prefab = bossPrefabs[idx];
        }

        if (prefab != null)
        {
            GameObject bossObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            activeBoss = bossObj.GetComponent<BossController>();

            if (activeBoss != null)
            {
                activeBoss.data = data;
                activeBoss.requiredTier = tier;

                activeBoss.OnBossDeath += HandleBossDefeated;
                activeBoss.OnPhaseChanged += HandlePhaseChange;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBossHealth(data?.bossName ?? "Boss", 1f);
                }

                AudioManager.Instance?.PlayBossSpawnSound();
                ParticleManager.Instance?.PlayBossSpawnEffect(spawnPos);

                Debug.Log($"[BossSpawner] Boss Tier {tier} da xuat hien!");
            }
        }
        else
        {
            Debug.LogWarning("[BossSpawner] Khong co prefab cho Tier " + tier);
            bossSpawned = false;
        }
    }

    void HandleBossDefeated(BossController boss)
    {
        Debug.Log("[BossSpawner] Boss da bi ha!");

        ParticleManager.Instance?.PlayBossDeathEffect(boss.transform.position);
        AudioManager.Instance?.PlayDeathSound();

        if (UIManager.Instance != null)
            UIManager.Instance.HideBossHealth();

        if (GameManager.Instance != null && GameManager.Instance.currentBossTier >= 4)
        {
            GameManager.Instance.Victory();
        }
        else
        {
            GameManager.Instance?.AdvanceTier();
            bossSpawned = false;
        }
    }

    void HandlePhaseChange(int newPhase)
    {
        Debug.Log($"[BossSpawner] Boss chuyen phase {newPhase}!");
    }

    public void ResetSpawner()
    {
        bossSpawned = false;
        if (activeBoss != null)
        {
            activeBoss.OnBossDeath -= HandleBossDefeated;
            activeBoss = null;
        }
    }

    public bool IsBossAlive => activeBoss != null && !activeBoss.isDead;
}
