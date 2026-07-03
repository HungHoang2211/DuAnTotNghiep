using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance
{
    public readonly struct BodypartView
    {
        public readonly BodypartResource Resource;
        public readonly BodypartSlotEntry SlotEntry;

        public BodypartView(BodypartResource resource, BodypartSlotEntry slotEntry)
        {
            Resource = resource;
            SlotEntry = slotEntry;
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

        public static Texture2D BakeAtlas(IReadOnlyList<BodypartView> views, int atlasSize, TextureFormat format, Color haircutTint)
        {
            Texture2D atlas = new Texture2D(atlasSize, atlasSize, format, mipChain: false)
            {
                name = "CharacterAppearanceAtlas"
            };

            Color32[] clearPixels = new Color32[atlasSize * atlasSize];
            atlas.SetPixels32(clearPixels);

            foreach (BodypartView view in views)
            {
                RectInt rect = view.SlotEntry.AtlasRect;
                Color[] regionPixels = ExtractRegionPixels(
                    view.Resource.Texture,
                    view.Resource.RegionMask,
                    view.Resource.DetailTexture,
                    view.Resource.DetailTiling,
                    view.Resource.DetailOffset,
                    haircutTint,
                    rect.width,
                    rect.height);

                atlas.SetPixels(rect.x, rect.y, rect.width, rect.height, regionPixels);
            }

            atlas.Apply();
            return atlas;
        }

        public static Texture2D BuildStandaloneTexture(
            Texture2D baseTexture,
            Texture2D regionMask,
            Texture2D detailTexture,
            Vector2 detailTiling,
            Vector2 detailOffset,
            Color haircutTint)
        {
            if (regionMask == null)
                return baseTexture;

            int width = baseTexture.width;
            int height = baseTexture.height;
            Color[] pixels = ExtractRegionPixels(
                baseTexture, regionMask, detailTexture, detailTiling, detailOffset, haircutTint, width, height);

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "GeneratedTintedTexture"
            };
            result.SetPixels(pixels);
            result.Apply();

            return result;
        }

        private static Color[] ExtractRegionPixels(
            Texture2D baseTexture,
            Texture2D regionMask,
            Texture2D detailTexture,
            Vector2 detailTiling,
            Vector2 detailOffset,
            Color haircutTint,
            int targetWidth,
            int targetHeight)
        {
            Color[] pixels = new Color[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                float v = (y + 0.5f) / targetHeight;
                for (int x = 0; x < targetWidth; x++)
                {
                    float u = (x + 0.5f) / targetWidth;
                    pixels[y * targetWidth + x] = SamplePixel(
                        baseTexture, regionMask, detailTexture, detailTiling, detailOffset, haircutTint, u, v);
                }
            }

            return pixels;
        }

        private static Color SamplePixel(
            Texture2D baseTexture,
            Texture2D regionMask,
            Texture2D detailTexture,
            Vector2 detailTiling,
            Vector2 detailOffset,
            Color haircutTint,
            float u,
            float v)
        {
            Color baseColor = baseTexture.GetPixelBilinear(u, v);
            if (regionMask == null)
                return baseColor;

            Color mask = regionMask.GetPixelBilinear(u, v);
            Color tintedColor = baseColor * haircutTint;
            Color result = Color.Lerp(baseColor, tintedColor, mask.g);

            if (detailTexture != null && mask.b > 0f)
            {
                float du = Wrap(u * detailTiling.x + detailOffset.x);
                float dv = Wrap(v * detailTiling.y + detailOffset.y);
                Color detailColor = detailTexture.GetPixelBilinear(du, dv);
                result = Color.Lerp(result, detailColor, mask.b);
            }

            return result;
        }

        private static float Wrap(float value)
        {
            return value - Mathf.Floor(value);
        }
    }
}