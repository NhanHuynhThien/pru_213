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
                FixResourceNodes();
                FixCameraMovement();
            };
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            DebugComponents();
        }
    }

    [MenuItem("Tools/Fix Missing Resource Nodes & Camera")]
    public static void FixResourceNodes()
    {
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
        if (pc != null)
        {
            Debug.Log($"[PlayerController] Enabled: {pc.enabled}");
            Debug.Log($"[PlayerController] WalkSpeed: {pc.walkSpeed}, SprintSpeed: {pc.sprintSpeed}");
            Debug.Log($"[PlayerController] Stats asset assigned: {pc.stats != null}");
            if (pc.stats != null)
            {
                Debug.Log($"[PlayerController.Stats] moveSpeed: {pc.stats.moveSpeed}, copper: {pc.stats.copperCount}, tin: {pc.stats.tinCount}");
            }
        }
        else
        {
            Debug.Log("[PlayerController] Not found on Player.");
        }

        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            Debug.Log($"[PlayerMovement] Enabled: {pm.enabled}");
            
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
            Debug.Log("[PlayerMovement] Not found on Player.");
        }
    }
}
