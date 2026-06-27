using System.IO;
using UnityEditor;
using UnityEngine;

public class ThumbnailGeneratorTool : EditorWindow
{
    private int resolution = 256;
    private Color backgroundColor = new Color(0, 0, 0, 0);
    private InventorySO inventory;

    [MenuItem("Tools/Thumbnail Generator")]
    public static void ShowWindow()
    {
        GetWindow<ThumbnailGeneratorTool>("Thumbnail Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate Prefab Thumbnail", EditorStyles.boldLabel);
        
        resolution = EditorGUILayout.IntField("Resolution", resolution);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        inventory = (InventorySO)EditorGUILayout.ObjectField("Inventory", inventory, typeof(InventorySO), false);

        if (GUILayout.Button("Generate"))
        {
            GenerateThumbnails();
        }
    }

    private void GenerateThumbnails()
    {
        foreach (var item in inventory.Items)
        {
            GenerateThumbnail(item);
        }
    }
    
    private void GenerateThumbnail(InventoryItemSO item)
    {
        GameObject spawned = Instantiate(item.VehicleAttachment, Vector3.zero, Quaternion.identity);
        
        RenderTexture rt = new RenderTexture(resolution, resolution, 24);
        Camera.main.targetTexture = rt;
        
        Texture2D screenshot = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

        Camera.main.Render();
        
        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        screenshot.Apply();
        
        Camera.main.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(spawned);
        
        byte[] bytes = screenshot.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, "_Project/Art/Thumbnails", $"{item.Name}.png");
        File.WriteAllBytes(path, bytes);

        AssetDatabase.Refresh();
    }
}
