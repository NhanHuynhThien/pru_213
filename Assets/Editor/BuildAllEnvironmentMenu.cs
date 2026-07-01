using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class BuildAllEnvironmentMenu
{
    [MenuItem("Tools/Build Complete Environment")]
    public static void BuildCompleteEnvironment()
    {
        Debug.Log("Starting Complete Environment Build...");
        
        // 1. Build basic map & outer fortress walls (Map 1)
        BuildCoLoaFortressMenu.BuildCoLoaMap();
        
        // 2. Build roads & city ground textures (now 100% dirt inside town)
        BuildRoadsMenu.BuildRoads();
        
        // 3. Build Royal Palace & inner stone walls
        BuildRoyalAreaMenu.BuildRoyalArea();
        
        // 4. Build civilian houses
        BuildVillageMenu.BuildVillage();
        
        // 5. Build Military barracks & watchtowers
        BuildMilitaryCampMenu.BuildMilitaryCamp();
        
        // 6. Populate forest with trees, rocks, bushes
        BuildForestMenu.BuildForest();
        
        // 7. Add decorative props
        BuildPropsMenu.BuildProps();
        
        // 8. Fix materials & shaders compatibility
        FixMaterialsMenu.FixMaterialsAndShaders();
        
        // 9. Fix physics colliders
        FixAllCollidersMenu.FixAllColliders();
        
        // 10. Apply skybox, lighting, fog, detail grass
        BuildWorldPolishMenu.ApplyWorldPolish();
        
        // 11. Setup player and camera controller
        SetupPlayerMenu.SetupPlayer();
        
        // 12. Build Rung Hac Am (Map 2)
        BuildRungHacAmMenu.BuildRungHacAmScene();

        // 13. Set up Portals to link Map 1 and Map 2
        BuildPortalsMenu.SetUpPortals();
        
        Debug.Log("Complete Environment Build finished successfully!");
    }
}

