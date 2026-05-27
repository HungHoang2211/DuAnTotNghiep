using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TerrainToMesh : MonoBehaviour
{
    [Header("Terrain Settings")]
    public Terrain terrain;

    [Header("Mesh Generation Settings")]
    [Range(1, 10)]
    public int simplification = 1; // Упрощение геометрии (1 = полное качество)

    [Header("Output Settings")]
    public string meshName = "TerrainMesh";
    public bool createPrefab = true;
    public bool saveMeshAsset = true;

    [Header("Texture Baking Settings")]
    public int bakedTextureSize = 1024;
    public string textureOutputName = "BakedTerrainTexture";

    [ContextMenu("Convert Terrain to Mesh")]
    public void ConvertTerrainToMesh()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain is not assigned!");
            return;
        }

        Debug.Log("Starting terrain conversion...");

        TerrainData terrainData = terrain.terrainData;

        // Получаем данные высот
        int heightmapWidth = terrainData.heightmapResolution;
        int heightmapHeight = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

        // Применяем упрощение
        int simplifiedWidth = heightmapWidth / simplification;
        int simplifiedHeight = heightmapHeight / simplification;

        // Создаем вершины
        Vector3[] vertices = new Vector3[simplifiedWidth * simplifiedHeight];
        Vector2[] uvs = new Vector2[vertices.Length];
        Vector3[] normals = new Vector3[vertices.Length];

        Vector3 terrainSize = terrainData.size;

        int vertIndex = 0;
        for (int y = 0; y < simplifiedHeight; y++)
        {
            for (int x = 0; x < simplifiedWidth; x++)
            {
                // Позиция в исходном heightmap
                int originalX = x * simplification;
                int originalY = y * simplification;

                // Убеждаемся что не выходим за границы
                originalX = Mathf.Min(originalX, heightmapWidth - 1);
                originalY = Mathf.Min(originalY, heightmapHeight - 1);

                // Вычисляем позицию вершины
                float height = heights[originalY, originalX] * terrainSize.y;
                float posX = (float)x / (simplifiedWidth - 1) * terrainSize.x;
                float posZ = (float)y / (simplifiedHeight - 1) * terrainSize.z;

                vertices[vertIndex] = new Vector3(posX, height, posZ);
                uvs[vertIndex] = new Vector2((float)x / (simplifiedWidth - 1), (float)y / (simplifiedHeight - 1));

                // Вычисляем нормаль
                normals[vertIndex] = CalculateNormal(heights, originalX, originalY, heightmapWidth, heightmapHeight, terrainSize);

                vertIndex++;
            }
        }

        // Создаем треугольники
        int[] triangles = new int[(simplifiedWidth - 1) * (simplifiedHeight - 1) * 6];
        int triIndex = 0;

        for (int y = 0; y < simplifiedHeight - 1; y++)
        {
            for (int x = 0; x < simplifiedWidth - 1; x++)
            {
                int bottomLeft = y * simplifiedWidth + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (y + 1) * simplifiedWidth + x;
                int topRight = topLeft + 1;

                // Первый треугольник
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;

                // Второй треугольник
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
        }

        // Создаем меш
        Mesh mesh = new Mesh();
        mesh.name = meshName;

        // Unity имеет лимит в 65536 вершин для обычного меша
        if (vertices.Length > 65536)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;

        // Пересчитываем bounds
        mesh.RecalculateBounds();

        Debug.Log($"Mesh created with {vertices.Length} vertices and {triangles.Length / 3} triangles");

        // Создаем GameObject с мешем
        GameObject meshObject = new GameObject(meshName);
        meshObject.transform.position = terrain.transform.position;

        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        // Создаем базовый материал без запекания текстур
        Material finalMaterial = null;

        if (terrain.materialTemplate != null)
        {
            finalMaterial = new Material(terrain.materialTemplate);
        }
        else
        {
            // Используем URP Lit или Standard как fallback
            Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
            if (fallbackShader == null)
            {
                fallbackShader = Shader.Find("Standard");
            }
            finalMaterial = new Material(fallbackShader);
        }

        meshRenderer.material = finalMaterial;

#if UNITY_EDITOR
        // Сохраняем меш как ассет
        if (saveMeshAsset)
        {
            string assetPath = $"Assets/{meshName}.asset";
            AssetDatabase.CreateAsset(mesh, assetPath);
            Debug.Log($"Mesh saved as asset: {assetPath}");
        }

        // Создаем префаб
        if (createPrefab)
        {
            string prefabPath = $"Assets/{meshName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(meshObject, prefabPath);
            Debug.Log($"Prefab created: {prefabPath}");
        }

        // Обновляем Asset Database
        AssetDatabase.Refresh();
#endif

        Debug.Log("Terrain conversion completed!");
    }

    public Texture2D BakeTerrainTextures()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain is not assigned!");
            return null;
        }

        Debug.Log("Baking terrain textures...");

        // Временно исправляем материал террейна для запекания
        Material originalMaterial = terrain.materialTemplate;
        bool materialWasFixed = false;

        if (terrain.materialTemplate != null)
        {
            // Проверяем, розовый ли материал (несовместимый шейдер)
            Shader terrainShader = terrain.materialTemplate.shader;
            if (terrainShader == null || terrainShader.name.Contains("Hidden") ||
                terrainShader.name.Contains("Legacy") || terrainShader.name.Contains("Standard"))
            {
                // Создаем временный URP материал
                Shader urpTerrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                if (urpTerrainShader == null)
                {
                    urpTerrainShader = Shader.Find("Universal Render Pipeline/Lit");
                }

                if (urpTerrainShader != null)
                {
                    Material tempMaterial = new Material(urpTerrainShader);

                    // Копируем основные свойства если возможно
                    if (originalMaterial.HasProperty("_MainTex"))
                    {
                        tempMaterial.mainTexture = originalMaterial.mainTexture;
                    }

                    terrain.materialTemplate = tempMaterial;
                    materialWasFixed = true;
                    Debug.Log("Temporarily fixed terrain material for baking");
                }
            }
        }
        else
        {
            // Если материала нет вообще, создаем базовый URP материал
            Shader urpTerrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (urpTerrainShader == null)
            {
                urpTerrainShader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (urpTerrainShader != null)
            {
                terrain.materialTemplate = new Material(urpTerrainShader);
                materialWasFixed = true;
                Debug.Log("Created temporary URP material for terrain");
            }
        }

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        // Создаем временную камеру для запекания
        GameObject cameraObj = new GameObject("TerrainBakingCamera");
        Camera bakingCamera = cameraObj.AddComponent<Camera>();

        // Настройка камеры для ортографического вида сверху
        bakingCamera.transform.position = terrainPos + new Vector3(terrainSize.x / 2, terrainSize.y + 50, terrainSize.z / 2);
        bakingCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        bakingCamera.orthographic = true;
        bakingCamera.orthographicSize = Mathf.Max(terrainSize.x, terrainSize.z) / 2;
        bakingCamera.nearClipPlane = 0.1f;
        bakingCamera.farClipPlane = terrainSize.y + 100;
        bakingCamera.aspect = 1.0f;

        // Создаем RenderTexture
        RenderTexture renderTexture = new RenderTexture(bakedTextureSize, bakedTextureSize, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 1;
        bakingCamera.targetTexture = renderTexture;

        // Сохраняем текущие настройки освещения
        bool originalFog = RenderSettings.fog;
        UnityEngine.Rendering.AmbientMode originalAmbientMode = RenderSettings.ambientMode;
        Color originalAmbientColor = RenderSettings.ambientLight;

        // Отключаем туман и настраиваем освещение для лучшего результата
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;

        // Рендерим сцену
        bakingCamera.Render();

        // Читаем пиксели из RenderTexture
        RenderTexture.active = renderTexture;
        Texture2D bakedTexture = new Texture2D(bakedTextureSize, bakedTextureSize, TextureFormat.RGB24, false);
        bakedTexture.ReadPixels(new Rect(0, 0, bakedTextureSize, bakedTextureSize), 0, 0);
        bakedTexture.Apply();
        bakedTexture.name = textureOutputName;

        // Восстанавливаем настройки освещения
        RenderSettings.fog = originalFog;
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientLight = originalAmbientColor;

        // Восстанавливаем оригинальный материал террейна
        if (materialWasFixed)
        {
            terrain.materialTemplate = originalMaterial;
            Debug.Log("Restored original terrain material");
        }

        // Очищаем временные объекты
        RenderTexture.active = null;
        bakingCamera.targetTexture = null;
        renderTexture.Release();
        DestroyImmediate(cameraObj);

#if UNITY_EDITOR
        // Сохраняем текстуру как PNG
        byte[] pngData = bakedTexture.EncodeToPNG();
        string texturePath = $"Assets/{textureOutputName}.png";
        System.IO.File.WriteAllBytes(texturePath, pngData);

        // Импортируем текстуру и настраиваем параметры
        AssetDatabase.Refresh();
        TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        if (textureImporter != null)
        {
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.maxTextureSize = bakedTextureSize;
            AssetDatabase.ImportAsset(texturePath);
        }

        // Загружаем сохраненную текстуру
        Texture2D savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (savedTexture != null)
        {
            Debug.Log($"Terrain texture baked and saved: {texturePath}");
            return savedTexture;
        }
#endif

        Debug.Log("Terrain texture baked (runtime only)");
        return bakedTexture;
    }

    [ContextMenu("Bake Terrain Texture")]
    public void BakeTerrainTexture()
    {
        Texture2D bakedTexture = BakeTerrainTextures();
        if (bakedTexture != null)
        {
            Debug.Log($"Texture baking completed! Size: {bakedTexture.width}x{bakedTexture.height}");
        }
    }

    Vector3 CalculateNormal(float[,] heights, int x, int y, int width, int height, Vector3 terrainSize)
    {
        // Получаем соседние высоты для вычисления нормали
        float heightL = GetHeight(heights, x - 1, y, width, height);
        float heightR = GetHeight(heights, x + 1, y, width, height);
        float heightD = GetHeight(heights, x, y - 1, width, height);
        float heightU = GetHeight(heights, x, y + 1, width, height);

        // Вычисляем векторы
        Vector3 normal = new Vector3(
            (heightL - heightR) * terrainSize.y,
            2.0f,
            (heightD - heightU) * terrainSize.y
        );

        return normal.normalized;
    }

    float GetHeight(float[,] heights, int x, int y, int width, int height)
    {
        // Ограничиваем координаты границами массива
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        return heights[y, x];
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainToMesh))]
public class TerrainToMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainToMesh converter = (TerrainToMesh)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Bake Terrain Texture"))
        {
            converter.BakeTerrainTexture();
        }

        if (GUILayout.Button("Convert Terrain to Mesh"))
        {
            converter.ConvertTerrainToMesh();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Simplification: 1 = full quality, higher values = less polygons", MessageType.Info);

        if (converter.terrain != null)
        {
            TerrainData data = converter.terrain.terrainData;
            int originalVerts = data.heightmapResolution * data.heightmapResolution;
            int simplifiedVerts = (data.heightmapResolution / converter.simplification) * (data.heightmapResolution / converter.simplification);

            EditorGUILayout.LabelField($"Original vertices: {originalVerts:N0}");
            EditorGUILayout.LabelField($"Simplified vertices: {simplifiedVerts:N0}");
        }
    }
}
#endif