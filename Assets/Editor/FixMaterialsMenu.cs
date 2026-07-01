using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class FixMaterialsMenu
{
    static FixMaterialsMenu()
    {
        // Runs automatically when Unity compiles/reloads scripts or opens the project
        if (!SessionState.GetBool("AutoFixMaterialsAndShadersRun", false))
        {
            SessionState.SetBool("AutoFixMaterialsAndShadersRun", true);
            EditorApplication.delayCall += FixMaterialsAndShaders;
        }
    }

    [MenuItem("Tools/Fix Materials and Shaders")]
    public static void FixMaterialsAndShaders()
    {
        Debug.Log("[AutoFix] Starting material and shader fixing process...");

        // 1. Force reimport the Vegetation.shadergraph
        string shaderPath = "Assets/Idyllic Fantasy Nature/Shader/Vegetation.shadergraph";
        if (File.Exists(shaderPath))
        {
            Debug.Log($"[AutoFix] Forcing reimport of Vegetation.shadergraph: {shaderPath}");
            AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
        }
        else
        {
            Debug.LogWarning($"[AutoFix] Vegetation.shadergraph not found at: {shaderPath}");
        }

        // 2. Scan and fix materials that might be using Built-in Standard shader or missing shaders
        string[] scanFolders = {
            "Assets/JP Environmental Asset Pack",
            "Assets/Silver_Cats",
            "Assets/SkythianCat"
        };
        
        System.Collections.Generic.List<string> materialGuidsList = new System.Collections.Generic.List<string>();
        foreach (var folder in scanFolders)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
                materialGuidsList.AddRange(guids);
            }
        }

        string[] materialGuids = materialGuidsList.ToArray();
        int fixedCount = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            bool needsFix = false;
            Shader currentShader = mat.shader;

            if (currentShader == null || currentShader.name == "Hidden/Internal-ErrorShader")
            {
                needsFix = true;
            }
            else
            {
                string shaderName = currentShader.name;
                // If it doesn't contain URP or Shader Graph, and it's a standard/legacy shader that renders purple in URP
                if (!shaderName.Contains("Universal Render Pipeline") && 
                    !shaderName.Contains("Shader Graph") && 
                    !shaderName.Contains("TextMeshPro") &&
                    !shaderName.Contains("GUI") &&
                    !shaderName.Contains("UI/") &&
                    !shaderName.Contains("Sprite") &&
                    !shaderName.Contains("Skybox") &&
                    !shaderName.Contains("Unlit/"))
                {
                    needsFix = true;
                }
                
                // Force check if it is already URP Lit but is a cutout leaf/branch/grass material missing alpha clip
                if (shaderName == "Universal Render Pipeline/Lit")
                {
                    string matNameLower = mat.name.ToLower();
                    if (matNameLower.Contains("branch") || matNameLower.Contains("leaf") || matNameLower.Contains("leaves") || matNameLower.Contains("foliage") || matNameLower.Contains("grass"))
                    {
                        if (mat.GetFloat("_AlphaClip") == 0f)
                        {
                            needsFix = true;
                        }
                    }
                }
            }

            if (needsFix)
            {
                Debug.Log($"[AutoFix] Fixing material: {mat.name} at {path} (Current Shader: {(currentShader != null ? currentShader.name : "NULL")})");

                // Target URP Lit shader
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader != null)
                {
                    // Cache existing properties before switching shader
                    Texture mainTex = null;
                    Color mainColor = Color.white;
                    Texture bumpMap = null;
                    float cutoff = 0.3f;

                    if (mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");
                    else if (mat.HasProperty("_Texture")) mainTex = mat.GetTexture("_Texture");
                    else if (mat.HasProperty("_BaseMap")) mainTex = mat.GetTexture("_BaseMap");

                    if (mat.HasProperty("_Color")) mainColor = mat.GetColor("_Color");
                    else if (mat.HasProperty("_BaseColor")) mainColor = mat.GetColor("_BaseColor");

                    if (mat.HasProperty("_BumpMap")) bumpMap = mat.GetTexture("_BumpMap");
                    if (mat.HasProperty("_Cutoff")) cutoff = mat.GetFloat("_Cutoff");

                    // Switch shader
                    mat.shader = urpShader;

                    // Re-apply properties to URP equivalents
                    if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                    mat.SetColor("_BaseColor", mainColor);
                    if (bumpMap != null) mat.SetTexture("_BumpMap", bumpMap);

                    // Enable Alpha Clipping if it's cutout or leaf/branch/grass
                    bool isCutout = false;
                    string matNameLower = mat.name.ToLower();
                    if (matNameLower.Contains("branch") || matNameLower.Contains("leaf") || matNameLower.Contains("leaves") || matNameLower.Contains("foliage") || matNameLower.Contains("grass"))
                    {
                        isCutout = true;
                    }
                    if (currentShader != null)
                    {
                        string sName = currentShader.name.ToLower();
                        if (sName.Contains("cutout") || sName.Contains("transparent") || sName.Contains("fade"))
                        {
                            isCutout = true;
                        }
                    }
                    if (mat.HasProperty("_Mode") && mat.GetFloat("_Mode") > 0f)
                    {
                        isCutout = true;
                    }

                    if (isCutout)
                    {
                        mat.SetFloat("_AlphaClip", 1f);
                        mat.SetFloat("_Cutoff", cutoff > 0.05f ? cutoff : 0.3f);
                        mat.EnableKeyword("_ALPHATEST_ON");
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    }

                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
                else
                {
                    Debug.LogError("[AutoFix] Universal Render Pipeline/Lit shader not found! Cannot upgrade material.");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AutoFix] Material and shader fixing process completed! Fixed {fixedCount} materials.");
    }
}
