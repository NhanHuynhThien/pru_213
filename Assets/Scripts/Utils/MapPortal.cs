using UnityEngine;
using UnityEngine.SceneManagement;

public class MapPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    public string targetSceneName = "Rung_Hac_Am";
    public Vector3 spawnOffset = new Vector3(0f, 1f, 3f); // Offset to spawn player when teleporting (optional)
    
    [Header("Visual Effects")]
    public GameObject portalVFXPrefab;
    public float rotationSpeed = 45f;

    private bool isTeleporting = false;

    void Update()
    {
        // Rotate the portal visual to make it look active and dynamic
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;

        if (other.CompareTag("Player"))
        {
            isTeleporting = true;
            Debug.Log($"[MapPortal] Player entered portal. Teleporting to scene: {targetSceneName}");
            
            // Play sound effect
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPickupSound(); // Fallback to pickup sound
            }

            // Load target scene
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(targetSceneName);
            }
            else
            {
                SceneManager.LoadScene(targetSceneName);
            }
        }
    }
}
