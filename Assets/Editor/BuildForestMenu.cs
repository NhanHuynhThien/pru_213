using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildForestMenu
{
    [MenuItem("Tools/Build Forest Area")]
    public static void BuildForest()
    {
        // 1. Create Forest group
        GameObject forestGroup = GameObject.Find("Forest");
        if (forestGroup != null)
        {
            Object.DestroyImmediate(forestGroup);
        }
        forestGroup = new GameObject("Forest");
        forestGroup.transform.position = Vector3.zero;

        // 2. Load Prefabs
        string[] treePaths = {
            "Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_01_Green.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_02_Green.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Green.prefab"
        };
        string[] bushPaths = {
            "Assets/Idyllic Fantasy Nature/Prefabs/Bush_01_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Bush_02_01.prefab"
        };
        string[] rockPaths = {
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Medium_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Small_01.prefab"
        };

        List<GameObject> trees = LoadPrefabs(treePaths);
        List<GameObject> bushes = LoadPrefabs(bushPaths);
        List<GameObject> rocks = LoadPrefabs(rockPaths);

        if (trees.Count == 0 || bushes.Count == 0 || rocks.Count == 0)
        {
            Debug.LogError("Could not load all nature prefabs. Ensure Idyllic Fantasy Nature exists.");
            return;
        }

        System.Random rnd = new System.Random(888);
        int totalItems = 0;

        // 3. Scatter logic
        // Terrain is 300x300, (-150 to 150)
        for (float x = -140; x <= 140; x += 6)
        {
            for (float z = -140; z <= 140; z += 6)
            {
                float radius = Mathf.Sqrt(x * x + z * z);

                // Skip inside city
                if (radius < 65f) continue;

                // Skip Military Camp area (South of gate)
                if (z > -125 && z < -70 && x > -50 && x < 50) continue;
                
                // Skip road to south gate
                if (Mathf.Abs(x) < 8f && z < 0) continue;

                // Density increases towards boundary
                // At r=65, density factor ~0.1
                // At r=140, density factor ~1.0
                float normalizedDist = (radius - 65f) / (140f - 65f);
                float densityProb = Mathf.Lerp(0.05f, 0.8f, normalizedDist);

                if (rnd.NextDouble() < densityProb)
                {
                    // Add some noise to position
                    float posX = x + (float)(rnd.NextDouble() * 4 - 2);
                    float posZ = z + (float)(rnd.NextDouble() * 4 - 2);
                    Vector3 pos = new Vector3(posX, 0, posZ);

                    // Pick random rotation and scale
                    Quaternion rot = Quaternion.Euler(0, (float)(rnd.NextDouble() * 360), 0);
                    float scale = (float)(rnd.NextDouble() * 0.5f + 0.8f);

                    // Decide what to place: 60% Tree, 30% Bush, 10% Rock
                    double roll = rnd.NextDouble();
                    GameObject prefabToPlace;
                    if (roll < 0.6) prefabToPlace = trees[rnd.Next(trees.Count)];
                    else if (roll < 0.9) prefabToPlace = bushes[rnd.Next(bushes.Count)];
                    else prefabToPlace = rocks[rnd.Next(rocks.Count)];

                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);
                    instance.transform.position = pos;
                    instance.transform.rotation = rot;
                    instance.transform.localScale = new Vector3(scale, scale, scale);
                    
                    bool isTree = (roll < 0.6);
                    PhysicsLogicUtility.ApplyPhysicalLogic(instance, isTree);
                    
                    instance.transform.SetParent(forestGroup.transform);
                    totalItems++;
                }
            }
        }

        Debug.Log($"Forest Area generated successfully with {totalItems} nature objects!");
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
}
