using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance.Editor
{
    public sealed class CharacterAppearanceTestWindow : EditorWindow
    {
        private CharacterAppearanceConfig _config;
        private SkinnedMeshRenderer _targetRenderer;
        private Color _haircutTint = Color.white;
        private readonly Dictionary<BodypartSlotKind, int> _selectedIndices = new Dictionary<BodypartSlotKind, int>();
        private Vector2 _scrollPosition;

        private bool _includeHaircut;
        private MeshRenderer _haircutTargetRenderer;

        private bool _includeBeard;
        private MeshRenderer _beardTargetRenderer;

        [MenuItem("Simple Survival/Character/Test Appearance Generation")]
        private static void ShowWindow()
        {
            GetWindow<CharacterAppearanceTestWindow>("Test Appearance Gen");
        }

        private void OnGUI()
        {
            _config = (CharacterAppearanceConfig)EditorGUILayout.ObjectField(
                "Config", _config, typeof(CharacterAppearanceConfig), false);

            if (_config == null)
            {
                EditorGUILayout.HelpBox("Chọn CharacterAppearanceConfig để bắt đầu.", MessageType.Info);
                return;
            }

            _targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Target Body Renderer (optional)", _targetRenderer, typeof(SkinnedMeshRenderer), true);

            _haircutTint = EditorGUILayout.ColorField("Haircut Tint", _haircutTint);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chọn biến thể cho từng slot (Atlas):", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(140));
            foreach (BodypartSlotEntry slot in _config.Slots)
            {
                if (!IsAtlasComposite(slot.Kind))
                    continue;

                DrawSlotSelector(slot);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            DrawHaircutSection();

            EditorGUILayout.Space();
            DrawBeardSection();

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate"))
                Generate();
        }

        private void DrawHaircutSection()
        {
            BodypartSlotEntry haircutSlot = FindSlot(BodypartSlotKind.Haircut);
            if (haircutSlot == null || haircutSlot.Bodyparts.Count == 0)
            {
                EditorGUILayout.LabelField("Haircut", "(Không có slot hoặc bodypart nào)");
                return;
            }

            _includeHaircut = EditorGUILayout.ToggleLeft("Bao gồm Haircut (đè lên Head bằng hình học, không bake vào atlas)", _includeHaircut);

            using (new EditorGUI.DisabledScope(!_includeHaircut))
            {
                DrawSlotSelector(haircutSlot);
                _haircutTargetRenderer = (MeshRenderer)EditorGUILayout.ObjectField(
                    "Haircut Target Renderer (optional)", _haircutTargetRenderer, typeof(MeshRenderer), true);
            }
        }

        private void DrawBeardSection()
        {
            BodypartSlotEntry beardSlot = FindSlot(BodypartSlotKind.Beard);
            if (beardSlot == null || beardSlot.Bodyparts.Count == 0)
            {
                EditorGUILayout.LabelField("Beard", "(Không có slot hoặc bodypart nào)");
                return;
            }

            _includeBeard = EditorGUILayout.ToggleLeft("Bao gồm Beard (mượn chính atlas vừa bake)", _includeBeard);

            using (new EditorGUI.DisabledScope(!_includeBeard))
            {
                DrawSlotSelector(beardSlot);
                _beardTargetRenderer = (MeshRenderer)EditorGUILayout.ObjectField(
                    "Beard Target Renderer (optional)", _beardTargetRenderer, typeof(MeshRenderer), true);
            }
        }

        private void DrawSlotSelector(BodypartSlotEntry slot)
        {
            if (slot.Bodyparts.Count == 0)
            {
                EditorGUILayout.LabelField(slot.Kind.ToString(), "(Không có bodypart nào)");
                return;
            }

            string[] options = BuildOptionLabels(slot.Bodyparts);
            int currentIndex = _selectedIndices.TryGetValue(slot.Kind, out int stored) ? stored : 0;
            currentIndex = Mathf.Clamp(currentIndex, 0, options.Length - 1);

            int newIndex = EditorGUILayout.Popup(slot.Kind.ToString(), currentIndex, options);
            _selectedIndices[slot.Kind] = newIndex;
        }

        private static string[] BuildOptionLabels(IReadOnlyList<BodypartResource> bodyparts)
        {
            string[] labels = new string[bodyparts.Count];
            for (int i = 0; i < bodyparts.Count; i++)
            {
                BodypartResource resource = bodyparts[i];
                string name = resource != null
                    ? (string.IsNullOrEmpty(resource.BodypartId) ? resource.name : resource.BodypartId)
                    : "(Empty)";
                labels[i] = i + " " + name;
            }
            return labels;
        }

        private void Generate()
        {
            List<BodypartView> atlasViews = new List<BodypartView>();
            bool hideHaircut = false;
            bool hideBeard = false;

            foreach (BodypartSlotEntry slot in _config.Slots)
            {
                if (!IsAtlasComposite(slot.Kind))
                    continue;

                BodypartResource resource = ResolveSelected(slot);
                if (resource == null)
                    continue;

                atlasViews.Add(new BodypartView(resource, slot));
                hideHaircut |= resource.DisableHaircut;
                hideBeard |= resource.DisableBeard;
            }

            if (atlasViews.Count == 0)
            {
                Debug.LogWarning("Không có bodypart nào được chọn hợp lệ để generate.");
                return;
            }

            Mesh generatedMesh = CharacterAppearanceBuilder.CombineMesh(atlasViews);
            Texture2D generatedAtlas = CharacterAppearanceBuilder.BakeAtlas(
                atlasViews, _config.AtlasSize, _config.AtlasFormat, _haircutTint);

            SaveGeneratedAssets(generatedMesh, generatedAtlas);
            ApplyToTargetRenderer(generatedMesh, generatedAtlas);

            GenerateHaircut(hideHaircut);
            GenerateBeard(generatedAtlas, hideBeard);
        }

        private BodypartResource ResolveSelected(BodypartSlotEntry slot)
        {
            if (slot.Bodyparts.Count == 0)
                return null;

            int index = _selectedIndices.TryGetValue(slot.Kind, out int stored) ? stored : 0;
            index = Mathf.Clamp(index, 0, slot.Bodyparts.Count - 1);
            return slot.Bodyparts[index];
        }

        private void GenerateHaircut(bool hideHaircut)
        {
            if (!_includeHaircut)
                return;

            if (hideHaircut)
            {
                Debug.LogWarning("Head đang chọn có Disable Haircut = true, Haircut sẽ không hiển thị (đúng theo logic thật lúc runtime).");
                return;
            }

            BodypartSlotEntry haircutSlot = FindSlot(BodypartSlotKind.Haircut);
            BodypartResource resource = haircutSlot != null ? ResolveSelected(haircutSlot) : null;
            if (resource == null)
                return;

            Texture2D tintedTexture = CharacterAppearanceBuilder.BuildStandaloneTexture(
                resource.Texture, resource.RegionMask, resource.DetailTexture,
                resource.DetailTiling, resource.DetailOffset, _haircutTint);

            SaveStandaloneAsset(tintedTexture, "TestGeneratedHaircutTexture.asset");
            ApplyToMeshRenderer(_haircutTargetRenderer, resource.Mesh, tintedTexture);
        }

        private void GenerateBeard(Texture2D bodyAtlas, bool hideBeard)
        {
            if (!_includeBeard)
                return;

            if (hideBeard)
            {
                Debug.LogWarning("Torso đang chọn có Disable Beard = true, Beard sẽ không hiển thị (đúng theo logic thật lúc runtime).");
                return;
            }

            BodypartSlotEntry beardSlot = FindSlot(BodypartSlotKind.Beard);
            BodypartResource resource = beardSlot != null ? ResolveSelected(beardSlot) : null;
            if (resource == null)
                return;

            ApplyToMeshRenderer(_beardTargetRenderer, resource.Mesh, bodyAtlas);
        }

        private BodypartSlotEntry FindSlot(BodypartSlotKind kind)
        {
            foreach (BodypartSlotEntry slot in _config.Slots)
            {
                if (slot.Kind == kind)
                    return slot;
            }
            return null;
        }

        private static void SaveGeneratedAssets(Mesh mesh, Texture2D atlas)
        {
            string folder = "Assets/GeneratedAppearanceTest";
            EnsureFolder(folder);

            string meshPath = folder + "/TestGeneratedMesh.asset";
            string atlasPath = folder + "/TestGeneratedAtlas.asset";

            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.DeleteAsset(atlasPath);

            AssetDatabase.CreateAsset(mesh, meshPath);
            AssetDatabase.CreateAsset(atlas, atlasPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Đã lưu: " + meshPath + " và " + atlasPath);
        }

        private static void SaveStandaloneAsset(Texture2D texture, string fileName)
        {
            string folder = "Assets/GeneratedAppearanceTest";
            EnsureFolder(folder);

            string path = folder + "/" + fileName;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(texture, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Đã lưu: " + path);
        }

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "GeneratedAppearanceTest");
        }

        private void ApplyToTargetRenderer(Mesh mesh, Texture2D atlas)
        {
            if (_targetRenderer == null)
                return;

            Undo.RecordObject(_targetRenderer, "Apply Generated Appearance");
            _targetRenderer.sharedMesh = mesh;
            _targetRenderer.localBounds = mesh.bounds;

            Material sourceMaterial = _targetRenderer.sharedMaterial;
            if (sourceMaterial != null)
            {
                Material previewMaterial = new Material(sourceMaterial) { mainTexture = atlas };
                _targetRenderer.sharedMaterial = previewMaterial;
            }

            EditorUtility.SetDirty(_targetRenderer);
        }

        private static void ApplyToMeshRenderer(MeshRenderer renderer, Mesh mesh, Texture2D texture)
        {
            if (renderer == null)
                return;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null)
                return;

            Undo.RecordObject(meshFilter, "Apply Generated Cosmetic Mesh");
            Undo.RecordObject(renderer, "Apply Generated Cosmetic Texture");

            meshFilter.sharedMesh = mesh;

            Material sourceMaterial = renderer.sharedMaterial;
            if (sourceMaterial != null)
            {
                Material previewMaterial = new Material(sourceMaterial) { mainTexture = texture };
                renderer.sharedMaterial = previewMaterial;
            }

            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(renderer);
        }

        private static bool IsAtlasComposite(BodypartSlotKind kind)
        {
            return kind == BodypartSlotKind.Head
                || kind == BodypartSlotKind.Torso
                || kind == BodypartSlotKind.Legs
                || kind == BodypartSlotKind.Feet;
        }
    }
}