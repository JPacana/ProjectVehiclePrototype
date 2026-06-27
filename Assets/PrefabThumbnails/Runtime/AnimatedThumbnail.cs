using System;
using UnityEngine;

namespace PrefabThumbnails
{
    /// <summary>
    /// Result of an animated thumbnail render: a frame strip plus the
    /// caller's preferred playback rate. UI consumers flip
    /// <see cref="GetFrameAt"/> on a scheduled tick to drive the animation.
    ///
    /// Static renders also produce one of these (<see cref="FrameCount"/>=1)
    /// so consumers can treat both cases uniformly without branching on
    /// <see cref="ThumbnailAnimationMode"/>.
    /// </summary>
    public class AnimatedThumbnail : IDisposable
    {
        public readonly Texture2D[] Frames;
        public readonly float Fps;
        public readonly ThumbnailAnimationMode Mode;
        public int FrameCount => Frames?.Length ?? 0;

        bool _disposed;

        public AnimatedThumbnail(Texture2D[] frames, float fps, ThumbnailAnimationMode mode)
        {
            Frames = frames;
            Fps    = fps > 0f ? fps : 12f;
            Mode   = mode;
        }

        /// <summary>First frame — handy when a consumer wants a still preview.</summary>
        public Texture2D FirstFrame => (Frames != null && Frames.Length > 0) ? Frames[0] : null;

        /// <summary>
        /// Pick a frame given elapsed playback time in seconds. Loops
        /// modulo the strip length. O(1) — pure math, no allocation.
        /// </summary>
        public Texture2D GetFrameAt(float playbackSeconds)
        {
            if (Frames == null || Frames.Length == 0) return null;
            if (Frames.Length == 1) return Frames[0];
            float framesElapsed = playbackSeconds * Fps;
            int idx = ((int)Mathf.Floor(framesElapsed)) % Frames.Length;
            if (idx < 0) idx += Frames.Length;
            return Frames[idx];
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (Frames == null) return;
            for (int i = 0; i < Frames.Length; i++)
                if (Frames[i] != null) UnityEngine.Object.Destroy(Frames[i]);
        }
    }
}
