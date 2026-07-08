using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance.Editor
{
    public sealed class CharacterAppearanceTestWindow : EditorWindow
    {
        private CharacterAppearanceConfig _config;
        private Material _baseMaterial;
        private Material _previewMaterialInstance;
        private Color _haircutTint = Color.white;
        private readonly Dictionary<BodypartSlotKind, int> _selectedIndices = new Dictionary<BodypartSlotKind, int>();

        private SkinnedMeshRenderer _headTarget;
        private SkinnedMeshRenderer _torsoTarget;
        private SkinnedMeshRenderer _legsTarget;
        private SkinnedMeshRenderer _feetTarget;
        private SkinnedMeshRenderer _backpackTarget;
        private SkinnedMeshRenderer _beardTarget;

        private bool _useHelmet;
        private bool _includeBackpack;
        private bool _includeBeard;

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

            _baseMaterial = (Material)EditorGUILayout.ObjectField(
                "Base Material", _baseMaterial, typeof(Material), false);

            _haircutTint = EditorGUILayout.ColorField("Haircut Tint", _haircutTint);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Torso / Legs / Feet:", EditorStyles.boldLabel);
            DrawSlotSelector(BodypartSlotKind.Torso);
            _torsoTarget = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Torso Renderer", _torsoTarget, typeof(SkinnedMeshRenderer), true);

            DrawSlotSelector(BodypartSlotKind.Legs);
            _legsTarget = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Legs Renderer", _legsTarget, typeof(SkinnedMeshRenderer), true);

            DrawSlotSelector(BodypartSlotKind.Feet);
            _feetTarget = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Feet Renderer", _feetTarget, typeof(SkinnedMeshRenderer), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Head (Helmet thay thế Haircut nếu tick):", EditorStyles.boldLabel);
            _useHelmet = EditorGUILayout.ToggleLeft("Dùng Helmet thay vì Haircut", _useHelmet);
            using (new EditorGUI.DisabledScope(!_useHelmet))
                DrawSlotSelector(BodypartSlotKind.Head);
            using (new EditorGUI.DisabledScope(_useHelmet))
                DrawSlotSelector(BodypartSlotKind.Haircut);
            _headTarget = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Head Renderer", _headTarget, typeof(SkinnedMeshRenderer), true);

            EditorGUILayout.Space();
            _includeBeard = EditorGUILayout.ToggleLeft("Bao gồm Beard", _includeBeard);
            using (new EditorGUI.DisabledScope(!_includeBeard))
            {
                DrawSlotSelector(BodypartSlotKind.Beard);
                _beardTarget = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Beard Renderer", _beardTarget, typeof(SkinnedMeshRenderer), true);
            }

            EditorGUILayout.Space();
            _includeBackpack = EditorGUILayout.ToggleLeft("Bao gồm Backpack", _includeBackpack);
            using (new EditorGUI.DisabledScope(!_includeBackpack))
            {
                DrawSlotSelector(BodypartSlotKind.Backpack);
                _backpackTarget = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Backpack Renderer", _backpackTarget, typeof(SkinnedMeshRenderer), true);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate"))
                Generate();
        }

        private void DrawSlotSelector(BodypartSlotKind kind)
        {
            BodypartSlotEntry slot = FindSlot(kind);
            if (slot == null || slot.Bodyparts.Count == 0)
            {
                EditorGUILayout.LabelField(kind.ToString(), "(Không có slot hoặc bodypart nào)");
                return;
            }

            string[] options = BuildOptionLabels(slot.Bodyparts);
            int currentIndex = _selectedIndices.TryGetValue(kind, out int stored) ? stored : 0;
            currentIndex = Mathf.Clamp(currentIndex, 0, options.Length - 1);

            int newIndex = EditorGUILayout.Popup(kind.ToString(), currentIndex, options);
            _selectedIndices[kind] = newIndex;
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
            BodypartSlotEntry headSlot = FindSlot(BodypartSlotKind.Head);
            BodypartSlotEntry torsoSlot = FindSlot(BodypartSlotKind.Torso);
            BodypartSlotEntry legsSlot = FindSlot(BodypartSlotKind.Legs);
            BodypartSlotEntry feetSlot = FindSlot(BodypartSlotKind.Feet);
            BodypartSlotEntry haircutSlot = FindSlot(BodypartSlotKind.Haircut);
            BodypartSlotEntry backpackSlot = FindSlot(BodypartSlotKind.Backpack);
            BodypartSlotEntry beardSlot = FindSlot(BodypartSlotKind.Beard);

            BodypartResource torsoResource = ResolveSelected(torsoSlot);
            BodypartResource legsResource = ResolveSelected(legsSlot);
            BodypartResource feetResource = ResolveSelected(feetSlot);

            BodypartResource headResource = _useHelmet
                ? ResolveSelected(headSlot)
                : ResolveSelected(haircutSlot);

            BodypartResource backpackResource = _includeBackpack ? ResolveSelected(backpackSlot) : null;

            bool hideBeard = headResource != null && headResource.DisableBeard;
            BodypartResource beardResource = _includeBeard ? ResolveSelected(beardSlot) : null;
            bool beardVisible = beardResource != null && !hideBeard;

            if (_includeBeard && hideBeard)
                Debug.LogWarning("Resource đang lấp Head có Disable Beard = true, Beard sẽ không hiển thị.");

            List<BodypartView> atlasViews = new List<BodypartView>();
            AddView(atlasViews, headSlot, headResource);
            AddView(atlasViews, torsoSlot, torsoResource);
            AddView(atlasViews, legsSlot, legsResource);
            AddView(atlasViews, feetSlot, feetResource);
            AddView(atlasViews, backpackSlot, backpackResource);

            if (atlasViews.Count == 0)
            {
                Debug.LogWarning("Không có bodypart nào hợp lệ để bake atlas.");
                return;
            }

            Texture2D generatedAtlas = CharacterAppearanceBuilder.BakeAtlas(
                atlasViews, _config.AtlasSize, _config.AtlasFormat, _haircutTint);

            SaveGeneratedAtlas(generatedAtlas);
            EnsurePreviewMaterial(generatedAtlas);

            ApplyToRenderer(_headTarget, headResource);
            ApplyToRenderer(_torsoTarget, torsoResource);
            ApplyToRenderer(_legsTarget, legsResource);
            ApplyToRenderer(_feetTarget, feetResource);
            ApplyToRenderer(_backpackTarget, backpackResource);
            ApplyToRenderer(_beardTarget, beardVisible ? beardResource : null);
        }

        private static void AddView(List<BodypartView> views, BodypartSlotEntry slot, BodypartResource resource)
        {
            if (slot == null || resource == null)
                return;

            views.Add(new BodypartView(resource, slot));
        }

        private BodypartResource ResolveSelected(BodypartSlotEntry slot)
        {
            if (slot == null || slot.Bodyparts.Count == 0)
                return null;

            int index = _selectedIndices.TryGetValue(slot.Kind, out int stored) ? stored : 0;
            index = Mathf.Clamp(index, 0, slot.Bodyparts.Count - 1);
            return slot.Bodyparts[index];
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

        private static void SaveGeneratedAtlas(Texture2D atlas)
        {
            string folder = "Assets/GeneratedAppearanceTest";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "GeneratedAppearanceTest");

            string atlasPath = folder + "/TestGeneratedAtlas.asset";
            AssetDatabase.DeleteAsset(atlasPath);
            AssetDatabase.CreateAsset(atlas, atlasPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Đã lưu: " + atlasPath);
        }

        private void EnsurePreviewMaterial(Texture2D atlas)
        {
            if (_baseMaterial == null)
                return;

            if (_previewMaterialInstance == null)
                _previewMaterialInstance = new Material(_baseMaterial);

            _previewMaterialInstance.mainTexture = atlas;
        }

        private void ApplyToRenderer(SkinnedMeshRenderer renderer, BodypartResource resource)
        {
            if (renderer == null)
                return;

            bool visible = resource != null;
            renderer.gameObject.SetActive(visible);
            if (!visible)
                return;

            Undo.RecordObject(renderer, "Apply Generated Appearance");
            renderer.sharedMesh = resource.Mesh;
            renderer.localBounds = resource.Mesh.bounds;

            if (_previewMaterialInstance != null)
                renderer.sharedMaterial = _previewMaterialInstance;

            EditorUtility.SetDirty(renderer);
        }
    }
}