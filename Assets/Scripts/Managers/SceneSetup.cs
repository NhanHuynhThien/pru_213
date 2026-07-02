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
        // Chẩn đoán địa hình (Terrain) và các thực thể khác
        GameObject terrainGO = GameObject.Find("Terrain");
        if (terrainGO == null) terrainGO = GameObject.FindObjectOfType<Terrain>()?.gameObject;
        
        if (terrainGO != null)
        {
            Debug.Log($"[DIAGNOSTIC] Tìm thấy Terrain: Active={terrainGO.activeSelf}, ActiveInHierarchy={terrainGO.activeInHierarchy}, Vị trí={terrainGO.transform.position}");
            TerrainCollider tc = terrainGO.GetComponent<TerrainCollider>();
            Debug.Log($"[DIAGNOSTIC] TerrainCollider: {(tc != null ? "Có" : "Không")}, Enabled={(tc != null ? tc.enabled.ToString() : "N/A")}");
        }
        else
        {
            Debug.LogWarning("[DIAGNOSTIC] Không tìm thấy đối tượng Terrain nào trong cảnh!");
        }

        PlayerMovement[] allPMs = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        Debug.Log($"[DIAGNOSTIC] Số lượng PlayerMovement trong Scene: {allPMs.Length}");
        foreach (var pm in allPMs)
        {
            Debug.Log($"[DIAGNOSTIC] PlayerMovement nằm trên GameObject: {pm.gameObject.name}, Vị trí={pm.transform.position}");
        }

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
        // HỢP NHẤT: Tìm theo Tag trước, nếu không thấy thì tìm theo Tên (Logic của bạn)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        if (player != null)
        {
            // Nếu đã tìm thấy Player trong Scene, sử dụng luôn Player này và giữ nguyên vị trí đặt trong Editor
            Debug.Log("[SceneSetup] Phát hiện Player đã có sẵn trong Scene. Sử dụng vị trí đặt trong Editor.");
        }
        else
        {
            // Nếu chưa có, mới sinh ra từ Prefab tại điểm Spawn
            if (playerPrefab == null)
            {
                Debug.LogWarning("[SceneSetup] Không tìm thấy Player và không có playerPrefab!");
                return;
            }
            Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : new Vector3(0f, 1f, 0f);
            player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            Debug.Log("[SceneSetup] Đã sinh Player mới từ Prefab tại điểm Spawn.");
        }

        // HỢP NHẤT: Xử lý gán Camera follow và chống trùng lắp Component CameraController từ Main
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

        // HỢP NHẤT: Đồng bộ hệ thống SkinManager và dùng hàm tối ưu FindFirstObjectByType của bạn
        UpgradeSystem upgradeSys = FindFirstObjectByType<UpgradeSystem>();
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