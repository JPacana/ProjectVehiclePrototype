using System.IO;
using PrefabThumbnails;
using UnityEditor;
using UnityEngine;

public class ThumbnailGenerator : MonoBehaviour
{
    [SerializeField] private InventorySO _inventory;
    private PrefabThumbnailRenderer _renderer;
    public PrefabThumbnailRenderer Renderer { get { return _renderer; } }
    void Start()
    {
        _renderer = new PrefabThumbnailRenderer(thumbnailSize: 256);
        _renderer.Setup();
        _renderer.OnThumbnailReady += (key, anim) =>
        {
            // `anim` is an AnimatedThumbnail — Static thumbnails have FrameCount=1.
            //UpdateCardImage(key, anim.FirstFrame);
            Debug.Log($"Thumbnail ready for {key}");
            //var thumbnail = _renderer.GetThumbnail(key);
            //SaveTextureToResources(thumbnail, $"{key}");
            
        };

        foreach (var item in _inventory.Items)
        {
            Debug.Log($"Item: {item.Id}");
            _renderer.Queue(new ThumbnailRequest
            {
                Key = item.Id,
                Prefab = item.VehicleAttachment
                //PreRenderCallback = inst =>
                //{
                //    var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>();
                //    if (smr != null) smr.material = item.SkinMaterial;
                //},
                //PostProcessCallback = item.Sprite
            });
        }
        
        //_renderer = new PrefabThumbnailRenderer(thumbnailSize: 256);
        //_renderer.Setup();

        //foreach (var item in _inventory.Items)
        //{
        //    // Static — one frame, cached forever. The 99% case.
        //    _renderer.Queue(new ThumbnailRequest { Key = item.Name.GetHashCode(), Prefab = item.VehicleAttachment});
        //}
    }

    void Update()
    {
        _renderer.Tick();
    }
    
    public void SaveTextureToResources(Texture2D texture, string fileName)
    {
        // 1. Encode texture to bytes
        byte[] bytes = texture.EncodeToPNG(); // Or EncodeToJPG()

        // 2. Define path inside the Assets folder
        string folderPath = Application.dataPath + "/Resources/";
        string filePath = folderPath + fileName + ".png";

        // 3. Ensure directory exists and write file
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        File.WriteAllBytes(filePath, bytes);

        // 4. Refresh AssetDatabase so Unity acknowledges the new file
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
        Debug.Log($"Saved to Resources: {filePath}");
    }
}
