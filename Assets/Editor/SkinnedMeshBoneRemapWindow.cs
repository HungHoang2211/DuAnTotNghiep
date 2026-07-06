using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance.Editor
{
    public sealed class SkinnedMeshBoneRemapWindow : EditorWindow
    {
        private SkinnedMeshRenderer _sourceMesh;
        private Transform _targetSkeletonRoot;
        private SkinnedMeshRenderer _targetRenderer;

        [MenuItem("Simple Survival/Character/Remap Skinned Mesh Bones")]
        private static void ShowWindow()
        {
            GetWindow<SkinnedMeshBoneRemapWindow>("Remap Bones");
        }

        private void OnGUI()
        {
            _sourceMesh = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Source Mesh (bản gốc FBX)", _sourceMesh, typeof(SkinnedMeshRenderer), true);
            _targetSkeletonRoot = (Transform)EditorGUILayout.ObjectField(
                "Target Skeleton Root (mainRig)", _targetSkeletonRoot, typeof(Transform), true);
            _targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Target Renderer (Body)", _targetRenderer, typeof(SkinnedMeshRenderer), true);

            GUI.enabled = _sourceMesh != null && _targetSkeletonRoot != null && _targetRenderer != null;
            if (GUILayout.Button("Remap Bones By Name"))
                RemapBones();
            GUI.enabled = true;
        }

        private void RemapBones()
        {
            Transform[] sourceBones = _sourceMesh.bones;
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
                Debug.LogWarning("Không tìm thấy " + missingCount + " bone theo tên trong target skeleton.");
                return;
            }

            Transform rootBoneMatch = FindChildByName(_targetSkeletonRoot, _sourceMesh.rootBone.name);

            Undo.RecordObject(_targetRenderer, "Remap Skinned Mesh Bones");
            _targetRenderer.bones = remappedBones;
            _targetRenderer.rootBone = rootBoneMatch != null ? rootBoneMatch : _targetSkeletonRoot;
            EditorUtility.SetDirty(_targetRenderer);
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