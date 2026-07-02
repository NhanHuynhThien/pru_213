using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ZoneLootSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnZone
    {
        public string zoneName = "New Zone";
        [Tooltip("Transform đại diện cho tâm vùng rải tài nguyên (ví dụ: Mỏ Đồng, Rừng)")]
        public Transform zoneCenter;
        [Tooltip("Bán kính rải tối thiểu tính từ tâm")]
        public float minRadius = 0f;
        [Tooltip("Bán kính rải tối đa xung quanh tâm")]
        public float spawnRadius = 20f;
        [Tooltip("Số lượng nguyên liệu muốn sinh ra trong vùng này")]
        public int lootCount = 5;
    }

    [System.Serializable]
    public class LootTemplateConfig
    {
        public string lootName = "Loot";
        [Tooltip("Prefab của tài nguyên (Đá, Quặng, ...), hoặc kéo mẫu có sẵn trong Scene vào đây")]
        public GameObject templateObject;
        [Tooltip("Tỷ lệ xuất hiện (0 đến 1)")]
        [Range(0f, 1f)] public float spawnChance = 0.5f;
        
        [Tooltip("Thay đổi kích cỡ của tài nguyên khi rải (1 = giữ nguyên kích thước gốc)")]
        public float customScale = 1f;
        [Tooltip("Bù trừ độ cao để tài nguyên không bị chìm hay lơ lửng (Mặc định 0.02)")]
        public float customYOffset = 0.02f;
    }

    [Header("Loot Templates")]
    [Tooltip("Kéo các mẫu tài nguyên (như Đá Lưu Ly, Quặng Đồng) vào đây")]
    public List<LootTemplateConfig> lootTemplates = new();

    [Header("Spawn Zones")]
    [Tooltip("Danh sách các vùng muốn rải tài nguyên")]
    public List<SpawnZone> spawnZones = new();

    [Header("Settings")]
    [Tooltip("Tự động rải tài nguyên khi bắt đầu chơi game (Runtime)")]
    public bool spawnOnStart = false;
    [Tooltip("Tên GameObject cha để gom nhóm tài nguyên cho gọn")]
    public string generatedHolderName = "_Scattered_Loot_Container";

    private void Start()
    {
        // Tự động ẩn các template gốc trong Hierarchy
        foreach (var config in lootTemplates)
        {
            if (config.templateObject != null && !string.IsNullOrEmpty(config.templateObject.scene.name))
            {
                config.templateObject.SetActive(false);
            }
        }

        if (spawnOnStart)
        {
            ClearGeneratedLoot();
            SpawnLoot();
        }
    }

    public void SpawnLoot()
    {
        if (lootTemplates.Count == 0)
        {
            Debug.LogWarning("[ZoneLootSpawner] Chưa cấu hình mẫu trong danh sách 'Loot Templates'!");
            return;
        }

        GameObject holder = GameObject.Find(generatedHolderName);
        if (holder == null)
        {
            holder = new GameObject(generatedHolderName);
        }

        foreach (var zone in spawnZones)
        {
            if (zone.zoneCenter == null)
            {
                Debug.LogWarning($"[ZoneLootSpawner] Vùng '{zone.zoneName}' chưa được gán 'Zone Center'!");
                continue;
            }

            int spawnedCount = 0;
            int attempts = 0;
            int maxAttempts = zone.lootCount * 10; 

            while (spawnedCount < zone.lootCount && attempts < maxAttempts)
            {
                attempts++;
                
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(zone.minRadius, zone.spawnRadius);
                Vector3 randomOffset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                Vector3 searchPos = zone.zoneCenter.position + randomOffset;

                if (GetGroundPosition(searchPos, out Vector3 groundPos))
                {
                    LootTemplateConfig config = GetRandomLootTemplate();
                    if (config != null && config.templateObject != null)
                    {
                        // Sinh loot tại groundPos với Y offset bù trừ
                        Vector3 finalPos = new Vector3(groundPos.x, groundPos.y + config.customYOffset, groundPos.z);
                        GameObject lootObj;
                        if (Application.isPlaying)
                        {
                            lootObj = Instantiate(config.templateObject, finalPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
                        }
                        else
                        {
#if UNITY_EDITOR
                            lootObj = (GameObject)PrefabUtility.InstantiatePrefab(config.templateObject);
                            lootObj.transform.position = finalPos;
                            lootObj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                            Undo.RegisterCreatedObjectUndo(lootObj, "Scatter Loot Item");
#else
                            lootObj = Instantiate(config.templateObject, finalPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
#endif
                        }
                        
                        lootObj.name = config.templateObject.name.Replace("(Clone)", "").Trim() + "_" + spawnedCount;
                        lootObj.transform.SetParent(holder.transform);
                        
                        // Đảm bảo đối tượng được sinh ra có component LootItem để nhặt được
                        LootItem loot = lootObj.GetComponent<LootItem>();
                        if (loot == null)
                        {
                            loot = lootObj.AddComponent<LootItem>();
                            loot.itemName = config.lootName; // Gán tên như Quặng Đồng, Quặng Thiếc
                            loot.startImmediately = true;
                        }

                        if (config.customScale != 1f)
                        {
                            lootObj.transform.localScale = new Vector3(config.customScale, config.customScale, config.customScale);
                        }

                        lootObj.SetActive(true);
                        spawnedCount++;
                    }
                }
            }

            Debug.Log($"[ZoneLootSpawner] Đã rải {spawnedCount}/{zone.lootCount} nguyên liệu tại vùng '{zone.zoneName}'.");
        }
    }

    public void ClearGeneratedLoot()
    {
        GameObject holder = GameObject.Find(generatedHolderName);
        if (holder != null)
        {
            if (Application.isPlaying)
            {
                holder.name = "_DELETED_HOLDER_";
                Destroy(holder);
            }
            else
            {
                DestroyImmediate(holder);
            }
            Debug.Log("[ZoneLootSpawner] Đã xóa sạch nguyên liệu/đá tự động rải.");
        }
    }

    private bool GetGroundPosition(Vector3 searchPos, out Vector3 groundPos)
    {
        groundPos = searchPos;
        Vector3 rayStart = searchPos + Vector3.up * 100f;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 200f);

        RaycastHit bestHit = new RaycastHit();
        bool foundGround = false;
        float lowestY = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger || hit.collider.CompareTag("Player") || hit.collider.CompareTag("MainCamera"))
                continue;

            if (hit.collider.gameObject.name.ToLower().Contains("terrain") || hit.collider.GetComponent<Terrain>() != null)
            {
                bestHit = hit;
                foundGround = true;
                break;
            }

            if (hit.point.y < lowestY)
            {
                lowestY = hit.point.y;
                bestHit = hit;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            groundPos = bestHit.point;
            return true;
        }

        return false;
    }

    private LootTemplateConfig GetRandomLootTemplate()
    {
        List<LootTemplateConfig> validConfigs = new();
        float totalChance = 0f;

        foreach (var config in lootTemplates)
        {
            if (config.templateObject != null && config.spawnChance > 0f)
            {
                validConfigs.Add(config);
                totalChance += config.spawnChance;
            }
        }

        if (validConfigs.Count == 0) return null;

        float rand = Random.Range(0f, totalChance);
        float cumulative = 0f;

        foreach (var config in validConfigs)
        {
            cumulative += config.spawnChance;
            if (rand <= cumulative)
            {
                return config;
            }
        }

        return validConfigs[0];
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ZoneLootSpawner))]
public class ZoneLootSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ZoneLootSpawner spawner = (ZoneLootSpawner)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("CÔNG CỤ RẢI NGUYÊN LIỆU (EDITOR TOOL)", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Rải Tài Nguyên Ngay", GUILayout.Height(35)))
        {
            Undo.RegisterCompleteObjectUndo(spawner, "Rải Tài Nguyên");
            spawner.ClearGeneratedLoot();
            spawner.SpawnLoot();

            EditorUtility.SetDirty(spawner.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }

        if (GUILayout.Button("Xóa Đã Rải", GUILayout.Height(35)))
        {
            Undo.RegisterCompleteObjectUndo(spawner, "Xóa Tài Nguyên");
            spawner.ClearGeneratedLoot();
            
            EditorUtility.SetDirty(spawner.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "HƯỚNG DẪN:\n" +
            "1. Kéo các Prefab Đá/Quặng từ Assets vào 'Loot Templates'.\n" +
            "2. Chỉnh 'Custom Scale' và 'Custom Y Offset' trực tiếp ở đây để căn chỉnh to/nhỏ/cao/thấp.\n" +
            "3. Tạo các vùng trong 'Spawn Zones' (chọn Mỏ Đồng hoặc Rừng làm Zone Center).\n" +
            "4. Bấm 'Rải Tài Nguyên Ngay' để tự động sinh hàng loạt ngay trên Scene.", 
            MessageType.Info
        );
    }
}
#endif
