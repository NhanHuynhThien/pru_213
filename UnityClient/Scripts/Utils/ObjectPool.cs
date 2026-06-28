using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size = 10;
    }

    public List<Pool> pools = new();
    private Dictionary<string, Queue<GameObject>> poolDictionary = new();
    private Dictionary<string, Pool> poolData = new();

    void Awake()
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
        foreach (Pool pool in pools)
        {
            CreatePool(pool);
        }
    }

    void CreatePool(Pool pool)
    {
        if (pool.prefab == null)
        {
            Debug.LogWarning($"[ObjectPool] Pool '{pool.tag}' has no prefab assigned!");
            return;
        }

        poolDictionary[pool.tag] = new Queue<GameObject>();
        poolData[pool.tag] = pool;

        for (int i = 0; i < pool.size; i++)
        {
            GameObject obj = Instantiate(pool.prefab, transform);
            obj.SetActive(false);
            poolDictionary[pool.tag].Enqueue(obj);
        }

        Debug.Log($"[ObjectPool] Created pool '{pool.tag}' with {pool.size} objects.");
    }

    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPool] Pool '{tag}' khong ton tai!");
            return null;
        }

        if (poolDictionary[tag].Count == 0)
        {
            Pool pool = poolData[tag];
            if (pool.prefab != null)
            {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                poolDictionary[tag].Enqueue(obj);
            }
            else
            {
                return null;
            }
        }

        GameObject objToSpawn = poolDictionary[tag].Dequeue();
        objToSpawn.SetActive(true);
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;

        IPooledObject pooled = objToSpawn.GetComponent<IPooledObject>();
        if (pooled != null)
            pooled.OnSpawn();
        else
            Debug.LogWarning($"[ObjectPool] Object in pool '{tag}' does not implement IPooledObject!");

        return objToSpawn;
    }

    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation, float lifetime)
    {
        GameObject obj = Spawn(tag, position, rotation);
        if (obj != null)
        {
            StartCoroutine(AutoDespawn(tag, obj, lifetime));
        }
        return obj;
    }

    System.Collections.IEnumerator AutoDespawn(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null && obj.activeInHierarchy)
        {
            Despawn(tag, obj);
        }
    }

    public void Despawn(string tag, GameObject obj)
    {
        if (obj == null) return;

        IPooledObject pooled = obj.GetComponent<IPooledObject>();
        if (pooled != null)
            pooled.OnDespawn();

        obj.SetActive(false);

        if (poolDictionary.ContainsKey(tag))
        {
            obj.transform.SetParent(transform);
            poolDictionary[tag].Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    public void Prewarm(string tag, int additionalCount)
    {
        if (!poolDictionary.ContainsKey(tag) || poolData[tag].prefab == null) return;

        for (int i = 0; i < additionalCount; i++)
        {
            GameObject obj = Instantiate(poolData[tag].prefab, transform);
            obj.SetActive(false);
            poolDictionary[tag].Enqueue(obj);
        }
    }

    public int GetAvailableCount(string tag)
    {
        return poolDictionary.ContainsKey(tag) ? poolDictionary[tag].Count : 0;
    }
}

public interface IPooledObject
{
    void OnSpawn();
    void OnDespawn();
}
