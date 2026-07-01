using UnityEngine;

public class SpawnerDistanceTrigger : MonoBehaviour
{
    public float activationDistance = 35f;
    private EnemySpawner spawner;
    private Transform playerTransform;

    void Start()
    {
        spawner = GetComponent<EnemySpawner>();
        if (spawner != null)
        {
            spawner.enabled = false; // Start disabled
        }
    }

    void Update()
    {
        if (spawner == null) return;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        // Enable spawner if player is close, disable if far
        if (distance <= activationDistance)
        {
            if (!spawner.enabled)
            {
                spawner.enabled = true;
                Debug.Log($"[SpawnerTrigger] Enabled spawner '{gameObject.name}' - Player is in range ({distance:F1}m)");
            }
        }
        else
        {
            if (spawner.enabled)
            {
                spawner.enabled = false;
                Debug.Log($"[SpawnerTrigger] Disabled spawner '{gameObject.name}' - Player left range ({distance:F1}m)");
            }
        }
    }
}
