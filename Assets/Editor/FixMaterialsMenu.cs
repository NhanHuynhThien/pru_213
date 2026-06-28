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
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Idyllic Fantasy Nature" });
        int fixedCount = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            bool needsFix = false;
            Shader currentShader = mat.shader;

            // Check if shader is missing/broken or is the Standard built-in shader
            if (currentShader == null || 
                currentShader.name == "Hidden/Internal-ErrorShader" || 
                currentShader.name == "Standard")
            {
                needsFix = true;
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
                    float cutoff = 0.5f;

                    if (mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");
                    else if (mat.HasProperty("_Texture")) mainTex = mat.GetTexture("_Texture");

                    if (mat.HasProperty("_Color")) mainColor = mat.GetColor("_Color");
                    if (mat.HasProperty("_BumpMap")) bumpMap = mat.GetTexture("_BumpMap");
                    if (mat.HasProperty("_Cutoff")) cutoff = mat.GetFloat("_Cutoff");

                    // Switch shader
                    mat.shader = urpShader;

                    // Re-apply properties to URP equivalents
                    if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                    mat.SetColor("_BaseColor", mainColor);
                    if (bumpMap != null) mat.SetTexture("_BumpMap", bumpMap);

                    // If it was transparent/cutout, configure URP lit for alpha clipping
                    if (mat.shaderKeywords != null)
                    {
                        foreach (string keyword in mat.shaderKeywords)
                        {
                            if (keyword == "_ALPHATEST_ON")
                            {
                                mat.SetFloat("_AlphaClip", 1f);
                                mat.SetFloat("_Cutoff", cutoff);
                                mat.EnableKeyword("_ALPHATEST_ON");
                                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                                break;
                            }
                        }
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
