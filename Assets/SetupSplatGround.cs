using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SplatmapResolution
{
    Res256 = 256,
    Res512 = 512,
    Res1024 = 1024
}

public class SetupSplatGround : MonoBehaviour
{
    [Header("Inputs")]
    public Terrain terrain;
    public MeshRenderer targetRenderer;
    public Shader splatShader;

    [Header("Output")]
    public string splatmapAssetName = "TerrainSplatmap";
    public SplatmapResolution splatmapResolution = SplatmapResolution.Res512;

    [ContextMenu("Setup Splat Ground")]
    public void Setup()
    {
        if (!ValidateInputs()) return;

        var data = terrain.terrainData;
        var mat = targetRenderer.sharedMaterial;
        if (mat == null)
        {
            Debug.LogError("Setup: plane chưa có material. Kéo material vào plane trước.");
            return;
        }

        ApplyControlMaps(data, mat);
        ApplyLayers(data.terrainLayers, mat);
        ApplyTerrainBounds(data.size, mat);

        targetRenderer.sharedMaterial = mat;

#if UNITY_EDITOR
        Debug.Log($"Setup OK. Splatmap resolution={(int)splatmapResolution}. " +
                  $"Size=({data.size.x},{data.size.z}). Layers={data.terrainLayers.Length}.");
#endif
    }

    private bool ValidateInputs()
    {
        if (terrain == null || targetRenderer == null || splatShader == null)
        {
            Debug.LogError("Setup: thiếu Terrain / Target Renderer / Splat Shader.");
            return false;
        }
        return true;
    }

    private void ApplyControlMaps(TerrainData data, Material mat)
    {
        int resolution = (int)splatmapResolution;
        var alphamaps = data.alphamapTextures;

        if (alphamaps.Length > 0)
        {
            var tex0 = BakeSplatmap(alphamaps[0], $"{splatmapAssetName}_0", resolution);
            mat.SetTexture("_Control", tex0);
        }

        if (alphamaps.Length > 1)
        {
            var tex1 = BakeSplatmap(alphamaps[1], $"{splatmapAssetName}_1", resolution);
            mat.SetTexture("_Control2", tex1);
        }
        else
        {
            mat.SetTexture("_Control2", Texture2D.blackTexture);
        }
    }

    private Texture2D BakeSplatmap(Texture2D source, string assetName, int resolution)
    {
#if UNITY_EDITOR
        var rt = RenderTexture.GetTemporary(
            resolution, resolution, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var readable = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
        readable.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        readable.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        string path = $"Assets/{assetName}.png";
        System.IO.File.WriteAllBytes(path, readable.EncodeToPNG());
        DestroyImmediate(readable);
        AssetDatabase.Refresh();

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#else
        return source;
#endif
    }

    private void ApplyLayers(TerrainLayer[] layers, Material mat)
    {
        string[] splatProps =
        {
            "_Splat0", "_Splat1", "_Splat2", "_Splat3",
            "_Splat4", "_Splat5", "_Splat6", "_Splat7"
        };
        string[] tileProps =
        {
            "_Tile0", "_Tile1", "_Tile2", "_Tile3",
            "_Tile4", "_Tile5", "_Tile6", "_Tile7"
        };

        int count = Mathf.Min(8, layers.Length);
        for (int i = 0; i < count; i++)
        {
            if (layers[i] == null) continue;
            mat.SetTexture(splatProps[i], layers[i].diffuseTexture);
            mat.SetFloat(tileProps[i], layers[i].tileSize.x);
        }
    }

    private void ApplyTerrainBounds(Vector3 size, Material mat)
    {
        Vector3 origin = new Vector3(-size.x * 0.5f, 0, -size.z * 0.5f);
        mat.SetVector("_TerrainOrigin", new Vector4(origin.x, 0, origin.z, 0));
        mat.SetVector("_TerrainSize", new Vector4(size.x, 0, size.z, 0));
    }
}