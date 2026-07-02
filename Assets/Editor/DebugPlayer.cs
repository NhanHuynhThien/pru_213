using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class DebugPlayer
{
    static DebugPlayer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += () => {
                FixPlayerHierarchy();
                FixResourceNodes();
                FixCameraMovement();
                RecreateLootSpawnerAndTestModels(); // Tự động khôi phục LootSpawner & Test Models nếu bị mất
            };
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            FixPlayerHierarchy();
            DebugComponents();
        }
    }

    [MenuItem("Tools/Fix Missing Resource Nodes & Camera")]
    public static void FixResourceNodes()
    {
        FixPlayerHierarchy();
        // 1. Tự động gán LootItem cho các file Prefab mẫu trong thư mục Assets nếu chưa có
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Ruda_low") || path.Contains("metal_ore_pack_low") || path.Contains("Loot_HacAn") || path.Contains("Loot_NgocLuu") || path.Contains("Loot_TuMa"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    LootItem loot = prefab.GetComponent<LootItem>();
                    if (loot == null)
                    {
                        loot = prefab.AddComponent<LootItem>();
                        loot.startImmediately = true;
                        
                        if (path.Contains("Ruda_low")) loot.itemName = "Thiếc (Tin)";
                        else if (path.Contains("metal_ore_pack_low")) loot.itemName = "Đồng (Copper)";
                        
                        EditorUtility.SetDirty(prefab);
                        Debug.Log($"<color=cyan>[FixTools]</color> Đã tự động gán LootItem cho file Prefab: {prefab.name}");
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();

        // 2. Quét toàn bộ đối tượng trong Scene hiện tại
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
        int fixedCount = 0;
        int fixedLootCount = 0;
        
        foreach (GameObject go in allObjects)
        {
            string name = go.name.ToLower();
            
            // A. Vá lỗi cho các node quặng tĩnh (ResourceNode) bị mất script
            if (name.Contains("coppernode") || name.Contains("tinnode") || name.Contains("bronzenode") || name.Contains("turtleshellnode") || name.Contains("resourcenode"))
            {
                // Xóa component script bị lỗi (Missing Script)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                
                // Thêm lại component ResourceNode mới sạch sẽ
                ResourceNode node = go.GetComponent<ResourceNode>();
                if (node == null)
                {
                    node = go.AddComponent<ResourceNode>();
                    Undo.RegisterCreatedObjectUndo(node, "Restore ResourceNode");
                }
                
                // Cấu hình loại tài nguyên tương ứng theo tên gameobject
                if (name.Contains("copper")) node.type = ResourceNode.ResourceType.Copper;
                else if (name.Contains("tin")) node.type = ResourceNode.ResourceType.Tin;
                else if (name.Contains("bronze")) node.type = ResourceNode.ResourceType.Bronze;
                else if (name.Contains("turtleshell") || name.Contains("turtle")) node.type = ResourceNode.ResourceType.TurtleShell;
                
                node.amount = 5;
                node.respawnTime = 10f;
                
                // Gán visual model tự động từ con của nó
                if (node.nodeModel == null)
                {
                    Transform modelTrans = go.transform.Find("Model");
                    if (modelTrans == null) modelTrans = go.transform.Find("Ore_low");
                    if (modelTrans == null) modelTrans = go.transform.Find("Rock_low");
                    if (modelTrans == null && go.transform.childCount > 0) modelTrans = go.transform.GetChild(0);
                    
                    node.nodeModel = modelTrans != null ? modelTrans.gameObject : go;
                }
                
                // Đảm bảo có Trigger collider
                Collider col = go.GetComponent<Collider>();
                if (col == null)
                {
                    col = go.AddComponent<BoxCollider>();
                }
                col.isTrigger = true;
                
                EditorUtility.SetDirty(go);
                fixedCount++;
            }
            
            // B. Gán component LootItem cho các đối tượng Đồng & Thiếc đặt sẵn trong Scene
            if (name.Contains("ruda_low") || name.Contains("metal_ore_pack_low"))
            {
                // Kiểm tra xem đối tượng có nằm trong nhóm Loot_Test_Models (chỉ dùng để trưng bày mẫu thử) hay không
                Transform current = go.transform;
                bool isTestModel = false;
                while (current != null)
                {
                    if (current.name.Contains("Loot_Test_Models"))
                    {
                        isTestModel = true;
                        break;
                    }
                    current = current.parent;
                }

                if (isTestModel)
                {
                    // Tự động tắt Active để ẩn hoàn toàn khỏi Scene
                    if (go.activeSelf)
                    {
                        go.SetActive(false);
                        EditorUtility.SetDirty(go);
                    }
                    
                    // Nếu có component LootItem thì xóa đi
                    LootItem existingLoot = go.GetComponent<LootItem>();
                    if (existingLoot != null)
                    {
                        Object.DestroyImmediate(existingLoot);
                        EditorUtility.SetDirty(go);
                    }
                }
                else
                {
                    // Nếu là đối tượng được rải thực tế để chơi, đảm bảo có component LootItem
                    LootItem loot = go.GetComponent<LootItem>();
                    if (loot == null)
                    {
                        loot = go.AddComponent<LootItem>();
                        loot.startImmediately = true;
                        if (name.Contains("ruda_low")) loot.itemName = "Thiếc (Tin)";
                        else if (name.Contains("metal_ore_pack_low")) loot.itemName = "Đồng (Copper)";
                        
                        EditorUtility.SetDirty(go);
                        fixedLootCount++;
                    }
                }
            }
        }
        if (fixedCount > 0)
        {
            Debug.Log($"<color=cyan>[FixTools]</color> Đã tự động vá và gán lại {fixedCount} ResourceNodes bị lỗi script trong scene!");
        }
        if (fixedLootCount > 0)
        {
            Debug.Log($"<color=cyan>[FixTools]</color> Đã tự động gán LootItem cho {fixedLootCount} quặng Đồng/Thiếc trong scene!");
        }
    }

    public static void FixCameraMovement()
    {
        // Khôi phục Camera script nếu bị thiếu và xóa CameraMovement gây xung đột
        Camera cam = Camera.main;
        if (cam != null)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cam.gameObject);
            
            // Xóa component CameraMovement dư thừa để tránh xung đột với CameraController chính của game
            CameraMovement cm = cam.GetComponent<CameraMovement>();
            if (cm != null)
            {
                Object.DestroyImmediate(cm);
                Debug.Log("<color=cyan>[FixTools]</color> Đã tự động gỡ bỏ CameraMovement dư thừa trên Main Camera!");
            }
        }
    }

    private static void CopySerializedFields(Component source, Component target)
    {
        if (source == null || target == null) return;
        var type = source.GetType();
        var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            // Sao chép các trường có SerializeField hoặc public
            if (field.GetCustomAttributes(typeof(SerializeField), true).Length > 0 || field.IsPublic)
            {
                try
                {
                    field.SetValue(target, field.GetValue(source));
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[FixTools] Không thể sao chép trường {field.Name}: {ex.Message}");
                }
            }
        }
    }

    public static void FixPlayerHierarchy()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null) return;

        // 1. Đảm bảo Parent "Player" có PlayerMovement và CharacterController
        PlayerMovement parentPM = player.GetComponent<PlayerMovement>();
        if (parentPM == null)
        {
            parentPM = player.AddComponent<PlayerMovement>();
            Debug.Log("<color=cyan>[FixTools]</color> Đã tự động thêm PlayerMovement cho cha 'Player'!");
            EditorUtility.SetDirty(player);
        }

        CharacterController parentCC = player.GetComponent<CharacterController>();
        if (parentCC == null)
        {
            parentCC = player.AddComponent<CharacterController>();
            Debug.Log("<color=cyan>[FixTools]</color> Đã tự động thêm CharacterController cho cha 'Player'!");
            EditorUtility.SetDirty(player);
        }

        // Luôn chuẩn hóa kích thước và tâm của CharacterController trên cha để chân đứng khít mặt đất (tránh rơi xuyên map)
        if (parentCC.center != new Vector3(0f, 1f, 0f) || parentCC.height != 2f || parentCC.radius != 0.5f)
        {
            parentCC.center = new Vector3(0f, 1f, 0f);
            parentCC.height = 2f;
            parentCC.radius = 0.5f;
            Debug.Log("<color=cyan>[FixTools]</color> Đã tự động chuẩn hóa kích thước CharacterController trên cha 'Player' (Tâm Y=1, Cao=2, Rộng=0.5)!");
            EditorUtility.SetDirty(player);
        }

        // 2. Tìm model con (tripo_convert...) và dọn dẹp các component trùng lặp trên nó
        foreach (Transform child in player.transform)
        {
            if (child.name.StartsWith("tripo_convert"))
            {
                // Sao chép và xóa PlayerMovement trùng lặp trên con
                PlayerMovement childPM = child.GetComponent<PlayerMovement>();
                if (childPM != null)
                {
                    CopySerializedFields(childPM, parentPM);
                    Object.DestroyImmediate(childPM);
                    Debug.Log("<color=cyan>[FixTools]</color> Đã sao chép dữ liệu cấu hình và xóa PlayerMovement trên con 'tripo_convert'!");
                    EditorUtility.SetDirty(player);
                }

                // Sao chép và xóa PlayerController trùng lặp trên con
                PlayerController childPC = child.GetComponent<PlayerController>();
                if (childPC != null)
                {
                    PlayerController parentPC = player.GetComponent<PlayerController>();
                    if (parentPC == null)
                    {
                        parentPC = player.AddComponent<PlayerController>();
                    }
                    CopySerializedFields(childPC, parentPC);
                    Object.DestroyImmediate(childPC);
                    Debug.Log("<color=cyan>[FixTools]</color> Đã sao chép dữ liệu cấu hình và xóa PlayerController trên con 'tripo_convert'!");
                    EditorUtility.SetDirty(player);
                }

                // Xóa CharacterController trùng lặp trên con
                CharacterController childCC = child.GetComponent<CharacterController>();
                if (childCC != null)
                {
                    Object.DestroyImmediate(childCC);
                    Debug.Log("<color=cyan>[FixTools]</color> Đã tự động xóa CharacterController trùng lặp trên con 'tripo_convert'!");
                    EditorUtility.SetDirty(player);
                }
            }
        }
    }

    [MenuItem("Tools/Debug Player Components")]
    public static void DebugComponents()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
        }
        if (player == null)
        {
            Debug.LogError("Could not find 'Player' GameObject in the scene.");
            return;
        }

        Debug.Log("=== PLAYER DEBUG ===");
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null) pc = player.GetComponentInChildren<PlayerController>();
        
        if (pc != null)
        {
            Debug.Log($"[PlayerController] Found on: {pc.gameObject.name}, Enabled: {pc.enabled}");
            Debug.Log($"[PlayerController] WalkSpeed: {pc.walkSpeed}, SprintSpeed: {pc.sprintSpeed}");
            Debug.Log($"[PlayerController] Stats asset assigned: {pc.stats != null}");
            if (pc.stats != null)
            {
                Debug.Log($"[PlayerController.Stats] moveSpeed: {pc.stats.moveSpeed}, copper: {pc.stats.copperCount}, tin: {pc.stats.tinCount}");
            }
        }
        else
        {
            Debug.Log("[PlayerController] Not found on Player or Children.");
        }

        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm == null) pm = player.GetComponentInChildren<PlayerMovement>();
        
        if (pm != null)
        {
            Debug.Log($"[PlayerMovement] Found on: {pm.gameObject.name}, Enabled: {pm.enabled}");
            
            // Read private fields using reflection
            var speedField = typeof(PlayerMovement).GetField("_movementSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (speedField != null)
            {
                Debug.Log($"[PlayerMovement] _movementSpeed: {speedField.GetValue(pm)}");
            }
            var hasSwordField = typeof(PlayerMovement).GetField("_hasSword", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (hasSwordField != null)
            {
                Debug.Log($"[PlayerMovement] _hasSword: {hasSwordField.GetValue(pm)}");
            }
        }
        else
        {
            Debug.Log("[PlayerMovement] Not found on Player or Children.");
        }

        // Chẩn đoán Địa hình (Terrain) và Va chạm
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain != null)
        {
            Debug.Log($"[DIAGNOSTIC] Tìm thấy Terrain: {terrain.gameObject.name}, Active={terrain.gameObject.activeInHierarchy}, Position={terrain.transform.position}");
            TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
            Debug.Log($"[DIAGNOSTIC] TerrainCollider: {(tc != null ? "Có" : "Không")}, Enabled={(tc != null ? tc.enabled.ToString() : "N/A")}");
        }
        else
        {
            Debug.LogWarning("[DIAGNOSTIC] Không tìm thấy đối tượng Terrain nào trong cảnh!");
        }
    }

    [MenuItem("Tools/Restore Spawner & Loot Test Models")]
    public static void RecreateLootSpawnerAndTestModels()
    {
        // 1. Tải các Prefab cần thiết
        GameObject ngocLuuLyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Loot_NgocLuuLy.prefab");
        GameObject tuMaThachPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Loot_TuMaThach.prefab");
        GameObject hacAnTinhThePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Loot_HacAnTinhThe.prefab");
        GameObject rudaLowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ruda_low.prefab");
        GameObject metalOrePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/metal_ore_pack_low.prefab");

        if (ngocLuuLyPrefab == null || tuMaThachPrefab == null || hacAnTinhThePrefab == null || rudaLowPrefab == null || metalOrePrefab == null)
        {
            Debug.LogError("[FixTools] Không thể tìm thấy đầy đủ các Prefabs quặng và đá trong thư mục Assets/Prefabs!");
            return;
        }

        // 2. Tạo hoặc khôi phục LootSpawner
        GameObject lootSpawnerGO = GameObject.Find("LootSpawner");
        if (lootSpawnerGO == null)
        {
            lootSpawnerGO = new GameObject("LootSpawner");
            lootSpawnerGO.transform.position = new Vector3(-25.87f, 1.05f, -74.47f); // Đồng bộ vị trí với MonsterSpawner
            Undo.RegisterCreatedObjectUndo(lootSpawnerGO, "Recreate LootSpawner");
            Debug.Log("<color=green>[FixTools]</color> Đã tạo mới đối tượng 'LootSpawner' trong Scene.");
        }

        ZoneLootSpawner spawner = lootSpawnerGO.GetComponent<ZoneLootSpawner>();
        if (spawner == null)
        {
            spawner = lootSpawnerGO.AddComponent<ZoneLootSpawner>();
        }

        // Cấu hình loot templates
        spawner.lootTemplates.Clear();
        
        spawner.lootTemplates.Add(new ZoneLootSpawner.LootTemplateConfig {
            lootName = "Ngọc Lưu Ly",
            templateObject = ngocLuuLyPrefab,
            spawnChance = 0.8f,
            customScale = 1f,
            customYOffset = 0.02f
        });

        spawner.lootTemplates.Add(new ZoneLootSpawner.LootTemplateConfig {
            lootName = "Tử Ma Thạch",
            templateObject = tuMaThachPrefab,
            spawnChance = 0.6f,
            customScale = 1f,
            customYOffset = 0.02f
        });

        spawner.lootTemplates.Add(new ZoneLootSpawner.LootTemplateConfig {
            lootName = "Hắc Ám Tinh Thể",
            templateObject = hacAnTinhThePrefab,
            spawnChance = 0.4f,
            customScale = 1f,
            customYOffset = 0.02f
        });

        spawner.lootTemplates.Add(new ZoneLootSpawner.LootTemplateConfig {
            lootName = "Thiếc (Tin)",
            templateObject = rudaLowPrefab,
            spawnChance = 0.7f,
            customScale = 0.08f,
            customYOffset = 0.02f
        });

        spawner.lootTemplates.Add(new ZoneLootSpawner.LootTemplateConfig {
            lootName = "Đồng (Copper)",
            templateObject = metalOrePrefab,
            spawnChance = 0.7f,
            customScale = 0.08f,
            customYOffset = 0.02f
        });

        // Cấu hình spawn zones
        Transform forestTrans = null;
        GameObject forestGO = GameObject.Find("Forest");
        if (forestGO != null) forestTrans = forestGO.transform;
        else forestTrans = lootSpawnerGO.transform;

        spawner.spawnZones.Clear();
        spawner.spawnZones.Add(new ZoneLootSpawner.SpawnZone {
            zoneName = "Trong Thành",
            zoneCenter = lootSpawnerGO.transform,
            minRadius = 0f,
            spawnRadius = 25f,
            lootCount = 8
        });
        spawner.spawnZones.Add(new ZoneLootSpawner.SpawnZone {
            zoneName = "Rừng Xanh",
            zoneCenter = forestTrans,
            minRadius = 30f,
            spawnRadius = 80f,
            lootCount = 15
        });

        spawner.spawnOnStart = true;
        spawner.generatedHolderName = "_Scattered_Loot_Container";
        EditorUtility.SetDirty(lootSpawnerGO);
        Debug.Log("<color=green>[FixTools]</color> Đã tự động cấu hình 5 loại tài nguyên & 2 vùng rải cho 'LootSpawner' thành công!");

        // 3. Tạo hoặc khôi phục Loot_Test_Models (Khu trưng bày mẫu thử)
        GameObject testModelsGO = GameObject.Find("Loot_Test_Models");
        if (testModelsGO == null)
        {
            testModelsGO = new GameObject("Loot_Test_Models");
            testModelsGO.transform.position = new Vector3(-18.9f, 0.47f, 15f);
            Undo.RegisterCreatedObjectUndo(testModelsGO, "Recreate Loot_Test_Models");
            Debug.Log("<color=green>[FixTools]</color> Đã tạo mới đối tượng 'Loot_Test_Models' trong Scene.");
        }

        // Tạo các mẫu thử bên trong
        string[] testNames = { "Loot_NgocLuuLy", "Loot_TuMaThach", "Loot_HacAnTinhThe", "Ruda_low", "metal_ore_pack_low" };
        GameObject[] prefabs = { ngocLuuLyPrefab, tuMaThachPrefab, hacAnTinhThePrefab, rudaLowPrefab, metalOrePrefab };
        Vector3[] localOffsets = {
            new Vector3(-4f, 0f, 0f),
            new Vector3(-2f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(2f, 0f, 0f),
            new Vector3(4f, 0f, 0f)
        };

        for (int i = 0; i < testNames.Length; i++)
        {
            Transform existingChild = testModelsGO.transform.Find(testNames[i]);
            if (existingChild == null)
            {
                GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], testModelsGO.transform);
                child.name = testNames[i];
                child.transform.localPosition = localOffsets[i];
                child.transform.localRotation = Quaternion.identity;
                
                // Set scale tương ứng
                if (testNames[i].Contains("low"))
                {
                    child.transform.localScale = new Vector3(1f, 1f, 1f); // Giữ to để trưng bày mẫu như cũ
                    child.SetActive(false); // Ẩn hoàn toàn như yêu cầu cũ
                }
                else
                {
                    child.transform.localScale = Vector3.one;
                    child.SetActive(true);
                }

                // Gỡ LootItem trên quặng test để người chơi không nhặt nhầm vật mẫu
                LootItem li = child.GetComponent<LootItem>();
                if (li != null)
                {
                    Object.DestroyImmediate(li);
                }
                
                Debug.Log($"<color=green>[FixTools]</color> Đã khôi phục vật mẫu trưng bày: {testNames[i]}");
            }
        }
        
        EditorUtility.SetDirty(testModelsGO);
        
        // Lưu Scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("<color=green>[FixTools]</color> Đã khôi phục hoàn toàn hệ thống Spawner & Test Models thành công!");
    }

    [MenuItem("Tools/Synchronize Player & Model")]
    public static void AlignPlayerWithModel()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[FixTools] Không tìm thấy đối tượng 'Player' trong Scene!");
            return;
        }

        Transform modelChild = null;
        foreach (Transform child in player.transform)
        {
            if (child.name.StartsWith("tripo_convert"))
            {
                modelChild = child;
                break;
            }
        }

        if (modelChild == null)
        {
            Debug.LogError("[FixTools] Không tìm thấy model con 'tripo_convert...' dưới 'Player'!");
            return;
        }

        Undo.RecordObject(player.transform, "Align Player");
        Undo.RecordObject(modelChild, "Align Model");

        // Lấy thế giới của model con hiện tại (vị trí người chơi nhìn thấy ở ngoài)
        Vector3 childWorldPos = modelChild.position;
        Quaternion childWorldRot = modelChild.rotation;

        // Bù trừ độ cao local Y chuẩn (khớp bàn chân chạm khít mặt đất)
        float targetLocalY = -0.064f;

        // Di chuyển cha tới đúng vị trí thế giới của con
        player.transform.position = new Vector3(childWorldPos.x, childWorldPos.y - targetLocalY, childWorldPos.z);
        player.transform.rotation = childWorldRot;

        // Đưa con về đồng tâm XZ và chỉ giữ lại độ cao bù trừ chuẩn Y = -0.064f
        modelChild.localPosition = new Vector3(0f, targetLocalY, 0f);
        modelChild.localRotation = Quaternion.identity;

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(modelChild.gameObject);

        // Lưu Scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("<color=green>[FixTools]</color> Đã đồng bộ thành công! Cha 'Player' đã dời xuống đất khớp với model con.");
    }
}
