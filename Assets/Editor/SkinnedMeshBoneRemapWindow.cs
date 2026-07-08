using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance.Editor
{
    public sealed class SkinnedMeshBoneRemapWindow : EditorWindow
    {
        [System.Serializable]
        private sealed class RemapEntry
        {
            public SkinnedMeshRenderer sourceMesh;
            public SkinnedMeshRenderer targetRenderer;
        }

        private Transform _targetSkeletonRoot;
        private readonly List<RemapEntry> _entries = new List<RemapEntry>();
        private Vector2 _scrollPosition;

        [MenuItem("Simple Survival/Character/Remap Skinned Mesh Bones")]
        private static void ShowWindow()
        {
            GetWindow<SkinnedMeshBoneRemapWindow>("Remap Bones");
        }

        private void OnEnable()
        {
            if (_entries.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                    _entries.Add(new RemapEntry());
            }
        }

        private void OnGUI()
        {
            _targetSkeletonRoot = (Transform)EditorGUILayout.ObjectField(
                "Target Skeleton Root (mainRig)", _targetSkeletonRoot, typeof(Transform), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Danh sách remap:", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _entries.Count; i++)
                DrawEntry(i);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Thêm dòng"))
                    _entries.Add(new RemapEntry());

                GUI.enabled = _entries.Count > 1;
                if (GUILayout.Button("- Xoá dòng cuối"))
                    _entries.RemoveAt(_entries.Count - 1);
                GUI.enabled = true;
            }

            EditorGUILayout.Space();
            GUI.enabled = _targetSkeletonRoot != null && HasAnyValidEntry();
            if (GUILayout.Button("Remap All"))
                RemapAll();
            GUI.enabled = true;
        }

        private void DrawEntry(int index)
        {
            RemapEntry entry = _entries[index];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Entry " + index, EditorStyles.boldLabel);
            entry.sourceMesh = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Source Mesh (FBX gốc)", entry.sourceMesh, typeof(SkinnedMeshRenderer), true);
            entry.targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Target Renderer", entry.targetRenderer, typeof(SkinnedMeshRenderer), true);
            EditorGUILayout.EndVertical();
        }

        private bool HasAnyValidEntry()
        {
            foreach (RemapEntry entry in _entries)
            {
                if (entry.sourceMesh != null && entry.targetRenderer != null)
                    return true;
            }
            return false;
        }

        private void RemapAll()
        {
            int successCount = 0;
            int failCount = 0;

            foreach (RemapEntry entry in _entries)
            {
                if (entry.sourceMesh == null || entry.targetRenderer == null)
                    continue;

                if (RemapOne(entry.sourceMesh, entry.targetRenderer))
                    successCount++;
                else
                    failCount++;
            }

            Debug.Log("Remap hoàn tất: " + successCount + " thành công, " + failCount + " thất bại.");
        }

        private bool RemapOne(SkinnedMeshRenderer sourceMesh, SkinnedMeshRenderer targetRenderer)
        {
            Transform[] sourceBones = sourceMesh.bones;
            Transform[] remappedBones = new Transform[sourceBones.Length];
            int missingCount = 0;

            for (int i = 0; i < sourceBones.Length; i++)
            {
                string boneName = sourceBones[i].name;
                Transform match = FindChildByName(_targetSkeletonRoot, boneName);
                remappedBones[i] = match;

                if (match == null)
                    missingCount++;
            }

            if (missingCount > 0)
            {
                Debug.LogWarning("[" + targetRenderer.name + "] Không tìm thấy " + missingCount + " bone theo tên trong target skeleton.");
                return false;
            }

            Transform rootBoneMatch = FindChildByName(_targetSkeletonRoot, sourceMesh.rootBone.name);

            Undo.RecordObject(targetRenderer, "Remap Skinned Mesh Bones");
            targetRenderer.bones = remappedBones;
            targetRenderer.rootBone = rootBoneMatch != null ? rootBoneMatch : _targetSkeletonRoot;
            EditorUtility.SetDirty(targetRenderer);

            return true;
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildByName(root.GetChild(i), name);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}