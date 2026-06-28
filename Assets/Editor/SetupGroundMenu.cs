using UnityEngine;
using UnityEditor;

public class SetupGroundMenu
{
    [MenuItem("Tools/Setup Ground Plane")]
    public static void SetupGround()
    {
        // Check if ground already exists
        GameObject existingGround = GameObject.Find("GroundPlane");
        if (existingGround != null)
        {
            Object.DestroyImmediate(existingGround);
        }

        // Create a basic plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GroundPlane";
        
        // A default Plane is 10x10 units. We need it to cover radius 60, so at least 120x120.
        // Scale 15 = 150x150 units.
        ground.transform.localScale = new Vector3(15f, 1f, 15f);
        ground.transform.position = new Vector3(0, 0, 0);

        // Try to find a nice ground material from the assets
        string[] guids = AssetDatabase.FindAssets("M_Ground01a t:Material");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                Renderer renderer = ground.GetComponent<Renderer>();
                renderer.sharedMaterial = mat;
                
                // Tile the material so it doesn't look stretched
                renderer.sharedMaterial.mainTextureScale = new Vector2(50, 50);
            }
        }
        else
        {
            // Try another one if the first is not found
            guids = AssetDatabase.FindAssets("MI_Floor01a t:Material");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    Renderer renderer = ground.GetComponent<Renderer>();
                    renderer.sharedMaterial = mat;
                    renderer.sharedMaterial.mainTextureScale = new Vector2(30, 30);
                }
            }
        }

        Debug.Log("Ground plane created successfully!");
    }
}
