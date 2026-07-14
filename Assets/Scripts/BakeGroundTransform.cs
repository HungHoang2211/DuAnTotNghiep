using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BakeGroundTransform : MonoBehaviour
{
    [Header("Source (Plane cũ, đang xoay theo ý đồ hiển thị)")]
    public MeshFilter sourceMeshFilter;
    public MeshRenderer sourceMeshRenderer;

    [Header("Output")]
    public string meshAssetName = "GroundBakedMesh";

    [ContextMenu("Bake Clean Transform")]
    public void Bake()
    {
        if (!ValidateInputs()) return;

        Mesh sourceMesh = sourceMeshFilter.sharedMesh;
        Transform sourceTransform = sourceMeshFilter.transform;

        Mesh bakedMesh = BuildBakedMesh(sourceTransform, sourceMesh);
        bakedMesh = SaveMeshAsset(bakedMesh);
        ApplyToTarget(bakedMesh);

        Debug.Log($"Bake Clean Transform OK. Vertices={bakedMesh.vertexCount}.");
    }

    private bool ValidateInputs()
    {
        if (sourceMeshFilter == null || sourceMeshRenderer == null)
        {
            Debug.LogError("Bake: thiếu Source Mesh Filter / Source Mesh Renderer.");
            return false;
        }
        if (sourceMeshFilter.sharedMesh == null)
        {
            Debug.LogError("Bake: Source chưa có Mesh.");
            return false;
        }
        if (GetComponent<MeshFilter>() == null || GetComponent<MeshRenderer>() == null)
        {
            Debug.LogError("Bake: GameObject đích thiếu MeshFilter / MeshRenderer.");
            return false;
        }
        return true;
    }

    private Mesh BuildBakedMesh(Transform sourceTransform, Mesh sourceMesh)
    {
        Vector3[] sourceVertices = sourceMesh.vertices;
        Vector3[] sourceNormals = sourceMesh.normals;

        Vector3[] bakedVertices = new Vector3[sourceVertices.Length];
        Vector3[] bakedNormals = new Vector3[sourceNormals.Length];

        for (int i = 0; i < sourceVertices.Length; i++)
        {
            bakedVertices[i] = sourceTransform.TransformPoint(sourceVertices[i]);
        }

        for (int i = 0; i < sourceNormals.Length; i++)
        {
            bakedNormals[i] = sourceTransform.TransformDirection(sourceNormals[i]).normalized;
        }

        Mesh bakedMesh = new Mesh();
        bakedMesh.name = meshAssetName;
        bakedMesh.vertices = bakedVertices;
        bakedMesh.normals = bakedNormals;
        bakedMesh.uv = sourceMesh.uv;
        bakedMesh.triangles = sourceMesh.triangles;
        bakedMesh.RecalculateBounds();

        return bakedMesh;
    }

    private Mesh SaveMeshAsset(Mesh bakedMesh)
    {
#if UNITY_EDITOR
        string path = $"Assets/{meshAssetName}.asset";

        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(bakedMesh, path);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
#else
        return bakedMesh;
#endif
    }

    private void ApplyToTarget(Mesh bakedMesh)
    {
        MeshFilter targetMeshFilter = GetComponent<MeshFilter>();
        MeshRenderer targetMeshRenderer = GetComponent<MeshRenderer>();

        targetMeshFilter.sharedMesh = bakedMesh;
        targetMeshRenderer.sharedMaterial = sourceMeshRenderer.sharedMaterial;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BakeGroundTransform))]
public class BakeGroundTransformEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8);

        BakeGroundTransform bake = (BakeGroundTransform)target;
        if (GUILayout.Button("Bake Clean Transform", GUILayout.Height(30)))
        {
            bake.Bake();
        }
    }
}
#endif