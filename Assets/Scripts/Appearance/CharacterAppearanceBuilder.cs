using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance
{
    public readonly struct BodypartView
    {
        public readonly BodypartResource Resource;
        public readonly BodypartSlotConfig SlotConfig;

        public BodypartView(BodypartResource resource, BodypartSlotConfig slotConfig)
        {
            Resource = resource;
            SlotConfig = slotConfig;
        }
    }

    public static class CharacterAppearanceBuilder
    {
        public static Mesh CombineMesh(IReadOnlyList<BodypartView> views)
        {
            CombineInstance[] combineInstances = new CombineInstance[views.Count];
            List<BoneWeight> boneWeights = new List<BoneWeight>();

            for (int i = 0; i < views.Count; i++)
            {
                Mesh partMesh = views[i].Resource.Mesh;
                combineInstances[i] = new CombineInstance { mesh = partMesh };
                boneWeights.AddRange(partMesh.boneWeights);
            }

            Mesh combinedMesh = new Mesh { name = "CharacterAppearanceMesh" };
            combinedMesh.CombineMeshes(combineInstances, mergeSubMeshes: true, useMatrices: false);
            combinedMesh.boneWeights = boneWeights.ToArray();
            combinedMesh.bindposes = views[0].Resource.Mesh.bindposes;
            combinedMesh.RecalculateBounds();

            return combinedMesh;
        }

        public static Texture2D BakeAtlas(IReadOnlyList<BodypartView> views, int atlasSize, TextureFormat format)
        {
            Texture2D atlas = new Texture2D(atlasSize, atlasSize, format, mipChain: false)
            {
                name = "CharacterAppearanceAtlas"
            };

            Color32[] clearPixels = new Color32[atlasSize * atlasSize];
            atlas.SetPixels32(clearPixels);

            foreach (BodypartView view in views)
            {
                RectInt rect = view.SlotConfig.AtlasRect;
                Color[] regionPixels = ExtractRegionPixels(view.Resource, rect.width, rect.height);
                atlas.SetPixels(rect.x, rect.y, rect.width, rect.height, regionPixels);
            }

            atlas.Apply();
            return atlas;
        }

        public static Texture2D BuildTintedTexture(BodypartResource resource)
        {
            if (resource.TintMask == null)
                return resource.Texture;

            int width = resource.Texture.width;
            int height = resource.Texture.height;
            Color[] pixels = ExtractRegionPixels(resource, width, height);

            Texture2D tinted = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = resource.BodypartId + "_Tinted"
            };
            tinted.SetPixels(pixels);
            tinted.Apply();

            return tinted;
        }

        private static Color[] ExtractRegionPixels(BodypartResource resource, int targetWidth, int targetHeight)
        {
            Texture2D source = resource.Texture;
            Texture2D mask = resource.TintMask;
            Color tintColor = resource.TintColor;
            Color[] pixels = new Color[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                float v = (y + 0.5f) / targetHeight;
                for (int x = 0; x < targetWidth; x++)
                {
                    float u = (x + 0.5f) / targetWidth;
                    pixels[y * targetWidth + x] = SamplePixel(source, mask, tintColor, u, v);
                }
            }

            return pixels;
        }

        private static Color SamplePixel(Texture2D source, Texture2D mask, Color tintColor, float u, float v)
        {
            Color sourceColor = source.GetPixelBilinear(u, v);
            if (mask == null)
                return sourceColor;

            float maskAmount = mask.GetPixelBilinear(u, v).r;
            Color tintedColor = sourceColor * tintColor;
            return Color.Lerp(sourceColor, tintedColor, maskAmount);
        }
    }
}