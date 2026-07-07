using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Characters.Appearance
{
    public sealed class CharacterAppearance : MonoBehaviour
    {
        [SerializeField] private CharacterAppearanceConfig config;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private SkinnedMeshRenderer bodyRenderer;
        [SerializeField] private SkinnedMeshRenderer backpackRenderer;
        [SerializeField] private int haircutColorIndex;

        private Mesh _generatedMesh;
        private Texture2D _generatedAtlas;
        private readonly HashSet<Texture2D> _runtimeTextures = new HashSet<Texture2D>();
        private Texture2D _activeBackpackTexture;

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
            DestroyIfOwned(_activeBackpackTexture);
        }

        private void HandleSlotChanged(EquipSlot slot, int slotIndex, ItemStack stack)
        {
            if (!IsAppearanceSlot(slot))
                return;

            Rebuild();
        }

        private void Rebuild()
        {
            BodypartSlotEntry torsoSlot = FindSlotEntry(BodypartSlotKind.Torso);
            BodypartSlotEntry legsSlot = FindSlotEntry(BodypartSlotKind.Legs);
            BodypartSlotEntry feetSlot = FindSlotEntry(BodypartSlotKind.Feet);
            BodypartSlotEntry headSlot = FindSlotEntry(BodypartSlotKind.Head);
            BodypartSlotEntry haircutSlot = FindSlotEntry(BodypartSlotKind.Haircut);
            BodypartSlotEntry beardSlot = FindSlotEntry(BodypartSlotKind.Beard);
            BodypartSlotEntry backpackSlot = FindSlotEntry(BodypartSlotKind.Backpack);

            List<BodypartView> atlasViews = new List<BodypartView>();

            AddIfPresent(atlasViews, torsoSlot, ResolveEquipmentResource(torsoSlot));
            AddIfPresent(atlasViews, legsSlot, ResolveEquipmentResource(legsSlot));
            AddIfPresent(atlasViews, feetSlot, ResolveEquipmentResource(feetSlot));

            BodypartResource helmetResource = headSlot != null ? ResolveEquipmentResource(headSlot) : null;
            BodypartResource haircutResource = haircutSlot?.DefaultResource;
            BodypartResource headFillerResource = helmetResource != null ? helmetResource : haircutResource;

            AddIfPresent(atlasViews, headSlot, headFillerResource);

            bool hideBeard = headFillerResource != null && headFillerResource.DisableBeard;
            BodypartResource beardResource = beardSlot?.DefaultResource;
            Mesh beardMesh = beardResource != null && !hideBeard ? beardResource.Mesh : null;

            Color haircutTint = ResolveHaircutColor();

            ApplyBodyMeshAndAtlas(atlasViews, beardMesh, haircutTint);
            ApplyBackpack(backpackSlot, haircutTint);
        }

        private static void AddIfPresent(List<BodypartView> list, BodypartSlotEntry slot, BodypartResource resource)
        {
            if (slot == null || resource == null)
                return;

            list.Add(new BodypartView(resource, slot));
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

        private void ApplyBodyMeshAndAtlas(List<BodypartView> atlasViews, Mesh beardMesh, Color haircutTint)
        {
            if (atlasViews.Count == 0)
                return;

            List<Mesh> combineMeshes = new List<Mesh>();
            foreach (BodypartView view in atlasViews)
                combineMeshes.Add(view.Resource.Mesh);

            if (beardMesh != null)
                combineMeshes.Add(beardMesh);

            Mesh newMesh = CharacterAppearanceBuilder.CombineMesh(combineMeshes);
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
            if (backpackRenderer == null)
                return;

            BodypartResource resource = slot != null ? ResolveBackpackResource(slot) : null;

            if (resource == null)
            {
                backpackRenderer.gameObject.SetActive(false);
                DestroyIfOwned(_activeBackpackTexture);
                _activeBackpackTexture = null;
                return;
            }

            Texture2D texture = CharacterAppearanceBuilder.BuildStandaloneTexture(
                resource.Texture, resource.RegionMask, resource.DetailTexture, resource.DetailTiling, resource.DetailOffset, haircutTint);

            if (texture != _activeBackpackTexture)
            {
                DestroyIfOwned(_activeBackpackTexture);
                if (texture != resource.Texture)
                    _runtimeTextures.Add(texture);
                _activeBackpackTexture = texture;
            }

            backpackRenderer.gameObject.SetActive(true);
            backpackRenderer.sharedMesh = resource.Mesh;
            backpackRenderer.localBounds = resource.Mesh.bounds;
            backpackRenderer.material.mainTexture = texture;
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