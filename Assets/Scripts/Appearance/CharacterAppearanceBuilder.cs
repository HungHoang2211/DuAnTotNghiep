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
        private static Texture2D _cachedDefaultMask;

        public static Texture2D BakeAtlas(
            IReadOnlyList<BodypartView> views,
            int atlasSize,
            TextureFormat format,
            Color haircutTint,
            Material blitMaterial)
        {
            Texture2D defaultMask = GetOrCreateDefaultMask();

            RenderTexture rt = RenderTexture.GetTemporary(atlasSize, atlasSize, 0, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = rt;

            GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, atlasSize, 0, atlasSize);

            foreach (BodypartView view in views)
                DrawView(view, blitMaterial, defaultMask, haircutTint);

            GL.PopMatrix();

            Texture2D atlas = new Texture2D(atlasSize, atlasSize, format, false)
            {
                name = "CharacterAppearanceAtlas"
            };
            atlas.ReadPixels(new Rect(0, 0, atlasSize, atlasSize), 0, 0);
            atlas.Apply();

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);

            return atlas;
        }

        private static void DrawView(BodypartView view, Material blitMaterial, Texture2D defaultMask, Color haircutTint)
        {
            RectInt rect = view.SlotEntry.AtlasRect;
            Rect pixelRect = new Rect(rect.x, rect.y, rect.width, rect.height);

            Texture2D maskTexture = view.Resource.RegionMask != null ? view.Resource.RegionMask : defaultMask;
            Texture2D detailTexture = view.Resource.DetailTexture != null ? view.Resource.DetailTexture : defaultMask;

            blitMaterial.SetTexture("_MaskTex", maskTexture);
            blitMaterial.SetTexture("_DetailTex", detailTexture);
            blitMaterial.SetVector("_DetailTiling", new Vector4(
                view.Resource.DetailTiling.x, view.Resource.DetailTiling.y,
                view.Resource.DetailOffset.x, view.Resource.DetailOffset.y));
            blitMaterial.SetColor("_TintColor", haircutTint);

            Graphics.DrawTexture(pixelRect, view.Resource.Texture, blitMaterial);
        }

        private static Texture2D GetOrCreateDefaultMask()
        {
            if (_cachedDefaultMask != null)
                return _cachedDefaultMask;

            _cachedDefaultMask = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "CharacterAppearanceDefaultMask"
            };
            _cachedDefaultMask.SetPixel(0, 0, Color.black);
            _cachedDefaultMask.Apply();

            return _cachedDefaultMask;
        }
    }
}