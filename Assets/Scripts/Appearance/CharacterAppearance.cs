using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Characters.Appearance
{
    [Serializable]
    public sealed class RigidAttachmentPoint
    {
        [SerializeField] private EquipSlot equipSlot;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        public EquipSlot EquipSlot => equipSlot;
        public MeshFilter MeshFilter => meshFilter;
        public MeshRenderer MeshRenderer => meshRenderer;
    }

    [Serializable]
    public sealed class CosmeticAttachmentPoint
    {
        [SerializeField] private string cosmeticName;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        public string CosmeticName => cosmeticName;
        public MeshFilter MeshFilter => meshFilter;
        public MeshRenderer MeshRenderer => meshRenderer;
    }

    public sealed class CharacterAppearance : MonoBehaviour
    {
        [SerializeField] private CharacterAppearanceConfig config;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private SkinnedMeshRenderer bodyRenderer;
        [SerializeField] private List<RigidAttachmentPoint> rigidAttachmentPoints = new List<RigidAttachmentPoint>();
        [SerializeField] private List<CosmeticAttachmentPoint> cosmeticAttachmentPoints = new List<CosmeticAttachmentPoint>();

        private Mesh _generatedMesh;
        private Texture2D _generatedAtlas;
        private readonly HashSet<Texture2D> _runtimeTextures = new HashSet<Texture2D>();
        private readonly Dictionary<string, Texture2D> _activeCosmeticTextures = new Dictionary<string, Texture2D>();

        private void OnEnable()
        {
            playerEquipment.System.OnSlotChanged += HandleSlotChanged;
            Rebuild();
        }

        private void OnDisable()
        {
            playerEquipment.System.OnSlotChanged -= HandleSlotChanged;
        }

        private void OnDestroy()
        {
            DestroyGeneratedMesh();
            DestroyGeneratedAtlas();
            DestroyAllCosmeticTextures();
        }

        private void HandleSlotChanged(EquipSlot slot, int slotIndex, ItemStack stack)
        {
            if (!IsAppearanceSlot(slot))
                return;

            Rebuild();
        }

        private void Rebuild()
        {
            List<BodypartView> atlasViews = new List<BodypartView>();
            HashSet<string> hiddenCosmetics = new HashSet<string>();

            foreach (BodypartSlotConfig slotConfig in config.BodySlots)
            {
                BodypartResource resource = ResolveResource(slotConfig);

                if (slotConfig.RenderMode == BodypartRenderMode.RigidAttachment)
                {
                    ApplyRigidAttachment(slotConfig.EquipSlot, resource);
                    continue;
                }

                if (resource == null)
                    continue;

                atlasViews.Add(new BodypartView(resource, slotConfig));
                hiddenCosmetics.UnionWith(resource.HiddenCosmetics);
            }

            ApplyBodyMeshAndAtlas(atlasViews);
            ApplyCosmetics(hiddenCosmetics);
        }

        private BodypartResource ResolveResource(BodypartSlotConfig slotConfig)
        {
            if (slotConfig.EquipSlot == EquipSlot.Backpack)
                return ResolveBackpackResource(slotConfig);

            return ResolveEquipmentResource(slotConfig);
        }

        private BodypartResource ResolveBackpackResource(BodypartSlotConfig slotConfig)
        {
            ItemStack stack = playerEquipment.System.GetSlot(EquipSlot.Backpack);
            ContainerAbility container = stack?.ItemData.GetAbility<ContainerAbility>();
            return container != null ? container.BackpackResource : slotConfig.DefaultResource;
        }

        private BodypartResource ResolveEquipmentResource(BodypartSlotConfig slotConfig)
        {
            ItemStack stack = playerEquipment.System.GetSlot(slotConfig.EquipSlot);
            EquipmentAbility equipment = stack?.ItemData.GetAbility<EquipmentAbility>();
            BodypartResource resource = equipment != null ? equipment.AppearanceResource : null;
            return resource != null ? resource : slotConfig.DefaultResource;
        }

        private void ApplyBodyMeshAndAtlas(List<BodypartView> atlasViews)
        {
            if (atlasViews.Count == 0)
                return;

            Mesh newMesh = CharacterAppearanceBuilder.CombineMesh(atlasViews);
            Texture2D newAtlas = CharacterAppearanceBuilder.BakeAtlas(atlasViews, config.AtlasSize, config.AtlasFormat);

            DestroyGeneratedMesh();
            DestroyGeneratedAtlas();

            _generatedMesh = newMesh;
            _generatedAtlas = newAtlas;

            bodyRenderer.sharedMesh = _generatedMesh;
            bodyRenderer.material.mainTexture = _generatedAtlas;
        }

        private void ApplyRigidAttachment(EquipSlot equipSlot, BodypartResource resource)
        {
            RigidAttachmentPoint point = FindRigidAttachmentPoint(equipSlot);
            if (point == null)
                return;

            bool hasResource = resource != null;
            point.MeshRenderer.gameObject.SetActive(hasResource);

            if (!hasResource)
                return;

            point.MeshFilter.sharedMesh = resource.Mesh;
            point.MeshRenderer.material.mainTexture = resource.Texture;
        }

        private void ApplyCosmetics(HashSet<string> hiddenCosmetics)
        {
            foreach (CosmeticMeshConfig cosmetic in config.Cosmetics)
            {
                bool visible = !hiddenCosmetics.Contains(cosmetic.CosmeticName);
                ApplyCosmetic(cosmetic, visible);
            }
        }

        private void ApplyCosmetic(CosmeticMeshConfig cosmetic, bool visible)
        {
            CosmeticAttachmentPoint point = FindCosmeticAttachmentPoint(cosmetic.CosmeticName);
            if (point == null)
                return;

            bool shouldShow = visible && cosmetic.DefaultOption != null;
            point.MeshRenderer.gameObject.SetActive(shouldShow);

            if (!shouldShow)
                return;

            Texture2D texture = BuildAndTrackCosmeticTexture(cosmetic.DefaultOption);
            SetGeneratedCosmeticTexture(cosmetic.CosmeticName, texture);

            point.MeshFilter.sharedMesh = cosmetic.DefaultOption.Mesh;
            point.MeshRenderer.material.mainTexture = texture;
        }

        private Texture2D BuildAndTrackCosmeticTexture(BodypartResource resource)
        {
            Texture2D texture = CharacterAppearanceBuilder.BuildTintedTexture(resource);
            if (texture != resource.Texture)
                _runtimeTextures.Add(texture);

            return texture;
        }

        private RigidAttachmentPoint FindRigidAttachmentPoint(EquipSlot equipSlot)
        {
            foreach (RigidAttachmentPoint point in rigidAttachmentPoints)
            {
                if (point.EquipSlot == equipSlot)
                    return point;
            }
            return null;
        }

        private CosmeticAttachmentPoint FindCosmeticAttachmentPoint(string cosmeticName)
        {
            foreach (CosmeticAttachmentPoint point in cosmeticAttachmentPoints)
            {
                if (point.CosmeticName == cosmeticName)
                    return point;
            }
            return null;
        }

        private void SetGeneratedCosmeticTexture(string cosmeticName, Texture2D texture)
        {
            if (_activeCosmeticTextures.TryGetValue(cosmeticName, out Texture2D existing))
                DestroyIfOwned(existing);

            _activeCosmeticTextures[cosmeticName] = texture;
        }

        private void DestroyIfOwned(Texture2D texture)
        {
            if (texture == null || !_runtimeTextures.Contains(texture))
                return;

            _runtimeTextures.Remove(texture);
            Destroy(texture);
        }

        private void DestroyGeneratedMesh()
        {
            if (_generatedMesh == null)
                return;

            Destroy(_generatedMesh);
            _generatedMesh = null;
        }

        private void DestroyGeneratedAtlas()
        {
            if (_generatedAtlas == null)
                return;

            Destroy(_generatedAtlas);
            _generatedAtlas = null;
        }

        private void DestroyAllCosmeticTextures()
        {
            foreach (Texture2D texture in _activeCosmeticTextures.Values)
                DestroyIfOwned(texture);

            _activeCosmeticTextures.Clear();
        }

        private static bool IsAppearanceSlot(EquipSlot slot)
        {
            return slot == EquipSlot.Helmet
                || slot == EquipSlot.Jacket
                || slot == EquipSlot.Pants
                || slot == EquipSlot.Boots
                || slot == EquipSlot.Backpack;
        }
    }
}