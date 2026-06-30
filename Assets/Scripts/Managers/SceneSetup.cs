using UnityEngine;

public class SceneSetup : MonoBehaviour
{
    [Header("Managers (Auto-assign)")]
    public GameObject gameManagerPrefab;
    public GameObject audioManagerPrefab;
    public GameObject particleManagerPrefab;
    public GameObject networkManagerPrefab;
    public GameObject loginManagerPrefab;

    [Header("Scene References")]
    public GameObject playerPrefab;
    public GameObject spawnerPrefab;
    public GameObject bossSpawnerPrefab;
    public Transform playerSpawnPoint;

    [Header("Environment")]
    public Light directionalLight;
    public Material skyboxMaterial;

    void Awake()
    {
        SetupManagers();
        SetupLighting();
    }

    void Start()
    {
        SetupPlayer();
        SetupSpawners();
    }

    void SetupManagers()
    {
        if (GameManager.Instance == null && gameManagerPrefab != null)
        {
            Instantiate(gameManagerPrefab);
        }

        if (AudioManager.Instance == null && audioManagerPrefab != null)
        {
            Instantiate(audioManagerPrefab);
        }

        if (ParticleManager.Instance == null && particleManagerPrefab != null)
        {
            Instantiate(particleManagerPrefab);
        }

        if (NetworkManager.Instance == null && networkManagerPrefab != null)
        {
            Instantiate(networkManagerPrefab);
        }

        if (LoginManager.Instance == null && loginManagerPrefab != null)
        {
            Instantiate(loginManagerPrefab);
        }
    }

    void SetupLighting()
    {
        if (directionalLight != null)
        {
            directionalLight.color = new Color(1f, 0.95f, 0.85f);
            directionalLight.intensity = 1.2f;
            directionalLight.shadows = LightShadows.Soft;
        }

        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.5f, 0.5f, 0.6f);
        RenderSettings.fogDensity = 0.01f;
    }

    void SetupPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("[SceneSetup] Player already exists in scene. Reusing.");
            
            // Setup camera for existing player
            CameraController cam = Camera.main?.GetComponent<CameraController>();
            if (cam != null)
            {
                cam.playerTransform = player.transform;
            }
            else if (Camera.main != null)
            {
                CameraController newCam = Camera.main.gameObject.GetComponent<CameraController>();
                if (newCam == null)
                {
                    newCam = Camera.main.gameObject.AddComponent<CameraController>();
                }
                newCam.playerTransform = player.transform;
            }

            UpgradeSystem us = FindObjectOfType<UpgradeSystem>();
            if (us != null)
            {
                us.skinManager = player.GetComponent<SkinManager>();
            }
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogWarning("[SceneSetup] Khong tim thay Player Prefab!");
            return;
        }

        Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : new Vector3(0f, 1f, 0f);
        player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        CameraController camFollow = Camera.main?.GetComponent<CameraController>();
        if (camFollow != null)
        {
            camFollow.playerTransform = player.transform;
        }
        else if (Camera.main != null)
        {
            CameraController newCam = Camera.main.gameObject.GetComponent<CameraController>();
            if (newCam == null)
            {
                newCam = Camera.main.gameObject.AddComponent<CameraController>();
            }
            newCam.playerTransform = player.transform;
        }

        UpgradeSystem upgradeSys = FindObjectOfType<UpgradeSystem>();
        if (upgradeSys != null)
        {
            upgradeSys.skinManager = player.GetComponent<SkinManager>();
        }
    }

    void SetupSpawners()
    {
        if (spawnerPrefab != null)
        {
            Instantiate(spawnerPrefab, transform);
        }

        if (bossSpawnerPrefab != null)
        {
            Instantiate(bossSpawnerPrefab, transform);
        }
    }
}
