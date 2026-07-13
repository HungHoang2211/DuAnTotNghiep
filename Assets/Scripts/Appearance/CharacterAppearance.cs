using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Characters.Appearance
{
    public sealed class CharacterAppearance : MonoBehaviour
    {
        [SerializeField] private CharacterAppearanceConfig config;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private Material baseMaterial;
        [SerializeField] private Material atlasBlitMaterial;
        [SerializeField] private SkinnedMeshRenderer headRenderer;
        [SerializeField] private SkinnedMeshRenderer torsoRenderer;
        [SerializeField] private SkinnedMeshRenderer legsRenderer;
        [SerializeField] private SkinnedMeshRenderer feetRenderer;
        [SerializeField] private SkinnedMeshRenderer backpackRenderer;
        [SerializeField] private SkinnedMeshRenderer beardRenderer;
        [SerializeField] private int haircutColorIndex;

        private Material _instanceMaterial;
        private Texture2D _generatedAtlas;

        private void OnEnable()
        {
            EnsureMaterialInstance();
            playerEquipment.System.OnSlotChanged += HandleSlotChanged;
            Rebuild();
        }

        private void OnDisable()
        {
            playerEquipment.System.OnSlotChanged -= HandleSlotChanged;
        }

        private void OnDestroy()
        {
            DestroyGeneratedAtlas();
        }

        private void HandleSlotChanged(EquipSlot slot, int slotIndex, ItemStack stack)
        {
            if (!IsAppearanceSlot(slot))
                return;

            Rebuild();
        }

        private void EnsureMaterialInstance()
        {
            if (_instanceMaterial != null)
                return;

            _instanceMaterial = new Material(baseMaterial);
            AssignSharedMaterial(headRenderer);
            AssignSharedMaterial(torsoRenderer);
            AssignSharedMaterial(legsRenderer);
            AssignSharedMaterial(feetRenderer);
            AssignSharedMaterial(backpackRenderer);
            AssignSharedMaterial(beardRenderer);
        }

        private void AssignSharedMaterial(SkinnedMeshRenderer renderer)
        {
            if (renderer != null)
                renderer.sharedMaterial = _instanceMaterial;
        }

        private void Rebuild()
        {
            BodypartSlotEntry headSlot = FindSlotEntry(BodypartSlotKind.Head);
            BodypartSlotEntry torsoSlot = FindSlotEntry(BodypartSlotKind.Torso);
            BodypartSlotEntry legsSlot = FindSlotEntry(BodypartSlotKind.Legs);
            BodypartSlotEntry feetSlot = FindSlotEntry(BodypartSlotKind.Feet);
            BodypartSlotEntry backpackSlot = FindSlotEntry(BodypartSlotKind.Backpack);
            BodypartSlotEntry haircutSlot = FindSlotEntry(BodypartSlotKind.Haircut);
            BodypartSlotEntry beardSlot = FindSlotEntry(BodypartSlotKind.Beard);

            BodypartResource torsoResource = torsoSlot != null ? ResolveEquipmentResource(torsoSlot) : null;
            BodypartResource legsResource = legsSlot != null ? ResolveEquipmentResource(legsSlot) : null;
            BodypartResource feetResource = feetSlot != null ? ResolveEquipmentResource(feetSlot) : null;

            BodypartResource helmetResource = ResolveHelmetResource();
            BodypartResource haircutResource = haircutSlot?.DefaultResource;
            BodypartResource headResource = helmetResource != null ? helmetResource : haircutResource;

            BodypartResource backpackResource = backpackSlot != null ? ResolveBackpackResource(backpackSlot) : null;

            bool hideBeard = headResource != null && headResource.DisableBeard;
            BodypartResource beardResource = beardSlot?.DefaultResource;
            bool beardVisible = beardResource != null && !hideBeard;

            List<BodypartView> atlasViews = new List<BodypartView>();
            AddView(atlasViews, headSlot, headResource);
            AddView(atlasViews, torsoSlot, torsoResource);
            AddView(atlasViews, legsSlot, legsResource);
            AddView(atlasViews, feetSlot, feetResource);
            AddView(atlasViews, backpackSlot, backpackResource);

            if (atlasViews.Count > 0)
            {
                Color haircutTint = ResolveHaircutColor();
                Texture2D newAtlas = CharacterAppearanceBuilder.BakeAtlas(atlasViews, config.AtlasSize, config.AtlasFormat, haircutTint, atlasBlitMaterial);

                DestroyGeneratedAtlas();
                _generatedAtlas = newAtlas;
                _instanceMaterial.mainTexture = _generatedAtlas;
            }

            ApplyRenderer(headRenderer, headResource);
            ApplyRenderer(torsoRenderer, torsoResource);
            ApplyRenderer(legsRenderer, legsResource);
            ApplyRenderer(feetRenderer, feetResource);
            ApplyRenderer(backpackRenderer, backpackResource);
            ApplyRenderer(beardRenderer, beardVisible ? beardResource : null);
        }

        private static void AddView(List<BodypartView> views, BodypartSlotEntry slot, BodypartResource resource)
        {
            if (slot == null || resource == null)
                return;

            views.Add(new BodypartView(resource, slot));
        }

        private static void ApplyRenderer(SkinnedMeshRenderer renderer, BodypartResource resource)
        {
            if (renderer == null)
                return;

            bool visible = resource != null;
            renderer.gameObject.SetActive(visible);
            if (!visible)
                return;

            renderer.sharedMesh = resource.Mesh;
            renderer.updateWhenOffscreen = true;
        }

        private BodypartResource ResolveEquipmentResource(BodypartSlotEntry slot)
        {
            EquipSlot equipSlot = ToEquipSlot(slot.Kind);
            ItemStack stack = playerEquipment.System.GetSlot(equipSlot);
            EquipmentAbility equipment = stack?.ItemData.GetAbility<EquipmentAbility>();
            BodypartResource resource = equipment != null ? equipment.AppearanceResource : null;
            return resource != null ? resource : slot.DefaultResource;
        }

        private BodypartResource ResolveHelmetResource()
        {
            ItemStack stack = playerEquipment.System.GetSlot(EquipSlot.Helmet);
            EquipmentAbility equipment = stack?.ItemData.GetAbility<EquipmentAbility>();
            return equipment != null ? equipment.AppearanceResource : null;
        }

        private BodypartResource ResolveBackpackResource(BodypartSlotEntry slot)
        {
            ItemStack stack = playerEquipment.System.GetSlot(EquipSlot.Backpack);
            ContainerAbility container = stack?.ItemData.GetAbility<ContainerAbility>();
            return container != null ? container.BackpackResource : null;
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

        private void DestroyGeneratedAtlas()
        {
            if (_generatedAtlas == null)
                return;

            Destroy(_generatedAtlas);
            _generatedAtlas = null;
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