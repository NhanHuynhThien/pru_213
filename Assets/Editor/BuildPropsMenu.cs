using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildPropsMenu
{
    [MenuItem("Tools/Build Environment Props")]
    public static void BuildProps()
    {
        // 1. Create Environment_Props group
        GameObject propsGroup = GameObject.Find("Environment_Props");
        if (propsGroup != null)
        {
            Object.DestroyImmediate(propsGroup);
        }
        propsGroup = new GameObject("Environment_Props");
        propsGroup.transform.position = Vector3.zero;

        // 2. Load Props Prefabs
        string[] propPaths = {
            "Assets/Naganeupseong/Prefabs/Prop/Cart.prefab",                    // Wooden cart
            "Assets/Namhansanseong/Prefabs/Structure/SM_Box.prefab",            // Storage box
            "Assets/Naganeupseong/Prefabs/Prop/Lampstand.prefab",               // Torch/Lamp
            "Assets/Naganeupseong/Prefabs/Prop/Water_Jar.prefab",               // Well/Water source
            "Assets/Naganeupseong/Prefabs/Prop/Straw_Bag.prefab"                // Extra storage
        };

        List<GameObject> props = new List<GameObject>();
        foreach (string path in propPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                props.Add(prefab);
            }
            else
            {
                Debug.LogWarning("Could not load prop prefab: " + path);
            }
        }

        if (props.Count == 0)
        {
            Debug.LogError("No props found! Aborting prop generation.");
            return;
        }

        System.Random rnd = new System.Random(777);
        int totalProps = 0;

        // 3. Find Village and Military camps to scatter around
        List<Transform> targetLocations = new List<Transform>();
        
        GameObject village = GameObject.Find("Village_Area");
        if (village != null)
        {
            foreach (Transform child in village.transform)
            {
                targetLocations.Add(child);
            }
        }

        GameObject military = GameObject.Find("Military_Camp");
        if (military != null)
        {
            foreach (Transform child in military.transform)
            {
                // Note: military camps have sub-groups "Camp_Group", so we add the groups
                targetLocations.Add(child);
            }
        }

        // Scatter 2-4 props near each location
        foreach (Transform location in targetLocations)
        {
            int numProps = rnd.Next(2, 5); // 2, 3, or 4
            for (int i = 0; i < numProps; i++)
            {
                // Random offset 6 to 12 units away
                float dist = (float)(rnd.NextDouble() * 6 + 6);
                float angle = (float)(rnd.NextDouble() * 360) * Mathf.Deg2Rad;
                
                float x = location.position.x + Mathf.Sin(angle) * dist;
                float z = location.position.z + Mathf.Cos(angle) * dist;

                // Do not block main crossroad
                if (Mathf.Abs(x) < 6f || Mathf.Abs(z) < 6f) continue;

                // Do not block grid roads (+/- 24, +/- 48)
                if (Mathf.Abs(Mathf.Abs(x) - 24f) < 4f || Mathf.Abs(Mathf.Abs(z) - 24f) < 4f) continue;
                if (Mathf.Abs(Mathf.Abs(x) - 48f) < 4f || Mathf.Abs(Mathf.Abs(z) - 48f) < 4f) continue;

                Vector3 pos = new Vector3(x, 0, z);

                GameObject selectedPrefab = props[rnd.Next(props.Count)];
                GameObject propInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                
                propInstance.transform.position = pos;
                propInstance.transform.rotation = Quaternion.Euler(0, (float)(rnd.NextDouble() * 360), 0);
                PhysicsLogicUtility.ApplyPhysicalLogic(propInstance);
                propInstance.transform.SetParent(propsGroup.transform);
                
                totalProps++;
            }
        }

        Debug.Log($"Generated {totalProps} environment props successfully!");
    }
}
