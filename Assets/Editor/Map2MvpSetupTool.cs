using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class Map2MvpSetupTool
{
    private const string Map2ScenePath = "Assets/Scene/Rung_Hac_Am.unity";
    private const string Map1ScenePath = "Assets/Scene/CoLoa_Forest_Map.unity";
    private static readonly Vector3 PlayerSpawnXZ = new(0f, 0f, -80f);

    [MenuItem("Tools/Map 2/Auto Setup Rung Hac Am MVP")]
    public static void AutoSetupMap2()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Map2Setup] Thoat Play Mode truoc khi chay tool.");
            return;
        }

        if (SceneManager.GetActiveScene().path.Replace("\\", "/") != Map2ScenePath)
        {
            bool openMap = EditorUtility.DisplayDialog(
                "Mo Map 2?",
                "Tool nay nen chay trong scene Rung_Hac_Am. Ban co muon mo scene Map 2 bay gio khong?",
                "Mo Map 2",
                "Huy"
            );

            if (!openMap) return;
            EditorSceneManager.OpenScene(Map2ScenePath, OpenSceneMode.Single);
        }

        GameObject systemGroup = EnsureGroup("Map2_System");
        GameObject playerGroup = EnsureGroup("Map2_Player");
        GameObject enemyGroup = EnsureGroup("Map2_Enemies");
        GameObject spawnerGroup = EnsureGroup("Map2_Spawners");
        GameObject lootGroup = EnsureGroup("Map2_Loot");
        GameObject portalGroup = EnsureGroup("Map2_Portals");
        EnsureGroup("Map2_Debug");

        Material enemyMaterial = EnsureEnemyMaterial();
        ConfigureCombatStats();
        FixEnemyPrefabs(enemyMaterial);
        GameObject player = SetupPlayer(playerGroup.transform);
        SetupCamera(player);
        SetupManagers(systemGroup.transform);
        SetupCombatHud(systemGroup.transform);
        SetupPlayerSpawnPoint(playerGroup.transform);
        SetupTestEnemy(enemyGroup.transform, player, enemyMaterial);
        SetupBossTest(enemyGroup.transform, player, enemyMaterial);
        SetupSpawner(spawnerGroup.transform);
        SetupResourceNode(lootGroup.transform);
        SetupPortal(portalGroup.transform);
        AddScenesToBuildSettings();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[Map2Setup]</color> Da setup Map 2 MVP xong. Hay bam Play de test: Player -> quai duoi -> danh quai -> roi loot -> portal ve Co Loa.");
    }

    private static GameObject EnsureGroup(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing;

        GameObject group = new(name);
        group.transform.position = Vector3.zero;
        return group;
    }

    private static GameObject SetupPlayer(Transform playerGroup)
    {
        GameObject player = FindRealPlayer();

        if (player == null)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.name = "Player";
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                UnityEngine.Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
                player.AddComponent<CharacterController>();
            }
        }

        GameObject oldParentNamedPlayer = player.transform.parent != null ? player.transform.parent.gameObject : null;
        if (oldParentNamedPlayer != null &&
            oldParentNamedPlayer != player &&
            oldParentNamedPlayer.name == "Player" &&
            oldParentNamedPlayer.GetComponent<CharacterController>() == null)
        {
            oldParentNamedPlayer.name = "PlayerRoot";
            TrySetTag(oldParentNamedPlayer, "Untagged");
        }

        player.name = "Player";
        TrySetTag(player, "Player");
        player.transform.SetParent(playerGroup, true);

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null) controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.4f;
        controller.center = new Vector3(0f, 1f, 0f);
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.08f;

        DisableExtraRigidbodies(player);

        Vector3 spawn = GetGroundedPosition(PlayerSpawnXZ.x, PlayerSpawnXZ.z, 0.05f);
        player.transform.position = spawn;
        player.transform.rotation = Quaternion.identity;
        player.transform.localScale = Vector3.one;

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.characterController = controller;
            if (playerController.stats == null)
            {
                playerController.stats = AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/Data/PlayerStats.asset");
            }

            playerController.attackRange = 2.7f;
            playerController.attackRadius = 1.6f;

            // Map 2 dung PlayerMovement lam motor chinh de tranh hai script cung Move mot CharacterController.
            playerController.enabled = false;
        }

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement == null) playerMovement = player.AddComponent<PlayerMovement>();
        playerMovement.enabled = true;
        AssignDefaultSword(playerMovement);

        PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
        if (playerCombat == null) playerCombat = player.AddComponent<PlayerCombat>();
        if (playerCombat != null && playerCombat.stats == null)
        {
            playerCombat.stats = AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/Data/PlayerStats.asset");
        }
        if (playerCombat != null && playerCombat.stats != null)
        {
            playerCombat.currentHealth = playerCombat.stats.EffectiveMaxHealth;
        }

        Map2HealthBar playerHealthBar = player.GetComponent<Map2HealthBar>();
        if (playerHealthBar == null) playerHealthBar = player.AddComponent<Map2HealthBar>();
        playerHealthBar.offset = new Vector3(0f, 2.35f, 0f);
        playerHealthBar.size = new Vector2(1.5f, 0.16f);

        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);
        return player;
    }

    private static GameObject FindRealPlayer()
    {
        List<GameObject> sceneObjects = GetSceneGameObjects();

        foreach (GameObject obj in sceneObjects)
        {
            if (obj.CompareTagSafe("Player") && obj.GetComponent<CharacterController>() != null)
                return obj;
        }

        foreach (GameObject obj in sceneObjects)
        {
            if (obj.GetComponent<CharacterController>() != null &&
                (obj.GetComponent<PlayerMovement>() != null || obj.GetComponent<PlayerController>() != null))
                return obj;
        }

        foreach (GameObject obj in sceneObjects)
        {
            if (obj.CompareTagSafe("Player")) return obj;
        }

        return GameObject.Find("Player");
    }

    private static void SetupCamera(GameObject player)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        }

        if (cam == null)
        {
            GameObject camObj = new("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        cam.gameObject.name = "Main Camera";
        TrySetTag(cam.gameObject, "MainCamera");
        cam.enabled = true;
        cam.targetDisplay = 0;
        cam.clearFlags = CameraClearFlags.Skybox;

        if (cam.GetComponent<AudioListener>() == null)
        {
            cam.gameObject.AddComponent<AudioListener>();
        }

        if (player != null)
        {
            cam.transform.SetParent(player.transform, false);
            cam.transform.localPosition = new Vector3(0f, 1.4f, -4f);
            cam.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.cameraTransform = cam.transform;
            }
        }
    }

    private static void SetupManagers(Transform systemGroup)
    {
        EnsureManagerPrefab("Assets/Prefabs/GameManager.prefab", "GameManager", systemGroup);
        EnsureManagerPrefab("Assets/Prefabs/AudioManager.prefab", "AudioManager", systemGroup);
        EnsureManagerPrefab("Assets/Prefabs/ParticleManager.prefab", "ParticleManager", systemGroup);
    }

    private static void SetupCombatHud(Transform systemGroup)
    {
        GameObject hud = GameObject.Find("Map2_CombatHUD");
        if (hud == null) hud = new GameObject("Map2_CombatHUD");
        hud.transform.SetParent(systemGroup, true);

        Map2CombatHUD combatHud = hud.GetComponent<Map2CombatHUD>();
        if (combatHud == null) hud.AddComponent<Map2CombatHUD>();
    }

    private static void EnsureManagerPrefab(string prefabPath, string fallbackName, Transform parent)
    {
        if (GameObject.Find(fallbackName) != null || GameObject.Find(fallbackName + "(Clone)") != null) return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject obj = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : new GameObject(fallbackName);
        obj.name = fallbackName;
        obj.transform.SetParent(parent, true);
    }

    private static void SetupPlayerSpawnPoint(Transform playerGroup)
    {
        GameObject spawn = GameObject.Find("PlayerSpawnPoint");
        if (spawn == null) spawn = new GameObject("PlayerSpawnPoint");

        spawn.transform.SetParent(playerGroup, true);
        spawn.transform.position = GetGroundedPosition(PlayerSpawnXZ.x, PlayerSpawnXZ.z, 0.05f);
        spawn.transform.rotation = Quaternion.identity;
    }

    private static void SetupTestEnemy(Transform enemyGroup, GameObject player, Material enemyMaterial)
    {
        SetupSceneEnemy(
            "Enemy_UMinhBinh_Test",
            "Assets/Prefabs/Enemy_UMinhBinh.prefab",
            "Assets/model/U Minh Binh.fbx",
            "Assets/Data/EnemyData_UMinhBinh.asset",
            enemyGroup,
            player,
            enemyMaterial,
            0f,
            -68f,
            2.1f,
            1
        );

        SetupSceneEnemy(
            "Enemy_UMinhCai_Test",
            "Assets/Prefabs/Enemy_UMinhCai.prefab",
            "Assets/model/Linh Thú Tha Hóa.fbx",
            "Assets/Data/EnemyData_UMinhCai.asset",
            enemyGroup,
            player,
            enemyMaterial,
            10f,
            -58f,
            1.8f,
            1
        );

        SetupSceneEnemy(
            "Enemy_UMinhSat_Test",
            "Assets/Prefabs/Enemy_UMinhSat.prefab",
            "Assets/model/U Minh Tướng.fbx",
            "Assets/Data/EnemyData_UMinhSat.asset",
            enemyGroup,
            player,
            enemyMaterial,
            -12f,
            -58f,
            2.4f,
            1
        );
    }

    private static void SetupBossTest(Transform enemyGroup, GameObject player, Material enemyMaterial)
    {
        GameObject boss = GameObject.Find("Boss_UMinhTuong_Test");
        if (boss == null)
        {
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss.prefab");
            boss = bossPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab) : new GameObject("Boss_UMinhTuong_Test");
        }

        boss.name = "Boss_UMinhTuong_Test";
        TrySetTag(boss, "Boss");
        boss.transform.SetParent(enemyGroup, true);
        boss.transform.position = GetGroundedPosition(18f, -42f, 0.05f);
        boss.transform.rotation = Quaternion.identity;
        boss.transform.localScale = Vector3.one;

        Rigidbody rb = boss.GetComponent<Rigidbody>();
        if (rb != null)
        {
            UnityEngine.Object.DestroyImmediate(rb);
        }

        for (int i = boss.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(boss.transform.GetChild(i).gameObject);
        }

        SetupProperEnemyVisual(boss, "Assets/model/U Minh Tướng.fbx", enemyMaterial, 2.8f);

        CharacterController characterController = boss.GetComponent<CharacterController>();
        if (characterController == null) characterController = boss.AddComponent<CharacterController>();
        characterController.height = 2.8f;
        characterController.radius = 0.65f;
        characterController.center = new Vector3(0f, 1.4f, 0f);
        characterController.stepOffset = 0.35f;
        characterController.skinWidth = 0.08f;

        BossController bossController = boss.GetComponent<BossController>();
        if (bossController == null) bossController = boss.AddComponent<BossController>();
        bossController.data = AssetDatabase.LoadAssetAtPath<BossData>("Assets/Data/BossData_TanThuong.asset");
        bossController.requiredTier = 1;
        bossController.target = player != null ? player.transform : null;
        bossController.charController = characterController;
        bossController.animator = boss.GetComponentInChildren<Animator>();
        bossController.detectionRange = 24f;
        bossController.attackRange = 3f;
        bossController.moveSpeed = 2.8f;
        bossController.chaseSpeed = 3.6f;
        bossController.attackDamage = 18f;
        bossController.attackCooldown = 2f;
        bossController.maxHealth = bossController.data != null ? bossController.data.GetScaledHealth(1) : 420f;
        bossController.currentHealth = bossController.maxHealth;

        Map2HealthBar healthBar = boss.GetComponent<Map2HealthBar>();
        if (healthBar == null) healthBar = boss.AddComponent<Map2HealthBar>();
        healthBar.offset = new Vector3(0f, 3.25f, 0f);
        healthBar.size = new Vector2(2.4f, 0.22f);
    }

    private static void SetupSceneEnemy(
        string name,
        string prefabPath,
        string modelPath,
        string dataPath,
        Transform enemyGroup,
        GameObject player,
        Material enemyMaterial,
        float x,
        float z,
        float desiredHeight,
        int tier)
    {
        GameObject enemy = GameObject.Find(name);
        if (enemy == null)
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (enemyPrefab != null)
            {
                enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
            }
            else
            {
                enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.AddComponent<NavMeshAgent>();
                enemy.AddComponent<EnemyController>();
            }
        }

        enemy.name = name;
        TrySetTag(enemy, "Enemy");
        enemy.transform.SetParent(enemyGroup, true);
        enemy.transform.position = GetGroundedPosition(x, z, 0.02f);
        enemy.transform.rotation = Quaternion.identity;
        enemy.transform.localScale = Vector3.one;

        SetupProperEnemyVisual(enemy, modelPath, enemyMaterial, desiredHeight);

        if (enemy.GetComponent<Collider>() == null)
        {
            CapsuleCollider collider = enemy.AddComponent<CapsuleCollider>();
            collider.radius = 0.5f;
            collider.height = 2f;
            collider.center = new Vector3(0f, 1f, 0f);
        }

        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent == null) agent = enemy.AddComponent<NavMeshAgent>();
        agent.speed = 3.6f;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        EnemyData sceneEnemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
        agent.stoppingDistance = sceneEnemyData != null ? Mathf.Max(1.1f, sceneEnemyData.attackRange * 0.8f) : 1.6f;
        agent.radius = 0.5f;
        agent.height = 2f;

        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller == null) controller = enemy.AddComponent<EnemyController>();
        controller.data = sceneEnemyData;
        controller.currentTier = tier;
        controller.sightRange = controller.data != null ? controller.data.sightRange : 14f;
        controller.chaseRange = controller.data != null ? controller.data.chaseRange : 22f;
        controller.attackRange = controller.data != null ? controller.data.attackRange : 2f;
        controller.target = player != null ? player.transform : null;
        controller.animator = enemy.GetComponentInChildren<Animator>();
        controller.ApplyTierScaling();
        controller.currentHealth = controller.data != null ? controller.data.GetScaledHealth(controller.currentTier) : 50f;

        LootDropper lootDropper = enemy.GetComponent<LootDropper>();
        if (lootDropper == null) lootDropper = enemy.AddComponent<LootDropper>();
        ConfigureDefaultDrops(lootDropper);

        Map2HealthBar healthBar = enemy.GetComponent<Map2HealthBar>();
        if (healthBar == null) healthBar = enemy.AddComponent<Map2HealthBar>();
        healthBar.offset = new Vector3(0f, desiredHeight + 0.35f, 0f);
    }

    private static void ConfigureDefaultDrops(LootDropper lootDropper)
    {
        if (lootDropper == null) return;

        GameObject copperLoot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/metal_ore_pack_low.prefab");
        if (copperLoot != null)
        {
            lootDropper.possibleDrops = new List<LootDropper.DropItemConfig>
            {
                new()
                {
                    itemName = "Dong Tho",
                    itemPrefab = copperLoot,
                    dropChance = 1f,
                    customScale = new Vector3(0.2f, 0.2f, 0.2f)
                }
            };
        }
    }

    private static void SetupSpawner(Transform spawnerGroup)
    {
        SetupSpawnerObject(
            "Spawner_BiaRung",
            spawnerGroup,
            0f,
            -45f,
            1,
            4,
            4f,
            12f,
            35f,
            "Assets/Data/EnemyData_UMinhBinh.asset"
        );

        SetupSpawnerObject(
            "Spawner_DamLinhQuy",
            spawnerGroup,
            0f,
            12f,
            2,
            5,
            5f,
            15f,
            40f,
            "Assets/Data/EnemyData_UMinhBinh.asset",
            "Assets/Data/EnemyData_UMinhCai.asset"
        );

        SetupSpawnerObject(
            "Spawner_DanTe",
            spawnerGroup,
            0f,
            60f,
            3,
            5,
            6f,
            16f,
            45f,
            "Assets/Data/EnemyData_UMinhCai.asset",
            "Assets/Data/EnemyData_UMinhSat.asset"
        );
    }

    private static void SetupSpawnerObject(
        string name,
        Transform spawnerGroup,
        float x,
        float z,
        int tier,
        int maxEnemies,
        float interval,
        float radius,
        float activationDistance,
        params string[] enemyDataPaths)
    {
        GameObject spawnerObj = GameObject.Find(name);
        if (spawnerObj == null) spawnerObj = new GameObject("Spawner_BiaRung");
        spawnerObj.name = name;

        spawnerObj.transform.SetParent(spawnerGroup, true);
        spawnerObj.transform.position = GetGroundedPosition(x, z, 0.05f);

        EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
        if (spawner == null) spawner = spawnerObj.AddComponent<EnemySpawner>();
        spawner.currentTier = tier;
        spawner.maxEnemies = maxEnemies;
        spawner.spawnInterval = interval;
        spawner.spawnRadius = radius;
        spawner.enemyPrefabs = new List<EnemyData>();

        foreach (string dataPath in enemyDataPaths)
        {
            EnemyData enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
            if (enemyData != null) spawner.enemyPrefabs.Add(enemyData);
        }

        SpawnerDistanceTrigger trigger = spawnerObj.GetComponent<SpawnerDistanceTrigger>();
        if (trigger == null) trigger = spawnerObj.AddComponent<SpawnerDistanceTrigger>();
        trigger.activationDistance = activationDistance;
    }

    private static void SetupResourceNode(Transform lootGroup)
    {
        GameObject resource = GameObject.Find("Resource_Copper_BiaRung");
        if (resource == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ResourceNode.prefab");
            resource = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : new GameObject("Resource_Copper_BiaRung");
        }

        resource.name = "Resource_Copper_BiaRung";
        resource.transform.SetParent(lootGroup, true);
        resource.transform.position = GetGroundedPosition(-5f, -55f, 0.2f);

        ResourceNode node = resource.GetComponent<ResourceNode>();
        if (node == null) node = resource.AddComponent<ResourceNode>();
        node.type = ResourceNode.ResourceType.Copper;
        node.amount = 5;
        node.respawnTime = 10f;
    }

    private static void SetupPortal(Transform portalGroup)
    {
        GameObject portal = GameObject.Find("Portal_To_CoLoa");
        if (portal == null) portal = new GameObject("Portal_To_CoLoa");

        portal.transform.SetParent(portalGroup, true);
        portal.transform.position = GetGroundedPosition(0f, -70f, 0.02f);
        portal.transform.rotation = Quaternion.identity;

        BoxCollider collider = portal.GetComponent<BoxCollider>();
        if (collider == null) collider = portal.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(3f, 3f, 1f);
        collider.center = new Vector3(0f, 1.5f, 0f);

        MapPortal mapPortal = portal.GetComponent<MapPortal>();
        if (mapPortal == null) mapPortal = portal.AddComponent<MapPortal>();
        mapPortal.targetSceneName = "CoLoa_Forest_Map";
        mapPortal.rotationSpeed = 60f;

        for (int i = portal.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(portal.transform.GetChild(i).gameObject);
        }

        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX_Upgrade_Tier3.prefab");
        if (vfxPrefab != null)
        {
            GameObject vfx = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab, portal.transform);
            vfx.name = "Portal_Visual";
            vfx.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            vfx.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            vfx.transform.localScale = Vector3.one * 1.2f;
        }
        else
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Portal_Visual";
            marker.transform.SetParent(portal.transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            marker.transform.localScale = new Vector3(1.5f, 0.08f, 1.5f);
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
        }
    }

    private static void ConfigureCombatStats()
    {
        ConfigureEnemyData(
            "Assets/Data/EnemyData_UMinhBinh.asset",
            maxHealth: 120,
            moveSpeed: 3.2f,
            attackDamage: 12f,
            attackRange: 2.1f,
            attackCooldown: 1.4f,
            sightRange: 14f,
            chaseRange: 22f,
            defense: 1f,
            copperReward: 6,
            expReward: 12
        );

        ConfigureEnemyData(
            "Assets/Data/EnemyData_UMinhCai.asset",
            maxHealth: 240,
            moveSpeed: 2.8f,
            attackDamage: 22f,
            attackRange: 2.5f,
            attackCooldown: 1.8f,
            sightRange: 14f,
            chaseRange: 24f,
            defense: 6f,
            copperReward: 25,
            expReward: 50
        );

        ConfigureEnemyData(
            "Assets/Data/EnemyData_UMinhSat.asset",
            maxHealth: 180,
            moveSpeed: 3.4f,
            attackDamage: 18f,
            attackRange: 2.3f,
            attackCooldown: 1.5f,
            sightRange: 14f,
            chaseRange: 24f,
            defense: 4f,
            copperReward: 10,
            expReward: 20
        );

        ConfigureBossData("Assets/Data/BossData_TanThuong.asset");

        PlayerStats playerStats = AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/Data/PlayerStats.asset");
        if (playerStats != null)
        {
            playerStats.maxHealth = 120;
            playerStats.currentHealth = playerStats.EffectiveMaxHealth;
            playerStats.attackDamage = 24f;
            playerStats.defense = 5f;
            EditorUtility.SetDirty(playerStats);
        }
    }

    private static void ConfigureEnemyData(
        string path,
        int maxHealth,
        float moveSpeed,
        float attackDamage,
        float attackRange,
        float attackCooldown,
        float sightRange,
        float chaseRange,
        float defense,
        int copperReward,
        int expReward)
    {
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (data == null) return;

        data.maxHealth = maxHealth;
        data.moveSpeed = moveSpeed;
        data.attackDamage = attackDamage;
        data.attackRange = attackRange;
        data.attackCooldown = attackCooldown;
        data.sightRange = sightRange;
        data.chaseRange = chaseRange;
        data.defense = defense;
        data.copperReward = copperReward;
        data.expReward = expReward;
        data.tier1Multiplier = 1f;
        data.tier2Multiplier = 1.35f;
        data.tier3Multiplier = 1.8f;
        data.tier4Multiplier = 2.5f;
        EditorUtility.SetDirty(data);
    }

    private static void ConfigureBossData(string path)
    {
        BossData data = AssetDatabase.LoadAssetAtPath<BossData>(path);
        if (data == null) return;

        data.maxHealth = 420;
        data.moveSpeed = 2.8f;
        data.attackDamage = 18f;
        data.attackRange = 3f;
        data.attackCooldown = 2f;
        data.defense = 6f;
        data.totalPhases = 2;
        data.phaseHealthThresholds = new[] { 0.5f };
        data.specialAbilityCooldown = 10f;
        data.dashSpeed = 8f;
        data.aoeRadius = 4f;
        data.aoeDamage = 26f;
        EditorUtility.SetDirty(data);
    }

    private static Material EnsureEnemyMaterial()
    {
        const string materialPath = "Assets/Materials/EnemyMaterial.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (!Directory.Exists("Assets/Materials"))
        {
            Directory.CreateDirectory("Assets/Materials");
        }

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader)
            {
                name = "EnemyMaterial",
                color = new Color(0.65f, 0.55f, 0.48f, 1f)
            };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/model/shadesoldier3dmodel_basecolor.PNG");
        if (baseTexture != null)
        {
            material.SetTexture("_BaseMap", baseTexture);
            material.SetTexture("_MainTex", baseTexture);
            material.SetColor("_BaseColor", Color.white);
            material.color = Color.white;
        }
        else
        {
            material.SetColor("_BaseColor", new Color(0.65f, 0.55f, 0.48f, 1f));
            material.color = new Color(0.65f, 0.55f, 0.48f, 1f);
        }

        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.28f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void FixEnemyPrefabs(Material enemyMaterial)
    {
        ConfigureEnemyPrefab(
            "Assets/Prefabs/Enemy_UMinhBinh.prefab",
            "Assets/model/U Minh Binh.fbx",
            "Assets/Data/EnemyData_UMinhBinh.asset",
            enemyMaterial,
            2.1f
        );

        ConfigureEnemyPrefab(
            "Assets/Prefabs/Enemy_UMinhCai.prefab",
            "Assets/model/Linh Thú Tha Hóa.fbx",
            "Assets/Data/EnemyData_UMinhCai.asset",
            enemyMaterial,
            1.8f
        );

        ConfigureEnemyPrefab(
            "Assets/Prefabs/Enemy_UMinhSat.prefab",
            "Assets/model/U Minh Tướng.fbx",
            "Assets/Data/EnemyData_UMinhSat.asset",
            enemyMaterial,
            2.4f
        );
    }

    private static void ConfigureEnemyPrefab(string prefabPath, string modelPath, string dataPath, Material enemyMaterial, float desiredHeight)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            TrySetTag(root, "Enemy");

            MeshRenderer rootRenderer = root.GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            if (agent == null) agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 3.6f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            EnemyData prefabEnemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
            agent.stoppingDistance = prefabEnemyData != null ? Mathf.Max(1.1f, prefabEnemyData.attackRange * 0.8f) : 1.6f;
            agent.radius = 0.5f;
            agent.height = 2f;

            Collider collider = root.GetComponent<Collider>();
            if (collider == null)
            {
                CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
                capsule.radius = 0.45f;
                capsule.height = 2f;
                capsule.center = new Vector3(0f, 1f, 0f);
            }

            EnemyController controller = root.GetComponent<EnemyController>();
            if (controller == null) controller = root.AddComponent<EnemyController>();
            controller.data = prefabEnemyData;
            controller.currentTier = 1;
            controller.sightRange = controller.data != null ? controller.data.sightRange : 14f;
            controller.chaseRange = controller.data != null ? controller.data.chaseRange : 22f;
            controller.attackRange = controller.data != null ? controller.data.attackRange : 2f;

            LootDropper lootDropper = root.GetComponent<LootDropper>();
            if (lootDropper == null) lootDropper = root.AddComponent<LootDropper>();
            ConfigureDefaultDrops(lootDropper);

            SetupProperEnemyVisual(root, modelPath, enemyMaterial, desiredHeight);
            controller.animator = root.GetComponentInChildren<Animator>();

            Map2HealthBar healthBar = root.GetComponent<Map2HealthBar>();
            if (healthBar == null) healthBar = root.AddComponent<Map2HealthBar>();
            healthBar.offset = new Vector3(0f, desiredHeight + 0.35f, 0f);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
        GameObject updatedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (data != null && updatedPrefab != null)
        {
            data.modelPrefab = updatedPrefab;
            EditorUtility.SetDirty(data);
        }
    }

    private static void SetupProperEnemyVisual(GameObject enemy, string modelPath, Material enemyMaterial, float desiredHeight)
    {
        if (enemy == null) return;

        MeshRenderer rootRenderer = enemy.GetComponent<MeshRenderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;

        Transform oldVisual = enemy.transform.Find("EnemyVisual");
        if (oldVisual != null)
        {
            UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
        }

        GameObject holder = new("EnemyVisual");
        holder.transform.SetParent(enemy.transform, false);
        holder.transform.localPosition = Vector3.zero;
        holder.transform.localRotation = Quaternion.identity;
        holder.transform.localScale = Vector3.one;

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        GameObject model;
        if (modelAsset != null)
        {
            model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, holder.transform);
            model.name = Path.GetFileNameWithoutExtension(modelPath);
        }
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "FallbackEnemyVisual";
            model.transform.SetParent(holder.transform, false);
            UnityEngine.Object.DestroyImmediate(model.GetComponent<Collider>());
        }

        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        foreach (Rigidbody rb in holder.GetComponentsInChildren<Rigidbody>(true))
        {
            UnityEngine.Object.DestroyImmediate(rb);
        }

        foreach (Collider collider in holder.GetComponentsInChildren<Collider>(true))
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        AssignMaterial(holder, enemyMaterial, true);
        NormalizeVisualToRoot(holder, desiredHeight);
    }

    private static void NormalizeVisualToRoot(GameObject holder, float desiredHeight)
    {
        if (holder == null) return;
        if (!TryGetRendererBounds(holder, out Bounds bounds)) return;
        if (bounds.size.y <= 0.01f) return;

        float scale = desiredHeight / bounds.size.y;
        holder.transform.localScale *= scale;

        if (!TryGetRendererBounds(holder, out bounds)) return;

        Vector3 rootPosition = holder.transform.parent.position;
        Vector3 offset = new(
            rootPosition.x - bounds.center.x,
            rootPosition.y - bounds.min.y,
            rootPosition.z - bounds.center.z
        );
        holder.transform.position += offset;
    }

    private static bool TryGetRendererBounds(GameObject obj, out Bounds bounds)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(obj.transform.position, Vector3.zero);

        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void AssignDefaultSword(PlayerMovement playerMovement)
    {
        if (playerMovement == null) return;

        GameObject swordPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval_Weapons/Sword/Prefab/sword.prefab") ??
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Medieval Weapons (Melee + Ranged)/Prefabs/Arming_Sword.prefab");

        if (swordPrefab == null) return;

        SerializedObject serialized = new(playerMovement);
        SerializedProperty swordProperty = serialized.FindProperty("_swordPrefab");
        if (swordProperty != null && swordProperty.objectReferenceValue == null)
        {
            swordProperty.objectReferenceValue = swordPrefab;
        }

        SerializedProperty damageProperty = serialized.FindProperty("_attackDamage");
        if (damageProperty != null) damageProperty.floatValue = 24f;

        SerializedProperty rangeProperty = serialized.FindProperty("_attackRange");
        if (rangeProperty != null) rangeProperty.floatValue = 2.7f;

        SerializedProperty radiusProperty = serialized.FindProperty("_attackRadius");
        if (radiusProperty != null) radiusProperty.floatValue = 1.6f;

        SerializedProperty cooldownProperty = serialized.FindProperty("_attackCooldown");
        if (cooldownProperty != null) cooldownProperty.floatValue = 0.8f;

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(playerMovement);
    }

    private static void AssignMaterial(GameObject target, Material material, bool force = false)
    {
        if (material == null || target == null) return;

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = material;
                continue;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                if (force || materials[i] == null || materials[i].shader == null || materials[i].shader.name == "Hidden/InternalErrorShader")
                {
                    materials[i] = material;
                }
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static Vector3 GetGroundedPosition(float x, float z, float yOffset)
    {
        Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        if (terrain != null)
        {
            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideTerrain =
                x >= terrainPos.x &&
                x <= terrainPos.x + terrainSize.x &&
                z >= terrainPos.z &&
                z <= terrainPos.z + terrainSize.z;

            if (insideTerrain)
            {
                float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrainPos.y;
                return new Vector3(x, y + yOffset, z);
            }
        }

        Vector3 rayStart = new(x, 200f, z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 500f);
        float bestY = float.NegativeInfinity;
        bool foundGround = false;

        foreach (RaycastHit hit in hits)
        {
            string hitName = hit.collider.name.ToLowerInvariant();
            bool isGround =
                hit.collider.GetComponent<Terrain>() != null ||
                hitName.Contains("terrain") ||
                hitName.Contains("ground") ||
                hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground");

            if (!isGround) continue;
            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            return new Vector3(x, bestY + yOffset, z);
        }

        return new Vector3(x, 1f + yOffset, z);
    }

    private static void DisableExtraRigidbodies(GameObject player)
    {
        foreach (Rigidbody rb in player.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);
        AddScene(scenes, "Assets/Scenes/MainMenu.unity");
        AddScene(scenes, "Assets/Scenes/Loading.unity");
        AddScene(scenes, Map1ScenePath);
        AddScene(scenes, Map2ScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddScene(List<EditorBuildSettingsScene> scenes, string path)
    {
        if (!File.Exists(path)) return;

        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path.Replace("\\", "/") == path)
            {
                scene.enabled = true;
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(path, true));
    }

    private static List<GameObject> GetSceneGameObjects()
    {
        List<GameObject> result = new();
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (EditorUtility.IsPersistent(obj)) continue;
            if (!obj.scene.IsValid()) continue;
            if (obj.scene != SceneManager.GetActiveScene()) continue;
            result.Add(obj);
        }

        return result;
    }

    private static void TrySetTag(GameObject obj, string tag)
    {
        if (obj == null) return;
        try
        {
            obj.tag = tag;
        }
        catch
        {
            Debug.LogWarning($"[Map2Setup] Tag '{tag}' chua ton tai. Hay tao tag nay trong Unity neu can.");
        }
    }

    private static bool CompareTagSafe(this GameObject obj, string tag)
    {
        try
        {
            return obj != null && obj.CompareTag(tag);
        }
        catch
        {
            return false;
        }
    }
}
