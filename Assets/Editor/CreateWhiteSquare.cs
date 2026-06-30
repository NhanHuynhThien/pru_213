using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateWhiteSquare
{
    [MenuItem("Tools/PRU213/Create White Square Sprite")]
    public static void Create()
    {
        string dir = "Assets/UI";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string path = dir + "/WhiteSquare.png";
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Color[] cols = new Color[] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(cols);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        // Tự động cấu hình file vừa tạo thành Sprite (2D and UI)
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; // Giữ nguyên độ sắc nét
            importer.SaveAndReimport();
        }

        Debug.Log("<color=green>[Tools]</color> Đã tạo thành công ảnh WhiteSquare.png sắc nét tại Assets/UI!");
    }
}
