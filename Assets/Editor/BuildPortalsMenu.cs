using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class BuildPortalsMenu
{
    [MenuItem("Tools/Set Up Map Portals")]
    public static void SetUpPortals()
    {
        Debug.Log("[BuildPortals] Starting portal integration...");

        // 1. Setup portal in Map 1 (CoLoa_Forest_Map)
        string map1Path = "Assets/Scene/CoLoa_Forest_Map.unity";
        // Check fallback path just in case
        if (!System.IO.File.Exists(map1Path))
        {
            map1Path = "Assets/Map_1/CoLoa_Forest_Map.unity";
        }

        if (System.IO.File.Exists(map1Path))
        {
            Scene map1 = EditorSceneManager.OpenScene(map1Path, OpenSceneMode.Single);
            
            // Remove existing portal to avoid duplicates
            GameObject oldPortal = GameObject.Find("Portal_To_RungHacAm");
            if (oldPortal != null) Object.DestroyImmediate(oldPortal);

            // Create new portal
            GameObject portalObj = CreatePortalObject("Portal_To_RungHacAm", "Rung_Hac_Am", new Vector3(0f, 0.5f, -80f));
            
            // Save Map 1
            EditorSceneManager.SaveScene(map1);
            Debug.Log("[BuildPortals] Portal added to Map 1 (CoLoa_Forest_Map) successfully.");
        }
        else
        {
            Debug.LogError($"[BuildPortals] Map 1 scene not found at: {map1Path}");
        }

        // 2. Setup portal in Map 2 (Rung_Hac_Am)
        string map2Path = "Assets/Scene/Rung_Hac_Am.unity";
        if (System.IO.File.Exists(map2Path))
        {
            Scene map2 = EditorSceneManager.OpenScene(map2Path, OpenSceneMode.Single);

            // Remove existing portal
            GameObject oldPortal = GameObject.Find("Portal_To_CoLoa");
            if (oldPortal != null) Object.DestroyImmediate(oldPortal);

            // Create new portal
            GameObject portalObj = CreatePortalObject("Portal_To_CoLoa", "CoLoa_Forest_Map", new Vector3(0f, 0.5f, -70f));
            
            // Save Map 2
            EditorSceneManager.SaveScene(map2);
            Debug.Log("[BuildPortals] Portal added to Map 2 (Rung_Hac_Am) successfully.");
        }
        else
        {
            Debug.LogError($"[BuildPortals] Map 2 scene not found at: {map2Path}");
        }

        // 3. Register both scenes in Build Settings if not already present
        System.Collections.Generic.List<EditorBuildSettingsScene> buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        AddSceneToBuild(buildScenes, map1Path);
        AddSceneToBuild(buildScenes, map2Path);
        EditorBuildSettings.scenes = buildScenes.ToArray();

        // Re-open Map 1 at the end so developer starts in Map 1
        if (System.IO.File.Exists(map1Path))
        {
            EditorSceneManager.OpenScene(map1Path, OpenSceneMode.Single);
        }

        Debug.Log("[BuildPortals] Portals setup finished successfully!");
    }

    private static void AddSceneToBuild(System.Collections.Generic.List<EditorBuildSettingsScene> buildScenes, string path)
    {
        if (System.IO.File.Exists(path))
        {
            bool exists = false;
            foreach (var scene in buildScenes)
            {
                if (scene.path.Replace("\\", "/").Equals(path.Replace("\\", "/"), System.StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    scene.enabled = true;
                    break;
                }
            }
            if (!exists)
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }
    }

    private static GameObject CreatePortalObject(string name, string targetScene, Vector3 position)
    {
        // 1. Parent portal object
        GameObject portal = new GameObject(name);
        portal.transform.position = position;

        // 2. Add Trigger Collider
        BoxCollider box = portal.AddComponent<BoxCollider>();
        box.size = new Vector3(3f, 3f, 1f);
        box.isTrigger = true;

        // 3. Add MapPortal component
        MapPortal mp = portal.AddComponent<MapPortal>();
        mp.targetSceneName = targetScene;
        mp.rotationSpeed = 0f; // Portal object itself doesn't spin, only the visuals inside

        // 4. Add Visuals: Street lanterns on left and right to frame the portal
        GameObject lanternPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SkythianCat/Glowing_Forest/Prefabs/Props/Streetlight_ON.prefab");
        if (lanternPrefab != null)
        {
            // Left lantern
            GameObject leftLantern = (GameObject)PrefabUtility.InstantiatePrefab(lanternPrefab, portal.transform);
            leftLantern.transform.localPosition = new Vector3(-2.2f, 0f, 0f);
            
            // Right lantern
            GameObject rightLantern = (GameObject)PrefabUtility.InstantiatePrefab(lanternPrefab, portal.transform);
            rightLantern.transform.localPosition = new Vector3(2.2f, 0f, 0f);
        }

        // 5. Add Portal Core Visual (Glowing Upgrade Particle Effect)
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX_Upgrade_Tier3.prefab");
        if (vfxPrefab != null)
        {
            GameObject vfx = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab, portal.transform);
            vfx.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            vfx.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Rotate to face vertical
            vfx.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            
            // Assign VFX to MapPortal to rotate it
            mp.portalVFXPrefab = vfx;
            mp.rotationSpeed = 60f; // Spin the particle core
        }
        else
        {
            // Fallback visual cylinder if no prefab
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.transform.parent = portal.transform;
            cyl.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            cyl.transform.localScale = new Vector3(2f, 1.5f, 0.1f);
            cyl.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Object.DestroyImmediate(cyl.GetComponent<CapsuleCollider>()); // Remove physics collider
            
            // Set layer to Default or Water
            cyl.layer = 0;
            
            // Try to find a glowing blue material
            string[] guids = AssetDatabase.FindAssets("Water t:Material");
            if (guids.Length > 0)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (mat != null) cyl.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        return portal;
    }
}
