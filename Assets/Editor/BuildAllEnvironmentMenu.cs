using UnityEngine;
using UnityEditor;

public class BuildAllEnvironmentMenu
{
    [MenuItem("Tools/Build Complete Environment")]
    public static void BuildCompleteEnvironment()
    {
        Debug.Log("Starting Complete Environment Build...");
        
        // 1. Build basic map & outer fortress walls
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
        
        Debug.Log("Complete Environment Build finished successfully!");
    }
}
