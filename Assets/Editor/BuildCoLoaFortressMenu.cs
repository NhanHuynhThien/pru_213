using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class BuildCoLoaFortressMenu
{
    [MenuItem("Tools/Build CoLoa Forest Map")]
    public static void BuildCoLoaMap()
    {
        // 1. Create a new empty scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // 2. Add Directional Light
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // 3. Add Main Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0, 100, -80);
        camObj.transform.rotation = Quaternion.Euler(60, 0, 0);

        // 4. Create Terrain (300x300), center it so (0,0,0) is the middle
        TerrainData terrainData = new TerrainData();
        terrainData.size = new Vector3(300, 50, 300);
        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.transform.position = new Vector3(-150, 0, -150);

        // 5. Create Outer_Fortress Group
        GameObject fortressGroup = new GameObject("Outer_Fortress");
        fortressGroup.transform.position = Vector3.zero;

        // 6. Build the Circular Wall using SM_Outer_Castle01a.prefab
        string wallPath = "Assets/Namhansanseong/Prefabs/Structure/SM_Outer_Castle01a.prefab";
        GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wallPath);

        if (wallPrefab == null)
        {
            Debug.LogError("Could not find Wall01a.prefab at path: " + wallPath);
        }
        else
        {
            float radius = 60f;
            float assumedWallWidth = 6.5f; 
            float gateGapSize = 12f; // Total gap width = ~24 units
            
            // 1. North Edge (Z = radius)
            for (float x = -radius + assumedWallWidth / 2f; x <= radius - assumedWallWidth / 2f; x += assumedWallWidth)
            {
                InstantiateWall(wallPrefab, new Vector3(x, 0, radius), 90f, fortressGroup.transform);
            }

            // 2. East Edge (X = radius)
            for (float z = radius - assumedWallWidth / 2f; z >= -radius + assumedWallWidth / 2f; z -= assumedWallWidth)
            {
                InstantiateWall(wallPrefab, new Vector3(radius, 0, z), 180f, fortressGroup.transform);
            }

            // 3. South Edge (Z = -radius)
            for (float x = radius - assumedWallWidth / 2f; x >= -radius + assumedWallWidth / 2f; x -= assumedWallWidth)
            {
                if (Mathf.Abs(x) < gateGapSize) continue;
                InstantiateWall(wallPrefab, new Vector3(x, 0, -radius), 270f, fortressGroup.transform);
            }

            // 4. West Edge (X = -radius)
            for (float z = -radius + assumedWallWidth / 2f; z <= radius - assumedWallWidth / 2f; z += assumedWallWidth)
            {
                InstantiateWall(wallPrefab, new Vector3(-radius, 0, z), 0f, fortressGroup.transform);
            }
        }

        // Save Scene
        string scenePath = "Assets/Map_1/CoLoa_Forest_Map.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        Debug.Log("CoLoa_Forest_Map generated successfully with Square Outer_Fortress!");
    }

    private static void InstantiateWall(GameObject prefab, Vector3 pos, float rotY, Transform parent)
    {
        GameObject wallInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        wallInstance.transform.position = pos;
        wallInstance.transform.rotation = Quaternion.Euler(0, rotY, 0);
        PhysicsLogicUtility.ApplyPhysicalLogic(wallInstance);
        wallInstance.transform.SetParent(parent);
    }
}
