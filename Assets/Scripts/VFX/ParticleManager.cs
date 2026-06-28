using UnityEngine;
using System.Collections.Generic;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Pool Settings")]
    public int poolSize = 20;
    public int sfxPoolSize = 8;

    [Header("Effect Prefabs (Assign in Inspector)")]
    public GameObject attackHitEffect;
    public GameObject deathEffect;
    public GameObject healEffect;
    public GameObject upgradeTier2Effect;
    public GameObject upgradeTier3Effect;
    public GameObject upgradeTier4Effect;
    public GameObject consecrationEffect;
    public GameObject arrowTrailEffect;

    [Header("Pool Stats")]
    [SerializeField] private int hitPoolCount = 0;
    [SerializeField] private int deathPoolCount = 0;
    [SerializeField] private int healPoolCount = 0;

    private Dictionary<string, Queue<GameObject>> pools = new();
    private Dictionary<string, GameObject> poolPrefabs = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        CreatePools();
    }

    void CreatePools()
    {
        if (attackHitEffect != null)
            CreatePool("Hit", attackHitEffect, poolSize);
        if (deathEffect != null)
            CreatePool("Death", deathEffect, poolSize);
        if (healEffect != null)
            CreatePool("Heal", healEffect, poolSize * 2);
        if (arrowTrailEffect != null)
            CreatePool("ArrowTrail", arrowTrailEffect, poolSize * 2);
    }

    void CreatePool(string tag, GameObject prefab, int size)
    {
        if (prefab == null) return;

        pools[tag] = new Queue<GameObject>();
        poolPrefabs[tag] = prefab;

        GameObject poolParent = new GameObject($"Pool_{tag}");
        poolParent.transform.SetParent(transform);

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, poolParent.transform);
            obj.SetActive(false);
            pools[tag].Enqueue(obj);
        }
    }

    GameObject GetFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(tag) || pools[tag].Count == 0)
        {
            if (poolPrefabs.ContainsKey(tag) && poolPrefabs[tag] != null)
            {
                GameObject obj = Instantiate(poolPrefabs[tag], position, rotation);
                return obj;
            }
            return null;
        }

        GameObject objToSpawn = pools[tag].Dequeue();
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        IPooledObject pooled = objToSpawn.GetComponent<IPooledObject>();
        pooled?.OnSpawn();

        return objToSpawn;
    }

    void ReturnToPool(string tag, GameObject obj, float delay)
    {
        StartCoroutine(ReturnWithDelay(tag, obj, delay));
    }

    System.Collections.IEnumerator ReturnWithDelay(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj == null) yield break;

        IPooledObject pooled = obj.GetComponent<IPooledObject>();
        pooled?.OnDespawn();

        obj.SetActive(false);

        if (pools.ContainsKey(tag))
        {
            obj.transform.SetParent(transform.Find($"Pool_{tag}"));
            pools[tag].Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    public void PlayHitEffect(Vector3 position)
    {
        if (attackHitEffect == null) return;
        GameObject fx = GetFromPool("Hit", position, Quaternion.identity);
        if (fx != null)
            ReturnToPool("Hit", fx, 2f);
    }

    public void PlayDeathEffect(Vector3 position)
    {
        if (deathEffect == null) return;
        GameObject fx = GetFromPool("Death", position, Quaternion.identity);
        if (fx != null)
            ReturnToPool("Death", fx, 3f);
    }

    public void PlayHealEffect(Transform target)
    {
        if (healEffect == null) return;
        Vector3 pos = target.position + Vector3.up;
        GameObject fx = GetFromPool("Heal", pos, Quaternion.identity);
        if (fx != null)
            ReturnToPool("Heal", fx, 2f);
    }

    public void PlayUpgradeEffect(Transform target, int tier)
    {
        GameObject prefab = tier switch
        {
            2 => upgradeTier2Effect,
            3 => upgradeTier3Effect,
            4 => upgradeTier4Effect,
            _ => upgradeTier2Effect
        };

        if (prefab == null) return;

        Vector3 pos = target.position + Vector3.up * 0.5f;
        GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
        ReturnToPool("", fx, 4f);
    }

    public void PlayConsecrationEffect(Transform target)
    {
        if (consecrationEffect == null) return;
        GameObject fx = Instantiate(consecrationEffect, target.position, Quaternion.identity);
        ReturnToPool("", fx, 5f);
    }

    public void PlayArrowTrail(Vector3 position)
    {
        if (arrowTrailEffect == null) return;
        GameObject fx = GetFromPool("ArrowTrail", position, Quaternion.identity);
        if (fx != null)
            ReturnToPool("ArrowTrail", fx, 0.5f);
    }

    public void PlayBossSpawnEffect(Vector3 position)
    {
        if (upgradeTier3Effect == null) return;
        GameObject fx = Instantiate(upgradeTier3Effect, position, Quaternion.identity);
        ReturnToPool("", fx, 3f);
    }

    public void PlayBossDeathEffect(Vector3 position)
    {
        if (upgradeTier4Effect == null) return;
        GameObject fx = Instantiate(upgradeTier4Effect, position, Quaternion.identity);
        ReturnToPool("", fx, 5f);
    }

    void OnValidate()
    {
        foreach (var kvp in pools)
        {
            if (kvp.Key == "Hit") hitPoolCount = kvp.Value.Count;
            if (kvp.Key == "Death") deathPoolCount = kvp.Value.Count;
            if (kvp.Key == "Heal") healPoolCount = kvp.Value.Count;
        }
    }
}
