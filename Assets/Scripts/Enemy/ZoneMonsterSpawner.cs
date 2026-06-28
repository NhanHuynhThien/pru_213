using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ZoneMonsterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnZone
    {
        public string zoneName = "New Zone";
        [Tooltip("Transform đại diện cho tâm vùng rải quái (ví dụ: Village_Area, Forest...)")]
        public Transform zoneCenter;
        [Tooltip("Bán kính rải quái tối thiểu tính từ tâm (ví dụ: đặt 55 để quái chỉ xuất hiện ngoài tường thành)")]
        public float minRadius = 0f;
        [Tooltip("Bán kính rải quái tối đa xung quanh tâm")]
        public float spawnRadius = 20f;
        [Tooltip("Số lượng quái vật muốn sinh ra trong vùng này")]
        public int monsterCount = 5;
    }

    [System.Serializable]
    public class MonsterTemplateConfig
    {
        public string monsterName = "Monster";
        [Tooltip("Prefab quái vật, hoặc kéo GameObject mẫu đang có sẵn trong Hierarchy vào đây")]
        public GameObject templateObject;
        [Tooltip("Tỷ lệ xuất hiện của loại quái vật này (0 đến 1)")]
        [Range(0f, 1f)] public float spawnChance = 0.5f;
    }

    [Header("Monster Templates")]
    [Tooltip("Kéo các quái mẫu (như Wolf, Linh Thú Tha Hóa) vào đây")]
    public List<MonsterTemplateConfig> monsterTemplates = new();

    [Header("Spawn Zones")]
    [Tooltip("Danh sách các vùng muốn rải quái")]
    public List<SpawnZone> spawnZones = new();

    [Header("Settings")]
    [Tooltip("Tự động sinh quái khi bắt đầu chơi game (Runtime)")]
    public bool spawnOnStart = false;
    [Tooltip("Tên GameObject cha để gom nhóm các quái vật cho gọn")]
    public string generatedHolderName = "_GeneratedMonsters";

    private void Start()
    {
        // Tự động ẩn các template gốc trong Hierarchy để tránh chúng tự chạy hoặc hiển thị thừa
        foreach (var config in monsterTemplates)
        {
            if (config.templateObject != null && !string.IsNullOrEmpty(config.templateObject.scene.name))
            {
                config.templateObject.SetActive(false);
            }
        }

        if (spawnOnStart)
        {
            // Xóa quái cũ đã rải trong Editor (nếu có) trước khi sinh quái runtime để tránh trùng lặp
            ClearGeneratedMonsters();
            SpawnMonsters();
        }
    }

    public void SpawnMonsters()
    {
        if (monsterTemplates.Count == 0)
        {
            Debug.LogWarning("[ZoneMonsterSpawner] Chưa cấu hình quái vật mẫu trong danh sách 'Monster Templates'!");
            return;
        }

        // Tạo hoặc tìm GameObject gom nhóm quái vật
        GameObject holder = GameObject.Find(generatedHolderName);
        if (holder == null)
        {
            holder = new GameObject(generatedHolderName);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Transform playerTransform = playerObj != null ? playerObj.transform : null;

        foreach (var zone in spawnZones)
        {
            if (zone.zoneCenter == null)
            {
                Debug.LogWarning($"[ZoneMonsterSpawner] Vùng '{zone.zoneName}' chưa được gán 'Zone Center'!");
                continue;
            }

            int spawnedCount = 0;
            int attempts = 0;
            int maxAttempts = zone.monsterCount * 10; // Giới hạn lượt thử để tránh kẹt loop

            while (spawnedCount < zone.monsterCount && attempts < maxAttempts)
            {
                attempts++;
                
                // Sinh vị trí ngẫu nhiên hình vành khăn (giữa minRadius và spawnRadius) để rải đều xung quanh tâm
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(zone.minRadius, zone.spawnRadius);
                Vector3 randomOffset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                Vector3 searchPos = zone.zoneCenter.position + randomOffset;

                if (GetGroundPosition(searchPos, out Vector3 groundPos))
                {
                    GameObject template = GetRandomMonsterTemplate();
                    if (template != null)
                    {
                        // Sinh quái
                        GameObject monster = Instantiate(template, groundPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
                        monster.name = template.name.Replace("(Clone)", "").Trim() + "_" + spawnedCount;
                        monster.transform.SetParent(holder.transform);
                        
                        // Đảm bảo quái vật được active (phòng hờ template gốc đang bị ẩn)
                        monster.SetActive(true);

                        // Liên kết mục tiêu (Player) cho quái vật
                        SetupMonsterBehavior(monster, playerTransform);

                        spawnedCount++;
                    }
                }
            }

            Debug.Log($"[ZoneMonsterSpawner] Đã rải {spawnedCount}/{zone.monsterCount} quái tại vùng '{zone.zoneName}' sau {attempts} lượt thử.");
        }
    }

    public void ClearGeneratedMonsters()
    {
        GameObject holder = GameObject.Find(generatedHolderName);
        if (holder != null)
        {
            // Dùng DestroyImmediate nếu đang chạy trong Editor, Destroy nếu trong Runtime
            if (Application.isPlaying)
            {
                Destroy(holder);
            }
            else
            {
                DestroyImmediate(holder);
            }
            Debug.Log("[ZoneMonsterSpawner] Đã xóa sạch các quái vật đã rải tự động.");
        }
    }

    private bool GetGroundPosition(Vector3 searchPos, out Vector3 groundPos)
    {
        groundPos = searchPos;

        // 1. Thử chiếu lên NavMesh để quái vật có thể di chuyển được ngay
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(searchPos, out navHit, 15f, NavMesh.AllAreas))
        {
            groundPos = navHit.position;
            return true;
        }

        // 2. Dự phòng: Bắn tia Raycast xuyên qua mọi thứ từ trên cao xuống
        Vector3 rayStart = searchPos + Vector3.up * 100f;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 200f);

        RaycastHit bestHit = new RaycastHit();
        bool foundGround = false;
        float lowestY = float.MaxValue;

        foreach (var hit in hits)
        {
            // Bỏ qua va chạm với người chơi hoặc camera hoặc chính spawner
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("MainCamera"))
                continue;

            // Ưu tiên cao nhất: Đâm trúng đối tượng tên là Terrain hoặc có component Terrain
            if (hit.collider.gameObject.name.ToLower().Contains("terrain") || hit.collider.GetComponent<Terrain>() != null)
            {
                bestHit = hit;
                foundGround = true;
                break; // Tìm thấy terrain là lấy luôn
            }

            // Dự phòng: Chọn điểm va chạm thấp nhất (để tránh ngọn cây, mái nhà ở trên cao)
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

    private GameObject GetRandomMonsterTemplate()
    {
        List<MonsterTemplateConfig> validConfigs = new();
        float totalChance = 0f;

        foreach (var config in monsterTemplates)
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
                return config.templateObject;
            }
        }

        return validConfigs[0].templateObject;
    }

    private void SetupMonsterBehavior(GameObject monster, Transform playerTransform)
    {
        // 1. Gán target cho BossController (nếu là Wolf)
        BossController boss = monster.GetComponent<BossController>();
        if (boss != null)
        {
            boss.target = playerTransform;
            // Kích hoạt lại AI
            boss.currentState = BossController.BossState.Idle;
            return;
        }

        // 2. Gán target cho EnemyController (nếu là Linh Thú Tha Hóa / U Minh Binh)
        EnemyController enemy = monster.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.target = playerTransform;
            return;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ZoneMonsterSpawner))]
