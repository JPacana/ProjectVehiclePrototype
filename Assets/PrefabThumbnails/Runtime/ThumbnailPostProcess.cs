using UnityEngine;

namespace PrefabThumbnails
{
    /// <summary>
    /// Bundled post-process callbacks for <see cref="ThumbnailRequest.PostProcessCallback"/>.
    /// Drop these in directly when you need the common effects:
    ///
    /// <code>
    /// renderer.Queue(new ThumbnailRequest {
    ///     Key = item.Id,
    ///     Prefab = item.Prefab,
    ///     PostProcessCallback = ThumbnailPostProcess.Greyscale,
    /// });
    /// </code>
    ///
    /// All of these mutate the texture in place — the renderer's caching
    /// stage doesn't see the original color version. To toggle between
    /// "locked" (greyscale) and "owned" (full color) you have to evict +
    /// re-queue when the ownership state changes.
    ///
    /// For animated thumbnails the callback fires on EVERY captured frame,
    /// so all frames of the strip get the same treatment.
    /// </summary>
    public static class ThumbnailPostProcess
    {
        /// <summary>
        /// Convert to greyscale using ITU-R BT.601 luminance weights
        /// (0.299 R + 0.587 G + 0.114 B). The classic "locked / not owned"
        /// look — desaturated, a touch dim. Preserves alpha for cards with
        /// transparent backgrounds.
        /// </summary>
        public static void Greyscale(Texture2D tex)
        {
            var pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                byte lum = (byte)(p.r * 0.299f + p.g * 0.587f + p.b * 0.114f);
                pixels[i] = new Color32(lum, lum, lum, p.a);
            }
            tex.SetPixels32(pixels);
        }

        /// <summary>
        /// Multiply every pixel by a tint colour. Combine with
        /// <see cref="Greyscale"/> for a "tinted greyscale" look (e.g.
        /// blue-tinted greyscale for an under-development cosmetic). Tint
        /// is multiplicative — <c>Color.white</c> is no-op, <c>Color.black</c>
        /// is fully black.
        /// </summary>
        public static System.Action<Texture2D> Tint(Color tint)
        {
            return tex =>
            {
                var pixels = tex.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    pixels[i] = new Color32(
                        (byte)(p.r * tint.r),
                        (byte)(p.g * tint.g),
                        (byte)(p.b * tint.b),
                        (byte)(p.a * tint.a));
                }
                tex.SetPixels32(pixels);
            };
        }

        /// <summary>
        /// Greyscale + dim. Equivalent to Greyscale followed by a 0.7
        /// uniform tint — the LoL cosmetic "locked" look. Saves you from
        /// chaining two callbacks for the most common case.
        /// </summary>
        public static void GreyscaleDim(Texture2D tex)
        {
            var pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                byte lum = (byte)((p.r * 0.299f + p.g * 0.587f + p.b * 0.114f) * 0.7f);
                pixels[i] = new Color32(lum, lum, lum, p.a);
            }
            tex.SetPixels32(pixels);
        }
    }
}
