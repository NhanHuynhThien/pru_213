using UnityEngine;
using UnityEditor;

public class BuildRoyalAreaMenu
{
    [MenuItem("Tools/Build Royal Area")]
    public static void BuildRoyalArea()
    {
        // 1. Create Royal_Area group
        GameObject royalGroup = GameObject.Find("Royal_Area");
        if (royalGroup != null)
        {
            Object.DestroyImmediate(royalGroup);
        }
        royalGroup = new GameObject("Royal_Area");
        royalGroup.transform.position = Vector3.zero;

        // 2. Place Palace Building
        string palacePath = "Assets/Namhansanseong/Prefabs/Buildings/Sueojangdae_Command_Post.prefab";
        GameObject palacePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(palacePath);
        if (palacePrefab != null)
        {
            GameObject palace = (GameObject)PrefabUtility.InstantiatePrefab(palacePrefab);
            palace.transform.position = Vector3.zero;
            PhysicsLogicUtility.ApplyPhysicalLogic(palace);
            palace.transform.SetParent(royalGroup.transform);
        }
        else
        {
            Debug.LogError("Could not find Palace prefab: " + palacePath);
        }

        // 3. Create Square Inner Wall
        // The user asked for "PF_StoneWall". We use "SM_Wall01a.prefab" as the stone wall.
        string wallPath = "Assets/Namhansanseong/Prefabs/Structure/SM_Wall01a.prefab";
        GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wallPath);

        if (wallPrefab != null)
        {
            float squareHalfSize = 15f; // 30x30 square
            float wallLength = 5.5f; // Approximate length of the wall prefab

            // Number of walls per side
            int wallsPerSide = Mathf.CeilToInt((squareHalfSize * 2) / wallLength);
            float actualSideLength = wallsPerSide * wallLength;
            float startPos = -actualSideLength / 2f + wallLength / 2f;

            // North Wall (z = squareHalfSize)
            for (int i = 0; i < wallsPerSide; i++)
            {
                Vector3 pos = new Vector3(startPos + i * wallLength, 0, squareHalfSize);
                InstantiateWall(wallPrefab, pos, Quaternion.Euler(0, 0, 0), royalGroup.transform);
            }

            // South Wall (z = -squareHalfSize)
            for (int i = 0; i < wallsPerSide; i++)
            {
                Vector3 pos = new Vector3(startPos + i * wallLength, 0, -squareHalfSize);
                InstantiateWall(wallPrefab, pos, Quaternion.Euler(0, 180, 0), royalGroup.transform);
            }

            // East Wall (x = squareHalfSize)
            for (int i = 0; i < wallsPerSide; i++)
            {
                Vector3 pos = new Vector3(squareHalfSize, 0, startPos + i * wallLength);
                InstantiateWall(wallPrefab, pos, Quaternion.Euler(0, 90, 0), royalGroup.transform);
            }

            // West Wall (x = -squareHalfSize)
            for (int i = 0; i < wallsPerSide; i++)
            {
                Vector3 pos = new Vector3(-squareHalfSize, 0, startPos + i * wallLength);
                InstantiateWall(wallPrefab, pos, Quaternion.Euler(0, -90, 0), royalGroup.transform);
            }
        }
        else
        {
            Debug.LogError("Could not find Wall prefab: " + wallPath);
        }

        Debug.Log("Royal Area generated successfully!");
    }

    private static void InstantiateWall(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject wall = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        wall.transform.position = position;
        wall.transform.rotation = rotation;
        PhysicsLogicUtility.ApplyPhysicalLogic(wall);
        wall.transform.SetParent(parent);
    }
}
