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
        private SkinnedMeshRenderer _haircutTargetRenderer;
        private SkinnedMeshRenderer _beardTargetRenderer;
        private Color _haircutTint = Color.white;
        private readonly Dictionary<BodypartSlotKind, int> _selectedIndices = new Dictionary<BodypartSlotKind, int>();

        private bool _includeBackpack;
        private bool _includeHaircut;
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

            _bodyTargetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Body Target Renderer", _bodyTargetRenderer, typeof(SkinnedMeshRenderer), true);

            _haircutTint = EditorGUILayout.ColorField("Haircut Tint", _haircutTint);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Slot bắt buộc (luôn bake vào atlas):", EditorStyles.boldLabel);
            DrawFixedSlot(BodypartSlotKind.Head);
            DrawFixedSlot(BodypartSlotKind.Torso);
            DrawFixedSlot(BodypartSlotKind.Legs);
            DrawFixedSlot(BodypartSlotKind.Feet);

            EditorGUILayout.Space();
            _includeBackpack = EditorGUILayout.ToggleLeft("Bao gồm Backpack (bake vào atlas, renderer riêng)", _includeBackpack);
            using (new EditorGUI.DisabledScope(!_includeBackpack))
            {
                DrawFixedSlot(BodypartSlotKind.Backpack);
                _backpackTargetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                    "Backpack Target Renderer", _backpackTargetRenderer, typeof(SkinnedMeshRenderer), true);
            }

            EditorGUILayout.Space();
            _includeHaircut = EditorGUILayout.ToggleLeft("Bao gồm Haircut (mượn atlas, renderer riêng)", _includeHaircut);
            using (new EditorGUI.DisabledScope(!_includeHaircut))
            {
                DrawFixedSlot(BodypartSlotKind.Haircut);
                _haircutTargetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                    "Haircut Target Renderer", _haircutTargetRenderer, typeof(SkinnedMeshRenderer), true);
            }

            EditorGUILayout.Space();
            _includeBeard = EditorGUILayout.ToggleLeft("Bao gồm Beard (mượn atlas, renderer riêng)", _includeBeard);
            using (new EditorGUI.DisabledScope(!_includeBeard))
            {
                DrawFixedSlot(BodypartSlotKind.Beard);
                _beardTargetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                    "Beard Target Renderer", _beardTargetRenderer, typeof(SkinnedMeshRenderer), true);
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
            BodypartSlotEntry headSlot = FindSlot(BodypartSlotKind.Head);
            BodypartSlotEntry torsoSlot = FindSlot(BodypartSlotKind.Torso);
            BodypartSlotEntry legsSlot = FindSlot(BodypartSlotKind.Legs);
            BodypartSlotEntry feetSlot = FindSlot(BodypartSlotKind.Feet);
            BodypartSlotEntry backpackSlot = FindSlot(BodypartSlotKind.Backpack);

            List<BodypartView> atlasViews = new List<BodypartView>();
            List<Mesh> combineMeshes = new List<Mesh>();

            AddView(atlasViews, combineMeshes, headSlot, ResolveSelected(headSlot), combineIntoBody: true);
            AddView(atlasViews, combineMeshes, torsoSlot, ResolveSelected(torsoSlot), combineIntoBody: true);
            AddView(atlasViews, combineMeshes, legsSlot, ResolveSelected(legsSlot), combineIntoBody: true);
            AddView(atlasViews, combineMeshes, feetSlot, ResolveSelected(feetSlot), combineIntoBody: true);

            BodypartResource backpackResource = _includeBackpack ? ResolveSelected(backpackSlot) : null;
            AddView(atlasViews, combineMeshes, backpackSlot, backpackResource, combineIntoBody: false);

            if (combineMeshes.Count == 0)
            {
                Debug.LogWarning("Không có bodypart nào hợp lệ để combine mesh Body.");
                return;
            }

            Mesh generatedMesh = CharacterAppearanceBuilder.CombineMesh(combineMeshes);
            Texture2D generatedAtlas = CharacterAppearanceBuilder.BakeAtlas(
                atlasViews, _config.AtlasSize, _config.AtlasFormat, _haircutTint);

            SaveGeneratedAssets(generatedMesh, generatedAtlas);

            if (_bodyTargetRenderer != null)
                ApplyToSkinnedRenderer(_bodyTargetRenderer, generatedMesh, generatedAtlas);

            ApplyBackpackPreview(backpackResource, generatedAtlas);
            ApplyHaircutPreview(generatedAtlas);
            ApplyBeardPreview(generatedAtlas);
        }

        private static void AddView(List<BodypartView> views, List<Mesh> combineMeshes, BodypartSlotEntry slot, BodypartResource resource, bool combineIntoBody)
        {
            if (slot == null || resource == null)
                return;

            views.Add(new BodypartView(resource, slot));
            if (combineIntoBody)
                combineMeshes.Add(resource.Mesh);
        }

        private void ApplyBackpackPreview(BodypartResource resource, Texture2D atlas)
        {
            if (!_includeBackpack || resource == null || _backpackTargetRenderer == null)
                return;

            ApplyToSkinnedRenderer(_backpackTargetRenderer, resource.Mesh, atlas);
        }

        private void ApplyHaircutPreview(Texture2D atlas)
        {
            if (!_includeHaircut || _haircutTargetRenderer == null)
                return;

            BodypartSlotEntry haircutSlot = FindSlot(BodypartSlotKind.Haircut);
            BodypartResource resource = ResolveSelected(haircutSlot);
            if (resource == null)
                return;

            ApplyToSkinnedRenderer(_haircutTargetRenderer, resource.Mesh, atlas);
        }

        private void ApplyBeardPreview(Texture2D atlas)
        {
            if (!_includeBeard || _beardTargetRenderer == null)
                return;

            BodypartSlotEntry beardSlot = FindSlot(BodypartSlotKind.Beard);
            BodypartResource resource = ResolveSelected(beardSlot);
            if (resource == null)
                return;

            ApplyToSkinnedRenderer(_beardTargetRenderer, resource.Mesh, atlas);
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