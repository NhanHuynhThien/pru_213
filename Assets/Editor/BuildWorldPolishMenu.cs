using UnityEngine;
using UnityEditor;

public class BuildWorldPolishMenu
{
    [MenuItem("Tools/Apply World Polish")]
    public static void ApplyWorldPolish()
    {
        // 1. Skybox and Lighting
        string[] skyboxGuids = AssetDatabase.FindAssets("Skybox t:Material");
        if (skyboxGuids.Length > 0)
        {
            Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(skyboxGuids[0]));
            if (skyboxMat != null)
            {
                RenderSettings.skybox = skyboxMat;
            }
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        DynamicGI.UpdateEnvironment(); // Force update ambient

        // 2. Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.85f, 0.8f, 0.7f); // Warm dusty sunlight
        RenderSettings.fogDensity = 0.003f;

        // 3. Directional Light Tweaks
        GameObject sunObj = GameObject.Find("Directional Light");
        if (sunObj != null)
        {
            Light sun = sunObj.GetComponent<Light>();
            if (sun != null)
            {
                sun.color = new Color(1f, 0.95f, 0.85f);
                sun.intensity = 1.2f;
                sun.shadows = LightShadows.Soft;
            }
        }

        // 4. Terrain Grass Details & Colliders
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            // Add Terrain Collider if missing
            if (terrain.gameObject.GetComponent<TerrainCollider>() == null)
            {
                terrain.gameObject.AddComponent<TerrainCollider>().terrainData = terrain.terrainData;
            }

            TerrainData tData = terrain.terrainData;

            // Load Grass Texture
            string[] grassGuids = AssetDatabase.FindAssets("Grass_01 t:Texture2D");
            if (grassGuids.Length > 0)
            {
                Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(grassGuids[0]));
                if (grassTex != null)
                {
                    DetailPrototype grassDetail = new DetailPrototype();
                    grassDetail.prototypeTexture = grassTex;
                    grassDetail.renderMode = DetailRenderMode.GrassBillboard;
                    grassDetail.dryColor = new Color(0.8f, 0.8f, 0.4f);
                    grassDetail.healthyColor = new Color(0.6f, 0.8f, 0.3f);
                    grassDetail.minWidth = 1f;
                    grassDetail.maxWidth = 1.5f;
                    grassDetail.minHeight = 1f;
                    grassDetail.maxHeight = 1.5f;

                    tData.detailPrototypes = new DetailPrototype[] { grassDetail };

                    // Paint Grass only on grass splatmap
                    int detailRes = tData.detailResolution;
                    int splatRes = tData.alphamapResolution;
                    int[,] detailMap = new int[detailRes, detailRes];
                    float[,,] splatMap = tData.GetAlphamaps(0, 0, splatRes, splatRes);

                    for (int y = 0; y < detailRes; y++)
                    {
                        for (int x = 0; x < detailRes; x++)
                        {
                            // Map detail coordinate to splat coordinate
                            int sx = Mathf.RoundToInt((float)x / detailRes * splatRes);
                            int sy = Mathf.RoundToInt((float)y / detailRes * splatRes);
                            sx = Mathf.Clamp(sx, 0, splatRes - 1);
                            sy = Mathf.Clamp(sy, 0, splatRes - 1);

                            float grassWeight = splatMap[sx, sy, 0]; // Assume layer 0 is grass
                            if (grassWeight > 0.6f)
                            {
                                // Add 1-3 grass elements randomly
                                detailMap[x, y] = UnityEngine.Random.Range(1, 4);
                            }
                            else
                            {
                                detailMap[x, y] = 0;
                            }
                        }
                    }

                    tData.SetDetailLayer(0, 0, 0, detailMap);
                }
            }
        }

        // 5. Particles
        string[] particleGuids = AssetDatabase.FindAssets("Terrain_Particles t:GameObject");
        if (particleGuids.Length > 0)
        {
            GameObject particlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(particleGuids[0]));
            if (particlePrefab != null)
            {
                // Place particles over Royal Area
                GameObject p1 = (GameObject)PrefabUtility.InstantiatePrefab(particlePrefab);
                p1.transform.position = new Vector3(0, 2, 0);

                // Place near South Gate
                GameObject p2 = (GameObject)PrefabUtility.InstantiatePrefab(particlePrefab);
                p2.transform.position = new Vector3(0, 2, -60);
            }
        }

        Debug.Log("World Polish applied successfully!");
    }
}
