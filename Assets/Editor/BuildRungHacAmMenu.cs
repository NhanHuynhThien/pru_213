using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class BuildRungHacAmMenu
{
    private struct ResourceConf {
        public Vector3 pos;
        public ResourceNode.ResourceType type;
        public int amount;
        public string name;
    }

    [MenuItem("Tools/Build Rung Hac Am (Map 2)")]
    public static void BuildRungHacAmScene()
    {
        Debug.Log("[BuildRungHacAm] Starting Map 2 build process...");

        // Ensure directory exists
        string sceneDirectory = "Assets/Scene";
        if (!Directory.Exists(sceneDirectory))
        {
            Directory.CreateDirectory(sceneDirectory);
        }

        string scenePath = "Assets/Scene/Rung_Hac_Am.unity";

        // 1. Create a new empty scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Set Up Spooky Night lighting & skybox
        GameObject lightObj = new GameObject("Directional Light (Moonlight)");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        light.color = new Color(0.2f, 0.25f, 0.45f); // Moonlight blue
        light.intensity = 0.35f;
        lightObj.transform.rotation = Quaternion.Euler(45, -45, 0);

        // Global ambient settings
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.15f); // Deep dark purple/blue
        
        // Deep purple fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.04f, 0.03f, 0.08f);
        RenderSettings.fogDensity = 0.02f;

        // Apply Skybox
        string skyboxPath = "Assets/SkythianCat/Glowing_Forest/Materials/Skybox_NIGHT.mat";
        Material nightSkybox = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
        if (nightSkybox != null)
        {
            RenderSettings.skybox = nightSkybox;
        }
        else
        {
            Debug.LogWarning($"[BuildRungHacAm] Skybox not found at path: {skyboxPath}");
        }

        // 3. Create Main Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0, 15, -75);
        camObj.transform.rotation = Quaternion.Euler(20, 0, 0);

        // 4. Create Terrain (240x240) and save as Asset
        TerrainData terrainData = new TerrainData();
        terrainData.size = new Vector3(240, 30, 240);
        terrainData.heightmapResolution = 129; // Optimized size
        terrainData.alphamapResolution = 128;  // Optimized size

        // Save TerrainData asset to disk to optimize space
        string terrainAssetPath = "Assets/Map_2/Rung_Hac_Am_Terrain.asset";
        string map2Dir = "Assets/Map_2";
        if (!Directory.Exists(map2Dir))
        {
            Directory.CreateDirectory(map2Dir);
        }
        AssetDatabase.DeleteAsset(terrainAssetPath);
        AssetDatabase.CreateAsset(terrainData, terrainAssetPath);
        AssetDatabase.SaveAssets();

        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.name = "Terrain_RungHacAm";
        terrainObj.transform.position = new Vector3(-120, 0, -120);
        int groundLyr = LayerMask.NameToLayer("Ground");
        terrainObj.layer = (groundLyr != -1) ? groundLyr : 0;

        // Set Terrain Layers
        TerrainLayer groundLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Silver_Cats/Hand_Painted_Nature_Kit_LITE/Terrain_Layers/Layer_Forest_Ground.terrainlayer");
        TerrainLayer grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Silver_Cats/Hand_Painted_Nature_Kit_LITE/Terrain_Layers/Layer_Forest_Grass_01.terrainlayer");
        TerrainLayer roadLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Silver_Cats/Hand_Painted_Nature_Kit_LITE/Terrain_Layers/Layer_Forest_Road_01.terrainlayer");

        if (groundLayer != null && grassLayer != null && roadLayer != null)
        {
            terrainData.terrainLayers = new TerrainLayer[] { groundLayer, grassLayer, roadLayer };
            
            // Paint paths and swamp area
            int splatWidth = terrainData.alphamapWidth;
            int splatHeight = terrainData.alphamapHeight;
            float[,,] splatMapData = new float[splatWidth, splatHeight, 3];

            for (int y = 0; y < splatHeight; y++)
            {
                for (int x = 0; x < splatWidth; x++)
                {
                    float normX = (float)x / (splatWidth - 1);
                    float normY = (float)y / (splatHeight - 1);
                    float dx = normX - 0.5f;
                    float dy = normY - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float w0 = 1f; // Ground
                    float w1 = 0f; // Grass
                    float w2 = 0f; // Road

                    bool isRoad = false;

                    // Road from South Gate to Center
                    if (Mathf.Abs(normX - 0.5f) < 0.04f && normY < 0.5f && normY > 0.15f) isRoad = true;
                    // Road from Center to North Altar
                    if (Mathf.Abs(normX - 0.5f) < 0.04f && normY >= 0.5f && normY < 0.85f) isRoad = true;
                    
                    // Road from Center to South-West Village
                    float t = (normY - 0.25f) / 0.25f;
                    if (t >= 0 && t <= 1)
                    {
                        float expectedX = Mathf.Lerp(0.2f, 0.5f, t);
                        if (Mathf.Abs(normX - expectedX) < 0.04f) isRoad = true;
                    }

                    if (isRoad)
                    {
                        w0 = 0.1f;
                        w2 = 0.9f;
                    }

                    splatMapData[x, y, 0] = w0;
                    splatMapData[x, y, 1] = w1;
                    splatMapData[x, y, 2] = w2;
                }
            }
            terrainData.SetAlphamaps(0, 0, splatMapData);
        }

        // Add Terrain Collider
        TerrainCollider tc = terrainObj.GetComponent<TerrainCollider>();
        if (tc == null)
        {
            tc = terrainObj.AddComponent<TerrainCollider>();
        }
        tc.terrainData = terrainData;

        // 5. Create Map Structure Group
        GameObject structureGroup = new GameObject("Environment_Structures");
        structureGroup.transform.position = Vector3.zero;

        // 6. ZONE 1: ĐẦM LINH QUY (Swamp / Lake - Center)
        GameObject damLinhQuy = new GameObject("Dam_Linh_Quy");
        damLinhQuy.transform.parent = structureGroup.transform;
        
        // We removed the water and the bridge as they are no longer needed. We keep the mushrooms.

        // Scatter glowing mushrooms and lanterns around swamp
        string[] shrooms = {
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Plants/Mushrooms01.prefab",
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Plants/Mushrooms02.prefab",
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Plants/Mushrooms03.prefab"
        };
        for (int i = 0; i < 16; i++)
        {
            float angle = i * (360f / 16f);
            float radius = 14f + UnityEngine.Random.Range(-2f, 2f);
            float rx = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float rz = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;

            string shroomPath = shrooms[UnityEngine.Random.Range(0, shrooms.Length)];
            GameObject shroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shroomPath);
            if (shroomPrefab != null)
            {
                GameObject shroom = (GameObject)PrefabUtility.InstantiatePrefab(shroomPrefab, damLinhQuy.transform);
                shroom.transform.position = new Vector3(rx, 0, rz);
                shroom.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                shroom.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.3f);
                PhysicsLogicUtility.ApplyPhysicalLogic(shroom);
            }
        }

        // Add Swamp Spawner
        GameObject spawnerDam = new GameObject("Spawner_DamLinhQuy");
        spawnerDam.transform.position = new Vector3(0f, 0.5f, 12f);
        spawnerDam.transform.parent = structureGroup.transform;
        ConfigureSpawner(spawnerDam, 6, 2, 3.5f, 12f);

        // 7. ZONE 2: ĐÀN TẾ THẦN KIM QUY (Altar - North)
        GameObject danTe = new GameObject("Dan_Te_Than_Kim_Quy");
        danTe.transform.parent = structureGroup.transform;

        GameObject pavilionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Namhansanseong/Prefabs/Buildings/Jisudang_Pavilion.prefab");
        if (pavilionPrefab != null)
        {
            GameObject pavilion = (GameObject)PrefabUtility.InstantiatePrefab(pavilionPrefab, danTe.transform);
            pavilion.transform.position = new Vector3(0f, 0.1f, 60f);
            pavilion.transform.rotation = Quaternion.Euler(0, 180, 0); // Face south towards swamp
            pavilion.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            PhysicsLogicUtility.ApplyPhysicalLogic(pavilion);
        }

        // We removed the stone walls around the altar as they are no longer needed.

        // Add glowing stone lanterns around Altar
        GameObject lightOnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SkythianCat/Glowing_Forest/Prefabs/Props/Streetlight_ON.prefab");
        if (lightOnPrefab != null)
        {
            Vector3[] lightPositions = {
                new Vector3(-6f, 0f, 52f),
                new Vector3(6f, 0f, 52f),
                new Vector3(-10f, 0f, 68f),
                new Vector3(10f, 0f, 68f)
            };
            foreach (var pos in lightPositions)
            {
                GameObject lantern = (GameObject)PrefabUtility.InstantiatePrefab(lightOnPrefab, danTe.transform);
                lantern.transform.position = pos;
                PhysicsLogicUtility.ApplyPhysicalLogic(lantern);
            }
        }

        // Add Altar Spawner (High tier)
        GameObject spawnerTe = new GameObject("Spawner_DanTe");
        spawnerTe.transform.position = new Vector3(0f, 0.5f, 60f);
        spawnerTe.transform.parent = structureGroup.transform;
        ConfigureSpawner(spawnerTe, 5, 3, 4.5f, 15f);

        // 8. ZONE 3: PHẾ TÍCH LÀNG CỔ (Ancient Village - South West)
        GameObject langCo = new GameObject("Phe_Tich_Lang_Co");
        langCo.transform.parent = structureGroup.transform;

        // Decayed bamboo house 1
        GameObject house1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_01.prefab");
        if (house1Prefab != null)
        {
            GameObject house1 = (GameObject)PrefabUtility.InstantiatePrefab(house1Prefab, langCo.transform);
            house1.transform.position = new Vector3(-55f, 0.1f, -40f);
            house1.transform.rotation = Quaternion.Euler(0, 45, 0);
            house1.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            PhysicsLogicUtility.ApplyPhysicalLogic(house1);
        }

        // Decayed bamboo house 2
        GameObject house2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_02.prefab");
        if (house2Prefab != null)
        {
            GameObject house2 = (GameObject)PrefabUtility.InstantiatePrefab(house2Prefab, langCo.transform);
            house2.transform.position = new Vector3(-40f, 0.1f, -55f);
            house2.transform.rotation = Quaternion.Euler(0, 135, 0);
            house2.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            PhysicsLogicUtility.ApplyPhysicalLogic(house2);
        }

        // Enclosing fences
        GameObject fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SkythianCat/Glowing_Forest/Prefabs/Props/Fence.prefab");
        if (fencePrefab != null)
        {
            Vector3[] fencePositions = {
                new Vector3(-48f, 0f, -32f),
                new Vector3(-35f, 0f, -48f)
            };
            float[] fenceRotations = { -45f, 45f };
            for (int i = 0; i < fencePositions.Length; i++)
            {
                GameObject fence = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, langCo.transform);
                fence.transform.position = fencePositions[i];
                fence.transform.rotation = Quaternion.Euler(0, fenceRotations[i], 0);
                PhysicsLogicUtility.ApplyPhysicalLogic(fence);
            }
        }

        // Place resource nodes around village
        GameObject resourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ResourceNode.prefab");
        if (resourcePrefab != null)
        {
            List<ResourceConf> resourcesToSpawn = new List<ResourceConf> {
                new ResourceConf { pos = new Vector3(-68f, 0.2f, -48f), type = ResourceNode.ResourceType.Copper, amount = 5, name = "CopperNode_Farming1" },
                new ResourceConf { pos = new Vector3(-58f, 0.2f, -32f), type = ResourceNode.ResourceType.Copper, amount = 5, name = "CopperNode_Farming2" },
                new ResourceConf { pos = new Vector3(-35f, 0.2f, -38f), type = ResourceNode.ResourceType.Tin, amount = 3, name = "TinNode_Farming1" },
                new ResourceConf { pos = new Vector3(-62f, 0.2f, -58f), type = ResourceNode.ResourceType.Tin, amount = 3, name = "TinNode_Farming2" },
                new ResourceConf { pos = new Vector3(-48f, 0.2f, -28f), type = ResourceNode.ResourceType.Bronze, amount = 2, name = "BronzeNode_Farming1" },
                new ResourceConf { pos = new Vector3(-72f, 0.2f, -38f), type = ResourceNode.ResourceType.TurtleShell, amount = 2, name = "TurtleShellNode_Farming1" } // Sacred turtle shells!
            };

            foreach (var rc in resourcesToSpawn)
            {
                GameObject node = (GameObject)PrefabUtility.InstantiatePrefab(resourcePrefab, langCo.transform);
                node.name = rc.name;
                node.transform.position = rc.pos;
                PhysicsLogicUtility.ApplyPhysicalLogic(node);

                // Configure node settings
                ResourceNode rn = node.GetComponent<ResourceNode>();
                if (rn != null)
                {
                    rn.type = rc.type;
                    rn.amount = rc.amount;
                    rn.respawnTime = 10f;
                }
            }
        }

        // Add Village Spawner
        GameObject spawnerLang = new GameObject("Spawner_LangCo");
        spawnerLang.transform.position = new Vector3(-50f, 0.5f, -45f);
        spawnerLang.transform.parent = structureGroup.transform;
        ConfigureSpawner(spawnerLang, 5, 2, 4f, 15f);

        // 9. SCATTER GENERAL MAJESTIC/DARK FOREST TREES, ROCKS, FOLILAGE
        // Set up list of assets
        string[] forestTrees = {
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Plants/Fir.prefab",
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Plants/Oak.prefab",
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Plants/Birch.prefab",
            "Assets/Silver_Cats/Hand_Painted_Nature_Kit_LITE/Prefabs/Cedar_Tree_03.prefab",
            "Assets/Silver_Cats/Hand_Painted_Nature_Kit_LITE/Prefabs/Larch_Tree.prefab",
            "Assets/Silver_Cats/Hand_Painted_Nature_Kit_LITE/Prefabs/Pine_Tree.prefab",
            "Assets/JP Environmental Asset Pack/Prefabs/Tree 1.prefab",
            "Assets/JP Environmental Asset Pack/Prefabs/Tree 2.prefab",
            "Assets/JP Environmental Asset Pack/Prefabs/Tree 3.prefab",
            "Assets/JP Environmental Asset Pack/Prefabs/Tree 4.prefab"
        };

        string[] forestBushes = {
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Plants/Bush01.prefab",
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Plants/Bush02.prefab",
            "Assets/Silver_Cats/Hand_Painted_Nature_Kit_LITE/Prefabs/Fern.prefab",
            "Assets/Silver_Cats/Hand_Painted_Nature_Kit_LITE/Prefabs/Plantain.prefab",
            "Assets/JP Environmental Asset Pack/Prefabs/Foliage 1.prefab",
            "Assets/JP Environmental Asset Pack/Prefabs/Foliage 2.prefab"
        };

        string[] forestRocks = {
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Stone01.prefab",
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Stone02.prefab",
            "Assets/SkythianCat/Glowing_Forest/Prefabs/Stump.prefab",
            "Assets/JP Environmental Asset Pack/Prefabs/Large Rock 1.prefab",
            "Assets/JP Environmental Asset Pack/Prefabs/Rock Group.prefab"
        };

        List<GameObject> treePrefabs = LoadPrefabs(forestTrees);
        List<GameObject> bushPrefabs = LoadPrefabs(forestBushes);
        List<GameObject> rockPrefabs = LoadPrefabs(forestRocks);

        GameObject forestGroup = new GameObject("Forest_Objects");
        forestGroup.transform.position = Vector3.zero;

        System.Random rnd = new System.Random(999);
        int scatteredCount = 0;

        // Loop to scatter objects
        for (float x = -115; x <= 115; x += 6.5f)
        {
            for (float z = -115; z <= 115; z += 6.5f)
            {
                // Vector2 locations
                Vector2 pos2D = new Vector2(x, z);
                
                // Clearances for key gameplay zones
                if (Vector2.Distance(pos2D, Vector2.zero) < 22f) continue; // Center
                if (Vector2.Distance(pos2D, new Vector2(0f, 60f)) < 24f) continue; // Altar
                if (Vector2.Distance(pos2D, new Vector2(-50f, -45f)) < 30f) continue; // Village
                if (Vector2.Distance(pos2D, new Vector2(0f, -80f)) < 12f) continue; // Spawn point
                
                // Density factor based on distance to center (increase near borders)
                float distToCenter = pos2D.magnitude;
                float borderFactor = distToCenter / 120f; // 0 at center, 1 at edge
                float density = Mathf.Lerp(0.08f, 0.85f, borderFactor);

                if (rnd.NextDouble() < density)
                {
                    float px = x + (float)(rnd.NextDouble() * 4f - 2f);
                    float pz = z + (float)(rnd.NextDouble() * 4f - 2f);
                    Vector3 pos = new Vector3(px, 0f, pz);

                    Quaternion rot = Quaternion.Euler(0, (float)(rnd.NextDouble() * 360f), 0);
                    float scale = (float)(rnd.NextDouble() * 0.4f + 0.8f);

                    double roll = rnd.NextDouble();
                    GameObject selectedPrefab;
                    bool isTree = false;

                    if (roll < 0.55 && treePrefabs.Count > 0)
                    {
                        selectedPrefab = treePrefabs[rnd.Next(treePrefabs.Count)];
                        isTree = true;
                    }
                    else if (roll < 0.85 && bushPrefabs.Count > 0)
                    {
                        selectedPrefab = bushPrefabs[rnd.Next(bushPrefabs.Count)];
                    }
                    else if (rockPrefabs.Count > 0)
                    {
                        selectedPrefab = rockPrefabs[rnd.Next(rockPrefabs.Count)];
                    }
                    else
                    {
                        continue;
                    }

                    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab, forestGroup.transform);
                    obj.transform.position = pos;
                    obj.transform.rotation = rot;
                    obj.transform.localScale = Vector3.one * scale;

                    PhysicsLogicUtility.ApplyPhysicalLogic(obj, isTree);
                    scatteredCount++;
                }
            }
        }
        Debug.Log($"[BuildRungHacAm] Scattered {scatteredCount} environmental assets.");

        // 10. SETUP SCENE SETUP & PLAYER SPAWN POINT
        GameObject spawnPointObj = new GameObject("PlayerSpawnPoint");
        spawnPointObj.transform.position = new Vector3(0f, 1f, -80f);

        // Clean up any existing game cameras in the scene to prevent conflicts
        Camera[] oldCams = Object.FindObjectsOfType<Camera>();
        foreach (var oldCam in oldCams)
        {
            if (oldCam.gameObject != null && oldCam.gameObject.name != "SceneCamera")
            {
                Object.DestroyImmediate(oldCam.gameObject);
            }
        }

        // Instantiate Player statically in the scene using the project's standard SetupPlayer tool
        SetupPlayerMenu.SetupPlayer();
        GameObject playerInstance = GameObject.Find("Player");
        if (playerInstance != null)
        {
            playerInstance.tag = "Player"; // Set tag so CameraController can find it!
            playerInstance.transform.position = spawnPointObj.transform.position;
            playerInstance.transform.rotation = Quaternion.identity;
        }
        
        // Also keep a reference to Player.prefab for SceneSetup script just in case
        GameObject playerPref = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");

        GameObject sceneSetupObj = new GameObject("SceneSetup");
        SceneSetup ss = sceneSetupObj.AddComponent<SceneSetup>();
        
        // Find managers and prefabs dynamically to assign
        ss.gameManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab");
        ss.audioManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab");
        ss.particleManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ParticleManager.prefab");
        ss.networkManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NetworkManager.prefab");
        ss.loginManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LoginManager.prefab");
        
        ss.playerPrefab = playerPref;
        ss.playerSpawnPoint = spawnPointObj.transform;
        ss.directionalLight = light;
        ss.skyboxMaterial = nightSkybox;

        // Save scene
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"[BuildRungHacAm] Scene saved successfully at: {scenePath}");

        // Add Scene to Build Settings automatically
        AddSceneToBuildSettings(scenePath);
    }

    private static void ConfigureSpawner(GameObject spawnerObj, int maxEnemies, int tier, float interval, float radius)
    {
        EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();
        spawner.maxEnemies = maxEnemies;
        spawner.currentTier = tier;
        spawner.spawnInterval = interval;
        spawner.spawnRadius = radius;
        spawner.enemyPrefabs = new List<EnemyData>();
        
        // Distance Trigger to enable/disable based on player range
        spawnerObj.AddComponent<SpawnerDistanceTrigger>();

        string[] enemyDataPaths = {
            "Assets/Data/EnemyData_UMinhBinh.asset",
            "Assets/Data/EnemyData_UMinhCai.asset",
            "Assets/Data/EnemyData_UMinhSat.asset"
        };

        foreach (string path in enemyDataPaths)
        {
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null)
            {
                spawner.enemyPrefabs.Add(data);
            }
        }
    }

    private static List<GameObject> LoadPrefabs(string[] paths)
    {
        List<GameObject> list = new List<GameObject>();
        foreach (string path in paths)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) list.Add(go);
        }
        return list;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        
        // Check if scene is already in settings
        bool alreadyAdded = false;
        foreach (var s in scenes)
        {
            if (s.path.Equals(scenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                alreadyAdded = true;
                break;
            }
        }

        if (!alreadyAdded)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[BuildRungHacAm] Added scene '{scenePath}' to Build Settings.");
        }
    }
}
