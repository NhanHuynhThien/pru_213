using UnityEngine;
using UnityEditor;

public class BuildMilitaryCampMenu
{
    [MenuItem("Tools/Build Military Camp")]
    public static void BuildMilitaryCamp()
    {
        // 1. Create Military_Camp group
        GameObject militaryGroup = GameObject.Find("Military_Camp");
        if (militaryGroup != null)
        {
            Object.DestroyImmediate(militaryGroup);
        }
        militaryGroup = new GameObject("Military_Camp");
        militaryGroup.transform.position = Vector3.zero;

        // 2. Map fake prefabs to existing assets
        string barracksPath = "Assets/Naganeupseong/Prefabs/Build/Local_Personnel_Clerks_House_01.prefab";
        string watchTowerPath = "Assets/Namhansanseong/Prefabs/Buildings/Chimgwaejeong_Pavilion.prefab";
        string tentPath = "Assets/Naganeupseong/Prefabs/Build/Wooden_Bench_House_01.prefab";

        GameObject barracksPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(barracksPath);
        GameObject watchTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(watchTowerPath);
        GameObject tentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tentPath);

        if (barracksPrefab == null || watchTowerPrefab == null || tentPrefab == null)
        {
            Debug.LogError("Could not find mapped prefabs for the Military Camp!");
            return;
        }

        // 3. Create separated camps outside the Southern gate (Z < -65)
        // Gate is around (0, 0, -60). 
        // We will create 3 camps: Southwest, South, Southeast
        Vector3[] campCenters = new Vector3[]
        {
            new Vector3(-40, 0, -90),
            new Vector3(0, 0, -110),
            new Vector3(40, 0, -90)
        };

        System.Random rnd = new System.Random(999);

        foreach (Vector3 center in campCenters)
        {
            GameObject campObj = new GameObject("Camp_Group");
            campObj.transform.position = center;
            campObj.transform.SetParent(militaryGroup.transform);

            // Place WatchTower in the center-back of the camp
            Vector3 towerPos = center + new Vector3(0, 0, -10);
            InstantiatePrefab(watchTowerPrefab, towerPos, Quaternion.Euler(0, 0, 0), campObj.transform);

            // Place Barracks on the side
            Vector3 barracksPos = center + new Vector3(-15, 0, 0);
            InstantiatePrefab(barracksPrefab, barracksPos, Quaternion.Euler(0, 90, 0), campObj.transform);

            // Place Tents scattered around
            for (int i = 0; i < 4; i++)
            {
                float dx = (float)(rnd.NextDouble() * 20 - 10) + 10; // 0 to 20
                float dz = (float)(rnd.NextDouble() * 20 - 10);
                float rot = (float)(rnd.NextDouble() * 360);
                Vector3 tentPos = center + new Vector3(dx, 0, dz);
                
                InstantiatePrefab(tentPrefab, tentPos, Quaternion.Euler(0, rot, 0), campObj.transform);
            }
        }

        Debug.Log("Military Camps generated successfully outside the southern gate!");
    }

    private static void InstantiatePrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        PhysicsLogicUtility.ApplyPhysicalLogic(instance);
        instance.transform.SetParent(parent);
    }
}
