using Dummiesman;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace wackydatabase.OBJimporter;

//[Market_Autoload(Market_Autoload.Type.Client, Market_Autoload.Priority.Normal, "OnInit")] Thanks KG
public static class ObjModelLoader
{
    public static readonly Dictionary<string, GameObject> _loadedModels = new();
    public static readonly Dictionary<string, Sprite> _loadedIcons = new();
    private static readonly Dictionary<string, string> pngFiles = new();
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int MetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");
    private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
    public static GameObject MockItemBase;
    public static AssetBundle asset;

    internal static void ClearObjs()
    {
        foreach (GameObject model in _loadedModels.Values)
        {
            if (model != null)
            {
                UnityEngine.Object.Destroy(model);
            }
        }

        foreach (Sprite icon in _loadedIcons.Values)
        {
            if (icon != null)
            {
                UnityEngine.Object.Destroy(icon.texture);
                UnityEngine.Object.Destroy(icon);
            }
        }

        _loadedModels.Clear();
        _loadedIcons.Clear();
        pngFiles.Clear();

    }
    private static AssetBundle GetAssetBundle(string filename)
    {
        Assembly execAssembly = Assembly.GetExecutingAssembly();
        string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));
        using Stream stream = execAssembly.GetManifestResourceStream(resourceName);
        return AssetBundle.LoadFromStream(stream);
    }
    internal static void OnInit()
    {
        if (MockItemBase != null)
        {
            return;
        }

        asset = GetAssetBundle("rootcube");
        MockItemBase = asset.LoadAsset<GameObject>("RootCube");
        asset.Unload(false);
        asset = null;
    }

    internal static void LoadObjs()
    {
        OnInit();
        foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathObjects, "*.png", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            pngFiles.Add(fileName, file);
        }
        foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathObjects, "*.obj", SearchOption.AllDirectories))
        {
            try
            {
                GameObject obj = new OBJLoader().Load(file);
                obj.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(obj);
                string fileName = Path.GetFileNameWithoutExtension(file);
                _loadedModels.Add(fileName, obj);
                ParsePNGs(obj, fileName);
                AddColliders(obj);
                MakeMeshesNonReadable(obj);
            }
            catch (Exception ex)
            {
                WMRecipeCust.WLog.LogInfo($"Failed to load model {file}\n: {ex}");
            }
        }       
        foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathObjects, "*.fbx", SearchOption.AllDirectories))
        {
            try
            {
                GameObject obj = Resources.Load<GameObject>(file);
                UnityEngine.Object.DontDestroyOnLoad(obj);
                string fileName = Path.GetFileNameWithoutExtension(file);
                _loadedModels.Add(fileName, obj);
                //ParsePNGs(obj, fileName);
                AddColliders(obj);
            }
            catch (Exception ex)
            {
                WMRecipeCust.WLog.LogInfo($"Failed to load model fbx {file}\n: {ex}");
            }
        }
    }


    private static void ParsePNGs(GameObject go, string name)
    {
        string albedo = name + "_albedo";
        string metallic = name + "_metallic";
        string normal = name + "_normal";
        string icon = name + "_icon";

        if (pngFiles.TryGetValue(icon, out string iconFile))
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(iconFile), true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            _loadedIcons.Add(name, sprite);
        }

        var meshes = go.GetComponentsInChildren<MeshRenderer>();
        if (pngFiles.TryGetValue(albedo, out string file))
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(file), true);

            foreach (var mesh in meshes)
            {
                mesh.material.SetTexture(MainTex, texture);
            }
        }

        if (pngFiles.TryGetValue(metallic, out file))
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(file), true);

            foreach (var mesh in meshes)
            {
                mesh.material.SetTexture(MetallicGlossMap, texture);
            }
        }

        if (pngFiles.TryGetValue(normal, out file))
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(file), true);

            foreach (var mesh in meshes)
            {
                mesh.material.SetTexture(BumpMap, texture);
            }
        }
    }

    private static void AddColliders(GameObject go)
    {
        MeshFilter[] meshes = go.GetComponentsInChildren<MeshFilter>(true);
        bool hasBounds = false;
        Bounds combinedBounds = default;

        foreach (MeshFilter meshFilter in meshes)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            Bounds bounds = mesh.bounds;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                        Vector3 localCorner = go.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(corner));
                        if (!hasBounds)
                        {
                            combinedBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            combinedBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        if (hasBounds)
        {
            BoxCollider boxCollider = go.AddComponent<BoxCollider>();
            boxCollider.center = combinedBounds.center;
            boxCollider.size = combinedBounds.size;
        }
    }

    private static void MakeMeshesNonReadable(GameObject go)
    {
        foreach (MeshFilter meshFilter in go.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh != null && mesh.isReadable)
            {
                mesh.UploadMeshData(true);
            }
        }
    }
}