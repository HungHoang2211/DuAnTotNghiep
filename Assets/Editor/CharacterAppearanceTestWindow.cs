using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance.Editor
{
    public sealed class CharacterAppearanceTestWindow : EditorWindow
    {
        private CharacterAppearanceConfig _config;
        private SkinnedMeshRenderer _bodyTargetRenderer;
        private SkinnedMeshRenderer _backpackTargetRenderer;
        private Color _haircutTint = Color.white;
        private readonly Dictionary<BodypartSlotKind, int> _selectedIndices = new Dictionary<BodypartSlotKind, int>();
        private Vector2 _scrollPosition;

        private bool _useHelmet;
        private bool _includeBeard;
        private bool _includeBackpack;

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

            _bodyTargetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Body Target Renderer (optional)", _bodyTargetRenderer, typeof(SkinnedMeshRenderer), true);

            _haircutTint = EditorGUILayout.ColorField("Haircut Tint", _haircutTint);

            EditorGUILayout.Space();

            DrawFixedSlot(BodypartSlotKind.Torso);
            DrawFixedSlot(BodypartSlotKind.Legs);
            DrawFixedSlot(BodypartSlotKind.Feet);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Head (Helmet thay thế Haircut nếu tick):", EditorStyles.boldLabel);
            _useHelmet = EditorGUILayout.ToggleLeft("Dùng Helmet (Head slot) thay vì Haircut", _useHelmet);

            using (new EditorGUI.DisabledScope(!_useHelmet))
            {
                DrawFixedSlot(BodypartSlotKind.Head);
            }

            using (new EditorGUI.DisabledScope(_useHelmet))
            {
                DrawFixedSlot(BodypartSlotKind.Haircut);
            }

            EditorGUILayout.Space();
            _includeBeard = EditorGUILayout.ToggleLeft("Bao gồm Beard (mesh-only, mượn atlas)", _includeBeard);
            using (new EditorGUI.DisabledScope(!_includeBeard))
            {
                DrawFixedSlot(BodypartSlotKind.Beard);
            }

            EditorGUILayout.Space();
            _includeBackpack = EditorGUILayout.ToggleLeft("Bao gồm Backpack (texture riêng, renderer riêng)", _includeBackpack);
            using (new EditorGUI.DisabledScope(!_includeBackpack))
            {
                DrawFixedSlot(BodypartSlotKind.Backpack);
                _backpackTargetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                    "Backpack Target Renderer", _backpackTargetRenderer, typeof(SkinnedMeshRenderer), true);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate"))
                Generate();
        }

        private void DrawFixedSlot(BodypartSlotKind kind)
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
            BodypartSlotEntry torsoSlot = FindSlot(BodypartSlotKind.Torso);
            BodypartSlotEntry legsSlot = FindSlot(BodypartSlotKind.Legs);
            BodypartSlotEntry feetSlot = FindSlot(BodypartSlotKind.Feet);
            BodypartSlotEntry headSlot = FindSlot(BodypartSlotKind.Head);
            BodypartSlotEntry haircutSlot = FindSlot(BodypartSlotKind.Haircut);
            BodypartSlotEntry beardSlot = FindSlot(BodypartSlotKind.Beard);

            List<BodypartView> atlasViews = new List<BodypartView>();

            AddIfPresent(atlasViews, torsoSlot, ResolveSelected(torsoSlot));
            AddIfPresent(atlasViews, legsSlot, ResolveSelected(legsSlot));
            AddIfPresent(atlasViews, feetSlot, ResolveSelected(feetSlot));

            BodypartResource headFillerResource = _useHelmet
                ? ResolveSelected(headSlot)
                : ResolveSelected(haircutSlot);

            AddIfPresent(atlasViews, headSlot, headFillerResource);

            if (atlasViews.Count == 0)
            {
                Debug.LogWarning("Không có bodypart nào được chọn hợp lệ để generate.");
                return;
            }

            bool hideBeard = headFillerResource != null && headFillerResource.DisableBeard;
            BodypartResource beardResource = _includeBeard ? ResolveSelected(beardSlot) : null;
            Mesh beardMesh = beardResource != null && !hideBeard ? beardResource.Mesh : null;

            if (_includeBeard && hideBeard)
                Debug.LogWarning("Resource đang lấp Head có Disable Beard = true, Beard sẽ không hiển thị.");

            List<Mesh> combineMeshes = new List<Mesh>();
            foreach (BodypartView view in atlasViews)
                combineMeshes.Add(view.Resource.Mesh);
            if (beardMesh != null)
                combineMeshes.Add(beardMesh);

            Mesh generatedMesh = CharacterAppearanceBuilder.CombineMesh(combineMeshes);
            Texture2D generatedAtlas = CharacterAppearanceBuilder.BakeAtlas(
                atlasViews, _config.AtlasSize, _config.AtlasFormat, _haircutTint);

            SaveGeneratedAssets(generatedMesh, generatedAtlas);

            if (_bodyTargetRenderer != null)
                ApplyToSkinnedRenderer(_bodyTargetRenderer, generatedMesh, generatedAtlas);

            ApplyBackpackPreview();
        }

        private void ApplyBackpackPreview()
        {
            if (!_includeBackpack || _backpackTargetRenderer == null)
                return;

            BodypartSlotEntry backpackSlot = FindSlot(BodypartSlotKind.Backpack);
            BodypartResource resource = backpackSlot != null ? ResolveSelected(backpackSlot) : null;
            if (resource == null)
                return;

            ApplyToSkinnedRenderer(_backpackTargetRenderer, resource.Mesh, resource.Texture);
        }

        private static void AddIfPresent(List<BodypartView> list, BodypartSlotEntry slot, BodypartResource resource)
        {
            if (slot == null || resource == null)
                return;

            list.Add(new BodypartView(resource, slot));
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

        private static void SaveGeneratedAssets(Mesh mesh, Texture2D atlas)
        {
            string folder = "Assets/GeneratedAppearanceTest";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "GeneratedAppearanceTest");

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

        private static void ApplyToSkinnedRenderer(SkinnedMeshRenderer renderer, Mesh mesh, Texture2D texture)
        {
            Undo.RecordObject(renderer, "Apply Generated Appearance");

            renderer.sharedMesh = mesh;
            renderer.localBounds = mesh.bounds;

            Material sourceMaterial = renderer.sharedMaterial;
            if (sourceMaterial != null)
            {
                Material previewMaterial = new Material(sourceMaterial) { mainTexture = texture };
                renderer.sharedMaterial = previewMaterial;
            }

            EditorUtility.SetDirty(renderer);
        }
    }
}