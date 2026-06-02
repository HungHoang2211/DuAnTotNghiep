using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SetupSplatGround : MonoBehaviour
{
    [Header("Inputs")]
    public Terrain terrain;
    public MeshRenderer targetRenderer;
    public Shader splatShader;

    [Header("Output")]
    public string splatmapAssetName = "TerrainSplatmap";

    [ContextMenu("Setup Splat Ground")]
    public void Setup()
    {
        if (terrain == null || targetRenderer == null || splatShader == null)
        {
            Debug.LogError("Setup: thiếu Terrain / Target Renderer / Splat Shader.");
            return;
        }

        var data = terrain.terrainData;
        var mat = targetRenderer.sharedMaterial;
        if (mat == null)
        {
            Debug.LogError("Setup: plane chưa có material. Kéo material vào plane trước.");
            return;
        }

        // ===== Lưu splatmap thành PNG asset =====
        Texture2D splatTex = null;
#if UNITY_EDITOR
        Texture2D src = data.alphamapTextures[0];

        RenderTexture rt = RenderTexture.GetTemporary(
            src.width, src.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(src, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, true);
        readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        readable.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        string path = $"Assets/{splatmapAssetName}.png";
        System.IO.File.WriteAllBytes(path, readable.EncodeToPNG());
        DestroyImmediate(readable);

        AssetDatabase.Refresh();

        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.sRGBTexture = false;                              // splatmap là dữ liệu
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.SaveAndReimport();

        splatTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#else
        splatTex = data.alphamapTextures[0];
#endif

        mat.SetTexture("_Control", splatTex);

        // ===== Layer textures + tile sizes =====
        var layers = data.terrainLayers;
        string[] sp = { "_Splat0", "_Splat1", "_Splat2", "_Splat3" };
        string[] tl = { "_Tile0", "_Tile1", "_Tile2", "_Tile3" };

        int count = Mathf.Min(4, layers.Length);
        for (int i = 0; i < count; i++)
        {
            if (layers[i] == null) continue;
            mat.SetTexture(sp[i], layers[i].diffuseTexture);
            mat.SetFloat(tl[i], layers[i].tileSize.x);
        }

        // ===== Origin cố định = -size/2 =====
        // Shader đã tự trừ vị trí plane -> không cần biết plane ở đâu.
        // Plane Unity gốc ở TÂM mesh, alphamap gốc ở GÓC -> bù bằng -size/2.
        Vector3 size = data.size;
        Vector3 origin = new Vector3(-size.x * 0.5f, 0, -size.z * 0.5f);

        mat.SetVector("_TerrainOrigin", new Vector4(origin.x, 0, origin.z, 0));
        mat.SetVector("_TerrainSize", new Vector4(size.x, 0, size.z, 0));

        targetRenderer.sharedMaterial = mat;

#if UNITY_EDITOR
        Debug.Log($"Setup OK. Splatmap: {path} ({splatTex.width}x{splatTex.height}). " +
                  $"Size=({size.x},{size.z})");
#endif
    }
}