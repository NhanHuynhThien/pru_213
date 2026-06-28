using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildVillageMenu
{
    [MenuItem("Tools/Build Village Area")]
    public static void BuildVillage()
    {
        // 1. Create Village_Area group
        GameObject villageGroup = GameObject.Find("Village_Area");
        if (villageGroup != null)
        {
            Object.DestroyImmediate(villageGroup);
        }
        villageGroup = new GameObject("Village_Area");
        villageGroup.transform.position = Vector3.zero;

        // 2. Load House Prefabs
        string[] prefabPaths = new string[]
        {
            "Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_01.prefab",
            "Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_02.prefab",
            "Assets/Naganeupseong/Prefabs/Build/Bamboo_Rafter_House_03.prefab"
        };

        List<GameObject> housePrefabs = new List<GameObject>();
        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                housePrefabs.Add(prefab);
            }
            else
            {
                Debug.LogWarning("Could not load house prefab: " + path);
            }
        }

        if (housePrefabs.Count == 0)
        {
            Debug.LogError("No house prefabs found! Aborting village generation.");
            return;
        }

        // 3. Generate grid of houses
        float gridSpacing = 12f;
        float startPos = -42f;
        float endPos = 42f;

        // Random generator for variety
        System.Random rnd = new System.Random(12345);

        int totalHouses = 0;

        for (float x = startPos; x <= endPos; x += gridSpacing)
        {
            for (float z = startPos; z <= endPos; z += gridSpacing)
            {
                // Skip the central royal area
                if (Mathf.Abs(x) < 20f && Mathf.Abs(z) < 20f)
                {
                    continue;
                }

                // Skip the main crossroad
                if (Mathf.Abs(x) < 6f || Mathf.Abs(z) < 6f)
                {
                    continue;
                }

                // Skip the grid road lines (+/- 24)
                if (Mathf.Abs(Mathf.Abs(x) - 24f) < 4f || Mathf.Abs(Mathf.Abs(z) - 24f) < 4f)
                {
                    continue;
                }

                // Add some organic noise to placement
                float offsetX = (float)(rnd.NextDouble() * 4.0 - 2.0); // +/- 2.0
                float offsetZ = (float)(rnd.NextDouble() * 4.0 - 2.0); // +/- 2.0
                
                // Instantiate
                GameObject selectedPrefab = housePrefabs[rnd.Next(housePrefabs.Count)];
                GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                
                house.transform.position = new Vector3(x + offsetX, 0, z + offsetZ);

                // Align to grid (0, 90, 180, 270) + random jitter
                float[] rotations = { 0f, 90f, 180f, 270f };
                float baseRot = rotations[rnd.Next(rotations.Length)];
                float rotY = baseRot + (float)(rnd.NextDouble() * 20.0 - 10.0);
                house.transform.rotation = Quaternion.Euler(0, rotY, 0);

                // Random scale variation for natural look
                float randomScale = (float)(rnd.NextDouble() * 0.2 + 0.6); // 0.6 to 0.8
                house.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                PhysicsLogicUtility.ApplyPhysicalLogic(house);

                house.transform.SetParent(villageGroup.transform);
                totalHouses++;
            }
        }

        Debug.Log("Village Area generated successfully with " + totalHouses + " houses!");
    }
}
