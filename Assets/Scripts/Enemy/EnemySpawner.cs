using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Configuration")]
    public List<EnemyData> enemyPrefabs = new();
    public int currentTier = 1;
    public int maxEnemies = 10;
    public int activeEnemies = 0;
    public float spawnInterval = 3f;
    public float spawnRadius = 15f;

    [Header("Spawn Zones")]
    public Transform[] spawnPoints;
    public Transform playerReference;

    [Header("Wave System")]
    public int currentWave = 1;
    public int enemiesPerWave = 5;
    public int enemiesInWave = 0;
    public float waveCooldown = 5f;
    private float waveTimer = 0f;
    private bool waveActive = false;

    private float spawnTimer = 0f;
    public List<EnemyController> activeEnemiesList = new();
    private int totalKills = 0;

    [Header("Boss Spawn")]
    public bool spawnBossOnWaveComplete = false;
    public BossData bossData;

    void Start()
    {
        if (playerReference == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerReference = player.transform;
        }

        StartWave();
    }

    void Update()
    {
        CleanupDeadEnemies();

        if (activeEnemies >= maxEnemies) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && waveActive)
        {
            SpawnEnemy();
            spawnTimer = 0f;
        }

        if (waveActive && enemiesInWave >= enemiesPerWave && activeEnemies == 0)
        {
            EndWave();
        }
    }

    void CleanupDeadEnemies()
    {
        activeEnemiesList.RemoveAll(e => e == null);
        activeEnemies = activeEnemiesList.Count;
    }

    public void StartWave()
    {
        waveActive = true;
        enemiesInWave = 0;
        enemiesPerWave = 3 + (currentTier * 2) + (currentWave * 1);
        waveTimer = 0f;

        Debug.Log($"[Spawner] Wave {currentWave} started! Enemies: {enemiesPerWave}");
    }

    void EndWave()
    {
        waveActive = false;
        waveTimer += Time.deltaTime;

        if (waveTimer >= waveCooldown)
        {
            totalKills += enemiesPerWave;
            currentWave++;
            StartWave();
        }
    }

    void SpawnEnemy()
    {
        if (enemiesInWave >= enemiesPerWave) return;
        if (enemyPrefabs.Count == 0) return;

        EnemyData selected = SelectEnemyByWeight();

        Vector3 spawnPos = GetSpawnPosition();

        if (selected.modelPrefab == null)
        {
            Debug.LogWarning($"[Spawner] EnemyData '{selected.enemyName}' has no modelPrefab assigned!");
            return;
        }

        GameObject enemyObj = Instantiate(selected.modelPrefab, spawnPos, Quaternion.identity);
        EnemyController enemy = enemyObj.GetComponent<EnemyController>();

        if (enemy != null)
        {
            enemy.data = selected;
            enemy.currentTier = currentTier;
            enemy.ApplyTierScaling();

            if (playerReference != null)
                enemy.target = playerReference;

            activeEnemiesList.Add(enemy);
            activeEnemies = activeEnemiesList.Count;
            enemiesInWave++;

            enemy.OnEnemyDeath += HandleEnemyDeath;
        }
    }

    EnemyData SelectEnemyByWeight()
    {
        float totalWeight = 0f;
        foreach (var e in enemyPrefabs)
        {
            totalWeight += e.spawnWeight;
        }

        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var e in enemyPrefabs)
        {
            cumulative += e.spawnWeight;
            if (rand <= cumulative) return e;
        }

        return enemyPrefabs[0];
    }

    Vector3 GetSpawnPosition()
    {
        if (playerReference == null)
        {
            return transform.position + Random.insideUnitSphere * spawnRadius;
        }

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPos = playerReference.position + Random.insideUnitSphere * spawnRadius;
            randomPos.y = playerReference.position.y;

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                float distToPlayer = Vector3.Distance(hit.position, playerReference.position);
                if (distToPlayer > 5f && distToPlayer < spawnRadius)
                {
                    return hit.position;
                }
            }
        }

        return playerReference.position + (Random.insideUnitSphere * spawnRadius);
    }

    void HandleEnemyDeath(EnemyController enemy)
    {
        totalKills++;
        enemiesInWave = Mathf.Max(0, enemiesInWave - 1);
    }

    public void SetTier(int tier)
    {
        currentTier = Mathf.Clamp(tier, 1, 4);
        Debug.Log($"[Spawner] Tier updated to {currentTier}");
    }

    public int TotalKills => totalKills;
    public int CurrentWave => currentWave;
}
