using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class AssignSwordPrefab : EditorWindow
{
    [MenuItem("Tools/PRU213/Assign Sword Prefab to Player")]
    public static void Assign()
    {
        // 0. Tự động hiển thị lại các nhân vật tripo_convert bị ẩn nhầm trước đó
        int reactivatedCount = 0;
        
        // Tìm nhanh qua Animators
        var animators = Resources.FindObjectsOfTypeAll<Animator>();
        foreach (var anim in animators)
        {
            if (anim == null) continue;
            GameObject go = anim.gameObject;
            if (EditorUtility.IsPersistent(go)) continue;
            
            if (go.name.ToLower().Contains("tripo_convert") && !go.activeSelf)
            {
                Undo.RecordObject(go, "Reactivate Hidden Character");
                go.SetActive(true);
                EditorUtility.SetDirty(go);
                EditorSceneManager.MarkSceneDirty(go.scene);
                reactivatedCount++;
            }
        }
        
        // Tìm nhanh qua SkinnedMeshRenderers
        var skinnedRenderers = Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>();
        foreach (var smr in skinnedRenderers)
        {
            if (smr == null) continue;
            GameObject go = smr.gameObject;
            if (EditorUtility.IsPersistent(go)) continue;
            
            if (go.name.ToLower().Contains("tripo_convert") && !go.activeSelf)
            {
                // Kiểm tra để tránh trùng lặp đợt kích hoạt
                if (!go.activeSelf)
                {
                    Undo.RecordObject(go, "Reactivate Hidden Character");
                    go.SetActive(true);
                    EditorUtility.SetDirty(go);
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    reactivatedCount++;
                }
            }
        }

        if (reactivatedCount > 0)
        {
            Debug.Log($"<color=green>[Tools]</color> Đã tự động kích hoạt lại {reactivatedCount} nhân vật bị ẩn trước đó.");
        }

        // 1. Tìm nhân vật chính trong scene (ưu tiên đối tượng active có PlayerMovement)
        GameObject playerObj = null;
        PlayerMovement activePM = GameObject.FindAnyObjectByType<PlayerMovement>();
        if (activePM != null)
        {
            playerObj = activePM.gameObject;
        }
        else
        {
            playerObj = FindGameObjectInActiveScene("U Minh Tướng");
            if (playerObj == null) playerObj = GameObject.Find("U Minh Tướng");
            if (playerObj == null)
            {
                // Thử tìm bất kỳ đối tượng nào kể cả ẩn có component PlayerMovement
                var allPMs = Resources.FindObjectsOfTypeAll<PlayerMovement>();
                foreach (var pm in allPMs)
                {
                    if (!EditorUtility.IsPersistent(pm.gameObject))
                    {
                        playerObj = pm.gameObject;
                        break;
                    }
                }
            }
        }

        if (playerObj == null)
        {
            Debug.LogError("[Tools] Không tìm thấy nhân vật chính có component PlayerMovement trong scene!");
            return;
        }

        // 2. Lấy component PlayerMovement
        PlayerMovement playerMovement = playerObj.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("[Tools] Nhân vật không có component PlayerMovement!");
            return;
        }

        // 3. Tìm asset stylized_wooden_sword model
        string prefabPath = "Assets/Assets_3D/Assets_3D/Weapons/Kiếm Gỗ/stylized_wooden_sword.glb";
        GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (swordPrefab == null)
        {
            // Thử tìm kiếm trong AssetDatabase bằng tên
            string[] guids = AssetDatabase.FindAssets("stylized_wooden_sword t:Model");
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("stylized_wooden_sword t:Prefab");
            }
            if (guids.Length > 0)
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(foundPath);
            }
        }

        if (swordPrefab == null)
        {
            Debug.LogError("[Tools] Không tìm thấy file model/prefab stylized_wooden_sword trong Assets!");
            return;
        }

        // 3.5. Tìm asset bronze_dagger prefab
        string daggerPrefabPath = "Assets/Assets_3D/Weapons/Kiếm Đồng/bronze_dagger.glb";
        GameObject daggerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(daggerPrefabPath);
        if (daggerPrefab == null)
        {
            Debug.LogWarning("[Tools] Không tìm thấy file model bronze_dagger.glb trong Assets!");
        }

        // 4. Gán prefab vào trường private _swordPrefab bằng SerializedObject
        SerializedObject so = new SerializedObject(playerMovement);
        SerializedProperty prop = so.FindProperty("_swordPrefab");
        
        if (prop != null)
        {
            prop.objectReferenceValue = swordPrefab;
            so.ApplyModifiedProperties();
            
            // 5. Thiết lập Tag "Player" cho nhân vật để Boss Sói có thể phát hiện và di chuyển/tấn công
            playerObj.tag = "Player";
            if (playerObj.transform.parent != null && playerObj.transform.parent.name == "Player")
            {
                playerObj.transform.parent.gameObject.tag = "Player";
            }
            
            // 5.5. Tắt Apply Root Motion trên Animator của người chơi để tránh bị lún đất khi diễn hoạt
            Animator playerAnimator = playerObj.GetComponentInChildren<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.applyRootMotion = false;
                EditorUtility.SetDirty(playerAnimator);
            }
            
            // 6. Điều chỉnh Camera để ZOOM gần nhân vật hơn
            CameraController cameraController = GameObject.FindAnyObjectByType<CameraController>();
            if (cameraController != null)
            {
                cameraController.offset = new Vector3(0f, 2.2f, -3.5f); // Đưa camera xuống thấp và sát nhân vật hơn
                EditorUtility.SetDirty(cameraController);
            }

            // 7. Gán vị trí, góc xoay và tỉ lệ scale mặc định chuẩn cho Kiếm cầm trên tay
            SerializedProperty offsetProp = so.FindProperty("_swordOffset");
            SerializedProperty rotProp = so.FindProperty("_swordRotation");
            SerializedProperty scaleProp = so.FindProperty("_swordScale");
            if (offsetProp != null) offsetProp.vector3Value = new Vector3(0.03f, 0.102f, 0.062f);
            if (rotProp != null) rotProp.vector3Value = new Vector3(15.362f, -277.364f, -215.845f);
            if (scaleProp != null) scaleProp.vector3Value = new Vector3(0.2f, 0.2f, 0.2f);
            so.ApplyModifiedProperties();

            // 7.5. Cấu hình Boss Sói trong Scene để có thể nhận sát thương và tấn công
            GameObject wolfObj = FindGameObjectInActiveScene("Wolf");
            if (wolfObj == null) wolfObj = GameObject.Find("Wolf");
            if (wolfObj != null)
            {
                // Đảm bảo có Animator
                Animator wolfAnimator = wolfObj.GetComponent<Animator>();
                if (wolfAnimator == null)
                {
                    wolfAnimator = wolfObj.AddComponent<Animator>();
                }
                
                // Gán controller cho Animator của Wolf
                RuntimeAnimatorController bossControllerAsset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animators/BossAnimator.controller");
                if (bossControllerAsset != null)
                {
                    wolfAnimator.runtimeAnimatorController = bossControllerAsset;
                }
                
                // Tắt Apply Root Motion để tránh Sói bị di chuyển/lún đất lỗi khi diễn hoạt
                wolfAnimator.applyRootMotion = false;

                // Đảm bảo có CharacterController
                CharacterController wolfCC = wolfObj.GetComponent<CharacterController>();
                if (wolfCC == null)
                {
                    wolfCC = wolfObj.AddComponent<CharacterController>();
                }
                
                // Điều chỉnh kích thước CharacterController của Sói cho vừa vặn
                wolfCC.center = new Vector3(0f, 1f, 0f);
                wolfCC.radius = 1.0f;
                wolfCC.height = 2f;

                // Đảm bảo có BossController script
                BossController bossCtrl = wolfObj.GetComponent<BossController>();
                if (bossCtrl == null)
                {
                    bossCtrl = wolfObj.AddComponent<BossController>();
                }

                // Gán các tham chiếu cần thiết
                bossCtrl.animator = wolfAnimator;
                bossCtrl.charController = wolfCC;

                // Tải dữ liệu BossData (Ví dụ: ThienLoc)
                BossData bossData = AssetDatabase.LoadAssetAtPath<BossData>("Assets/Data/BossData_ThienLoc.asset");
                if (bossData != null)
                {
                    bossCtrl.data = bossData;
                }
                
                // Đánh dấu để lưu thay đổi trên Sói
                EditorUtility.SetDirty(wolfObj);
                Debug.Log("<color=green>[Tools]</color> Đã tự động cấu hình Boss Sói (Animator, CharacterController, BossController) thành công!");
            }

            // 7.6. Cấu hình LootDropper cho Wolf và Linh Thú Tha Hóa
            GameObject wolfTemplate = FindGameObjectInActiveScene("Wolf");
            if (wolfTemplate == null) wolfTemplate = GameObject.Find("Wolf");
            if (wolfTemplate != null)
            {
                SetupLootDropper(wolfTemplate);
            }

            GameObject linhThuTemplate = FindGameObjectInActiveScene("Linh Thú Tha Hóa");
            if (linhThuTemplate == null) linhThuTemplate = GameObject.Find("Linh Thú Tha Hóa");
            if (linhThuTemplate != null)
            {
                SetupLootDropper(linhThuTemplate);
            }

            // 7.8. Sao chép UI Canvas và các Managers từ GameScene nếu thiếu
            CopyManagersFromGameScene();

            // 8. Tự động sửa cài đặt hoạt ảnh One Hand Club Combo để tránh bị chui xuống đất
            string animPath = "Assets/Animation/One Hand Club Combo.fbx";
            ModelImporter importer = AssetImporter.GetAtPath(animPath) as ModelImporter;
            if (importer != null)
            {
                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }
                
                if (clips != null && clips.Length > 0)
                {
                    clips[0].keepOriginalPositionY = true; // true để giữ nguyên trục Y (tránh chui xuống đất)
                    clips[0].keepOriginalOrientation = false; // false để khớp Body Orientation (tránh quay 90 độ nằm sấp)
                    clips[0].keepOriginalPositionXZ = false; // false để dùng Center of Mass
                    clips[0].lockRootHeightY = true;
                    clips[0].lockRootPositionXZ = true;
                    clips[0].lockRootRotation = true;
                    clips[0].loopTime = false;
                    
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    Debug.Log("<color=green>[Tools]</color> Đã tự động sửa hoạt ảnh chém tránh bị chui xuống đất.");
                }
            }

            // 8.5. Cấu hình transition tự động quay lại trạng thái Run sau khi chém xong để không bị kẹt ở Attack
            string controllerPath = "Assets/Animators/PlayerAnimator.controller";
            var animatorController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
            if (animatorController != null)
            {
                var stateMachine = animatorController.layers[0].stateMachine;
                UnityEditor.Animations.AnimatorState attackState = null;
                UnityEditor.Animations.AnimatorState runState = null;
                
                foreach (var childState in stateMachine.states)
                {
                    if (childState.state.name == "Attack") attackState = childState.state;
                    if (childState.state.name == "Run") runState = childState.state;
                }
                
                if (attackState != null && runState != null)
                {
                    bool hasTransition = false;
                    foreach (var transition in attackState.transitions)
                    {
                        if (transition.destinationState == runState)
                        {
                            hasTransition = true;
                            break;
                        }
                    }
                    
                    if (!hasTransition)
                    {
                        var t = attackState.AddTransition(runState);
                        t.hasExitTime = true;
                        t.exitTime = 0.85f;
                        t.duration = 0.2f;
                        EditorUtility.SetDirty(animatorController);
                        AssetDatabase.SaveAssets();
                        Debug.Log("<color=green>[Tools]</color> Đã tự động thêm transition từ Attack quay lại Run.");
                    }
                }
            }

            // 7.9. Cấu hình LootItem cho vũ khí trên mặt đất để nhặt được
            ConfigureWeaponLootItems(swordPrefab, daggerPrefab);

            // Đánh dấu scene bị thay đổi để Unity cho phép lưu
            EditorUtility.SetDirty(playerMovement);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playerObj.scene);
            
            Debug.Log($"<color=green>[Tools] Thành công!</color> Đã gán kiếm, đặt Tag <b>'Player'</b>, <b>ZOOM gần camera</b>, thiết lập vị trí kiếm cầm tay, sửa lỗi hoạt ảnh chui đất và cấu hình các vũ khí trên mặt đất.");
        }
        else
        {
            Debug.LogError("[Tools] Không tìm thấy trường '_swordPrefab' trong PlayerMovement.cs!");
        }
    }

    private static void ConfigureWeaponLootItems(GameObject swordPrefab, GameObject daggerPrefab)
    {
        int sceneCount = SceneManager.sceneCount;
        int configuredCount = 0;
        
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                Transform[] allChildren = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    GameObject go = t.gameObject;
                    if (EditorUtility.IsPersistent(go)) continue;
                    
                    string nameLower = go.name.ToLower();
                    // Nhận diện kiếm gỗ qua tên "base_low", "wideblade_sword" hoặc "stylized_wooden_sword"
                    if ((nameLower.Contains("base_low") || nameLower.Contains("wideblade_sword") || nameLower.Contains("stylized_wooden_sword")) 
                        && !go.CompareTag("Player") 
                        && !nameLower.Contains("wolf") 
                        && !nameLower.Contains("linh thú")
                        && !nameLower.Contains("u minh"))
                    {
                        LootItem loot = go.GetComponent<LootItem>();
                        if (loot == null)
                        {
                            loot = go.AddComponent<LootItem>();
                        }
                        
                        loot.itemName = "Stylized Wooden Sword";
                        loot.isWeapon = true;
                        loot.weaponPrefab = swordPrefab;
                        loot.equipOffset = new Vector3(0.03f, 0.102f, 0.062f);
                        loot.equipRotation = new Vector3(15.362f, -277.364f, -215.845f);
                        loot.equipScale = new Vector3(0.2f, 0.2f, 0.2f);
                        loot.startImmediately = true;
                        
                        // Đảm bảo có BoxCollider hoặc MeshCollider là Trigger
                        Collider col = go.GetComponent<Collider>();
                        if (col == null)
                        {
                            col = go.AddComponent<BoxCollider>();
                        }
                        col.isTrigger = true;
                        
                        EditorUtility.SetDirty(go);
                        EditorSceneManager.MarkSceneDirty(scene);
                        configuredCount++;
                    }
                    // Nhận diện kiếm đồng (bronze_dagger)
                    else if (nameLower.Contains("bronze_dagger") || nameLower.Contains("bronze_dragger"))
                    {
                        LootItem loot = go.GetComponent<LootItem>();
                        if (loot == null)
                        {
                            loot = go.AddComponent<LootItem>();
                        }
                        
                        loot.itemName = "Bronze Dagger";
                        loot.isWeapon = true;
                        loot.weaponPrefab = daggerPrefab;
                        
                        // Cấu hình offset/rotation/scale cho bronze_dagger
                        // Vì bronze_dagger trong scene có scale 4.34, ta thiết lập equipScale = new Vector3(4.34f, 4.34f, 4.34f)
                        loot.equipOffset = new Vector3(-0.06f, 0.05f, 0.02f);
                        loot.equipRotation = new Vector3(80f, 0f, 0f);
                        loot.equipScale = new Vector3(4.34f, 4.34f, 4.34f);
                        loot.startImmediately = true;
                        
                        // Đảm bảo có Collider là Trigger
                        Collider col = go.GetComponent<Collider>();
                        if (col == null)
                        {
                            col = go.AddComponent<BoxCollider>();
                        }
                        col.isTrigger = true;
                        
                        EditorUtility.SetDirty(go);
                        EditorSceneManager.MarkSceneDirty(scene);
                        configuredCount++;
                    }
                }
            }
        }
        
        Debug.Log($"<color=green>[Tools]</color> Đã tự động cấu hình {configuredCount} vũ khí mặt đất (LootItem, Collider Trigger, Weapon Prefab).");
    }

    private static void CopyManagersFromGameScene()
    {
        string gameScenePath = "Assets/Scenes/GameScene.unity";
        
        // Kiểm tra xem GameScene có tồn tại không
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(gameScenePath) == null)
        {
            Debug.LogWarning("[Tools] Không tìm thấy GameScene.unity tại Assets/Scenes/");
            return;
        }

        // Mở GameScene additively
        Scene activeScene = SceneManager.GetActiveScene();
        Scene gameScene = EditorSceneManager.OpenScene(gameScenePath, OpenSceneMode.Additive);
        
        string[] namesToCopy = { "Canvas", "GameManager", "AudioManager", "ParticleManager", "NetworkManager", "LoginManager", "ObjectPool" };
        
        foreach (string name in namesToCopy)
        {
            // Kiểm tra xem đối tượng đã tồn tại trong Active Scene chưa
            GameObject existingObj = GameObject.Find(name);
            if (existingObj != null && existingObj.scene == activeScene)
            {
                continue; // Đã có rồi, bỏ qua
            }
            
            // Tìm đối tượng trong GameScene
            GameObject sourceObj = null;
            foreach (GameObject go in gameScene.GetRootGameObjects())
            {
                if (go.name == name)
                {
                    sourceObj = go;
                    break;
                }
            }
            
            if (sourceObj != null)
            {
                // Sao chép sang Active Scene
                GameObject newObj = Object.Instantiate(sourceObj);
                newObj.name = name;
                SceneManager.MoveGameObjectToScene(newObj, activeScene);
                Undo.RegisterCreatedObjectUndo(newObj, "Copy " + name);
                Debug.Log($"<color=green>[Tools]</color> Đã tự động sao chép <b>{name}</b> từ GameScene vào scene hiện tại!");
            }
        }
        
        // Đóng GameScene
        EditorSceneManager.CloseScene(gameScene, true);
    }

    private static void SetupLootDropper(GameObject targetObj)
    {
        LootDropper dropper = targetObj.GetComponent<LootDropper>();
        if (dropper == null)
        {
            dropper = targetObj.AddComponent<LootDropper>();
        }

        // Tải các prefab từ Project Assets
        GameObject rudaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Prefabs/Ruda_low.prefab");
        GameObject metalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Prefabs/metal_ore_pack_low.prefab");

        if (rudaPrefab == null || metalPrefab == null)
        {
            Debug.LogWarning($"[Tools] Không tìm thấy các prefab quặng trong Assets để cấu hình LootDropper cho {targetObj.name}!");
            return;
        }

        dropper.possibleDrops.Clear();

        // 1. Cấu hình rớt Thiếc (Ruda_low)
        LootDropper.DropItemConfig rudaConfig = new LootDropper.DropItemConfig();
        rudaConfig.itemName = "Thiếc";
        rudaConfig.itemPrefab = rudaPrefab;
        rudaConfig.dropChance = 1f; // 100% tỉ lệ rơi để dễ test
        rudaConfig.customScale = new Vector3(0.15f, 0.15f, 0.15f);
        dropper.possibleDrops.Add(rudaConfig);

        // 2. Cấu hình rớt Đồng (metal_ore_pack_low)
        LootDropper.DropItemConfig metalConfig = new LootDropper.DropItemConfig();
        metalConfig.itemName = "Đồng";
        metalConfig.itemPrefab = metalPrefab;
        metalConfig.dropChance = 1f; // 100% tỉ lệ rơi để dễ test
        metalConfig.customScale = new Vector3(0.15f, 0.15f, 0.15f);
        dropper.possibleDrops.Add(metalConfig);

        EditorUtility.SetDirty(targetObj);
        Debug.Log($"<color=green>[Tools]</color> Đã cấu hình xong LootDropper (Thiếc + Đồng) cho {targetObj.name}!");
    }

    private static GameObject FindGameObjectInActiveScene(string name)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject rootObj in activeScene.GetRootGameObjects())
        {
            if (rootObj.name == name) return rootObj;
            Transform[] children = rootObj.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child.name == name) return child.gameObject;
            }
        }
        return null;
    }

    [MenuItem("Tools/PRU213/Hide Static Rocks from Scene")]
    public static void HideStaticRocks()
    {
        int hiddenCount = 0;
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                Transform[] allChildren = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    GameObject go = t.gameObject;
                    string nameLower = go.name.ToLower();
                    if (nameLower.Contains("ruda_low") || nameLower.Contains("metal_ore_pack_low") || nameLower.Contains("tripo_convert"))
                    {
                        // Kiểm tra xem đối tượng có phải là asset trong Project không (chỉ xử lý các đối tượng trong scene)
                        if (EditorUtility.IsPersistent(go)) continue;

                        // BỎ QUA nếu là nhân vật (có Animator hoặc SkinnedMeshRenderer hoặc CharacterController)
                        if (go.GetComponentInChildren<Animator>(true) != null || 
                            go.GetComponentInChildren<SkinnedMeshRenderer>(true) != null ||
                            go.GetComponentInChildren<CharacterController>(true) != null)
                        {
                            continue;
                        }

                        if (go.activeSelf)
                        {
                            Undo.RecordObject(go, "Hide Static Rock");
                            go.SetActive(false);
                            EditorUtility.SetDirty(go);
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                            hiddenCount++;
                        }
                    }
                }
            }
        }
        
        Debug.Log($"<color=green>[Tools]</color> Đã ẩn {hiddenCount} cục đá tĩnh (Ruda_low / metal_ore_pack_low / tripo_convert) khỏi scene!");
    }

}
