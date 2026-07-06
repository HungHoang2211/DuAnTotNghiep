using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Characters.Appearance
{
    [Serializable]
    public sealed class RigidAttachmentPoint
    {
        [SerializeField] private BodypartSlotKind kind;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        public BodypartSlotKind Kind => kind;
        public MeshFilter MeshFilter => meshFilter;
        public MeshRenderer MeshRenderer => meshRenderer;
    }

    public sealed class CharacterAppearance : MonoBehaviour
    {
        [SerializeField] private CharacterAppearanceConfig config;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private SkinnedMeshRenderer bodyRenderer;
        [SerializeField] private List<RigidAttachmentPoint> rigidAttachmentPoints = new List<RigidAttachmentPoint>();
        [SerializeField] private int haircutColorIndex;

        private Mesh _generatedMesh;
        private Texture2D _generatedAtlas;
        private readonly HashSet<Texture2D> _runtimeTextures = new HashSet<Texture2D>();
        private readonly Dictionary<BodypartSlotKind, Texture2D> _activeRigidTextures = new Dictionary<BodypartSlotKind, Texture2D>();

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
            DestroyAllRigidTextures();
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
            bool hideHaircut = false;
            bool hideBeard = false;

            foreach (BodypartSlotEntry slot in config.Slots)
            {
                if (!IsAtlasComposite(slot.Kind))
                    continue;

                BodypartResource resource = ResolveEquipmentResource(slot);
                if (resource == null)
                    continue;

                atlasViews.Add(new BodypartView(resource, slot));
                hideHaircut |= resource.DisableHaircut;
                hideBeard |= resource.DisableBeard;
            }

            Debug.Log("CharacterAppearance Rebuild: atlasViews.Count = " + atlasViews.Count);

            Color haircutTint = ResolveHaircutColor();

            ApplyBodyMeshAndAtlas(atlasViews, haircutTint);
            ApplyBackpack(FindSlotEntry(BodypartSlotKind.Backpack), haircutTint);
            ApplyHaircut(FindSlotEntry(BodypartSlotKind.Haircut), hideHaircut, haircutTint);
            ApplyBeard(FindSlotEntry(BodypartSlotKind.Beard), hideBeard);
        }

        private BodypartResource ResolveEquipmentResource(BodypartSlotEntry slot)
        {
            EquipSlot equipSlot = ToEquipSlot(slot.Kind);
            ItemStack stack = playerEquipment.System.GetSlot(equipSlot);
            EquipmentAbility equipment = stack?.ItemData.GetAbility<EquipmentAbility>();
            BodypartResource resource = equipment != null ? equipment.AppearanceResource : null;
            return resource != null ? resource : slot.DefaultResource;
        }

        private BodypartResource ResolveBackpackResource(BodypartSlotEntry slot)
        {
            ItemStack stack = playerEquipment.System.GetSlot(EquipSlot.Backpack);
            ContainerAbility container = stack?.ItemData.GetAbility<ContainerAbility>();
            return container != null ? container.BackpackResource : slot.DefaultResource;
        }

        private void ApplyBodyMeshAndAtlas(List<BodypartView> atlasViews, Color haircutTint)
        {
            if (atlasViews.Count == 0)
                return;

            Mesh newMesh = CharacterAppearanceBuilder.CombineMesh(atlasViews);
            Texture2D newAtlas = CharacterAppearanceBuilder.BakeAtlas(atlasViews, config.AtlasSize, config.AtlasFormat, haircutTint);

            DestroyGeneratedMesh();
            DestroyGeneratedAtlas();

            _generatedMesh = newMesh;
            _generatedAtlas = newAtlas;

            bodyRenderer.sharedMesh = _generatedMesh;
            bodyRenderer.localBounds = _generatedMesh.bounds;
            bodyRenderer.material.mainTexture = _generatedAtlas;
        }

        private void ApplyBackpack(BodypartSlotEntry slot, Color haircutTint)
        {
            BodypartResource resource = slot != null ? ResolveBackpackResource(slot) : null;
            SetRigidAttachment(BodypartSlotKind.Backpack, resource != null ? RigidVisual.FromResource(resource) : (RigidVisual?)null, haircutTint);
        }

        private void ApplyHaircut(BodypartSlotEntry slot, bool hidden, Color haircutTint)
        {
            BodypartResource resource = slot?.DefaultResource;
            bool visible = resource != null && !hidden;
            SetRigidAttachment(BodypartSlotKind.Haircut, visible ? RigidVisual.FromResource(resource) : (RigidVisual?)null, haircutTint);
        }

        private void ApplyBeard(BodypartSlotEntry slot, bool hidden)
        {
            RigidAttachmentPoint point = FindAttachmentPoint(BodypartSlotKind.Beard);
            if (point == null)
                return;

            BodypartResource resource = slot?.DefaultResource;
            bool visible = resource != null && !hidden && _generatedAtlas != null;

            point.MeshRenderer.gameObject.SetActive(visible);

            if (!visible)
                return;

            point.MeshFilter.sharedMesh = resource.Mesh;
            point.MeshRenderer.material.mainTexture = _generatedAtlas;
        }

        private void SetRigidAttachment(BodypartSlotKind kind, RigidVisual? visual, Color haircutTint)
        {
            if (!visual.HasValue)
            {
                SetAttachmentPointActive(kind, false);
                ClearRigidTexture(kind);
                return;
            }

            RigidAttachmentPoint point = FindAttachmentPoint(kind);
            if (point == null)
                return;

            RigidVisual value = visual.Value;
            Texture2D texture = CharacterAppearanceBuilder.BuildStandaloneTexture(
                value.BaseTexture, value.RegionMask, value.DetailTexture, value.DetailTiling, value.DetailOffset, haircutTint);

            TrackRigidTexture(kind, texture, value.BaseTexture);

            point.MeshRenderer.gameObject.SetActive(true);
            point.MeshFilter.sharedMesh = value.Mesh;
            point.MeshRenderer.material.mainTexture = texture;
        }

        private void SetAttachmentPointActive(BodypartSlotKind kind, bool active)
        {
            RigidAttachmentPoint point = FindAttachmentPoint(kind);
            point?.MeshRenderer.gameObject.SetActive(active);
        }

        private void TrackRigidTexture(BodypartSlotKind kind, Texture2D texture, Texture2D sourceTexture)
        {
            ClearRigidTexture(kind);

            if (texture != sourceTexture)
                _runtimeTextures.Add(texture);

            _activeRigidTextures[kind] = texture;
        }

        private void ClearRigidTexture(BodypartSlotKind kind)
        {
            if (!_activeRigidTextures.TryGetValue(kind, out Texture2D existing))
                return;

            DestroyIfOwned(existing);
            _activeRigidTextures.Remove(kind);
        }

        private Color ResolveHaircutColor()
        {
            IReadOnlyList<Color> palette = config.HaircutPalette;
            if (palette.Count == 0)
                return Color.white;

            int index = Mathf.Clamp(haircutColorIndex, 0, palette.Count - 1);
            return palette[index];
        }

        private BodypartSlotEntry FindSlotEntry(BodypartSlotKind kind)
        {
            foreach (BodypartSlotEntry slot in config.Slots)
            {
                if (slot.Kind == kind)
                    return slot;
            }
            return null;
        }

        private RigidAttachmentPoint FindAttachmentPoint(BodypartSlotKind kind)
        {
            foreach (RigidAttachmentPoint point in rigidAttachmentPoints)
            {
                if (point.Kind == kind)
                    return point;
            }
            return null;
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

        private void DestroyAllRigidTextures()
        {
            foreach (BodypartSlotKind kind in new List<BodypartSlotKind>(_activeRigidTextures.Keys))
                ClearRigidTexture(kind);
        }

        private static EquipSlot ToEquipSlot(BodypartSlotKind kind)
        {
            switch (kind)
            {
                case BodypartSlotKind.Head: return EquipSlot.Helmet;
                case BodypartSlotKind.Torso: return EquipSlot.Jacket;
                case BodypartSlotKind.Legs: return EquipSlot.Pants;
                case BodypartSlotKind.Feet: return EquipSlot.Boots;
                default: return EquipSlot.None;
            }
        }

        private static bool IsAtlasComposite(BodypartSlotKind kind)
        {
            return kind == BodypartSlotKind.Head
                || kind == BodypartSlotKind.Torso
                || kind == BodypartSlotKind.Legs
                || kind == BodypartSlotKind.Feet;
        }

        private static bool IsAppearanceSlot(EquipSlot slot)
        {
            return slot == EquipSlot.Helmet
                || slot == EquipSlot.Jacket
                || slot == EquipSlot.Pants
                || slot == EquipSlot.Boots
                || slot == EquipSlot.Backpack;
        }

        private readonly struct RigidVisual
        {
            public readonly Mesh Mesh;
            public readonly Texture2D BaseTexture;
            public readonly Texture2D RegionMask;
            public readonly Texture2D DetailTexture;
            public readonly Vector2 DetailTiling;
            public readonly Vector2 DetailOffset;

            private RigidVisual(Mesh mesh, Texture2D baseTexture, Texture2D regionMask, Texture2D detailTexture, Vector2 detailTiling, Vector2 detailOffset)
            {
                Mesh = mesh;
                BaseTexture = baseTexture;
                RegionMask = regionMask;
                DetailTexture = detailTexture;
                DetailTiling = detailTiling;
                DetailOffset = detailOffset;
            }

            public static RigidVisual FromResource(BodypartResource resource)
            {
                return new RigidVisual(resource.Mesh, resource.Texture, resource.RegionMask, resource.DetailTexture, resource.DetailTiling, resource.DetailOffset);
            }
        }
    }
}