public class ZoneMonsterSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ZoneMonsterSpawner spawner = (ZoneMonsterSpawner)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("CÔNG CỤ RẢI QUÁI (EDITOR TOOL)", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Rải Quái Ngay (Editor)", GUILayout.Height(35)))
        {
            // Đăng ký lịch sử Undo để người dùng có thể Ctrl+Z hoàn tác
            Undo.RegisterCompleteObjectUndo(spawner, "Rải Quái Vật");
            
            // Xóa quái cũ trước khi rải mới
            spawner.ClearGeneratedMonsters();
            
            spawner.SpawnMonsters();

            // Đánh dấu Scene thay đổi để lưu được
            EditorUtility.SetDirty(spawner.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }

        if (GUILayout.Button("Xóa Quái Đã Rải", GUILayout.Height(35)))
        {
            Undo.RegisterCompleteObjectUndo(spawner, "Xóa Quái Vật");
            spawner.ClearGeneratedMonsters();
            
            EditorUtility.SetDirty(spawner.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "HƯỚNG DẪN:\n" +
            "1. Kéo các đối tượng mẫu (như Wolf, Linh Thú Tha Hóa) từ Hierarchy hoặc Prefab từ Assets vào 'Monster Templates'.\n" +
            "2. Trong 'Spawn Zones', tạo các vùng và gán các đối tượng đại diện như Village_Area, Forest làm 'Zone Center'.\n" +
            "3. Bấm 'Rải Quái Ngay (Editor)' để tự động sinh và sắp xếp ngẫu nhiên ngay trong Scene.\n" +
            "4. Bạn có thể tự do di chuyển, chỉnh sửa các con quái được sinh ra dưới thư mục '_GeneratedMonsters'.", 
            MessageType.Info
        );
    }
}
#endif
