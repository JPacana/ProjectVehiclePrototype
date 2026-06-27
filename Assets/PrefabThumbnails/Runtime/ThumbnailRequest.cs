using System;
using UnityEngine;

namespace PrefabThumbnails
{
    /// <summary>
    /// How a thumbnail is rendered: a single still frame, or an animated
    /// sequence the consumer can flip through at a target FPS.
    /// </summary>
    public enum ThumbnailAnimationMode
    {
        /// <summary>One frame, cached forever. Cheapest and most common.</summary>
        Static = 0,

        /// <summary>
        /// N frames captured while the prefab is rotated around its Y axis
        /// by <c>360 / FrameCount</c> between captures. The result loops
        /// seamlessly because the last frame meets frame 0 at 360°.
        /// </summary>
        YawRotation = 1,

        /// <summary>
        /// N frames captured while an <see cref="UnityEngine.AnimationClip"/>
        /// is sampled across its duration. Use <see cref="ThumbnailRequest.AnimationClip"/>
        /// to pick the clip; <see cref="ThumbnailRequest.AnimatorSampleTargetPath"/>
        /// to point at the rig root (e.g. Kenney AC2's <c>Root</c> child).
        /// </summary>
        AnimatorClip = 2,
    }

    /// <summary>
    /// One thumbnail render. Either <see cref="Prefab"/> or
    /// <see cref="ResourcePath"/> must be set; if both are set,
    /// <see cref="Prefab"/> wins.
    ///
    /// For animated thumbnails, set <see cref="AnimationMode"/> +
    /// <see cref="FrameCount"/>. The renderer captures one frame per
    /// <c>Tick</c>, so an 8-frame yaw rotation populates over 8 frames.
    /// </summary>
    public struct ThumbnailRequest
    {
        /// <summary>Caller-defined identity. Cache key, in-flight dedup key, and <c>OnThumbnailReady</c> argument. Typically your item ID or a hash of (model, skin, attachment).</summary>
        public int Key;

        /// <summary>Direct prefab reference. Use this for prefabs that aren't under a <c>Resources</c> folder.</summary>
        public GameObject Prefab;

        /// <summary>
        /// <see cref="Resources.Load{T}"/> path — an alternative to <see cref="Prefab"/>
        /// for assets you keep under <c>Resources/</c>. Lets you queue a thumbnail
        /// without holding a hard reference to the prefab (useful for catalogs
        /// where you only know the path until the thumbnail is needed).
        /// </summary>
        public string ResourcePath;

        /// <summary>
        /// Euler rotation applied to the instantiated prefab in degrees.
        /// <see cref="Vector3.zero"/> means "use the renderer's
        /// <c>DefaultPrefabRotation</c>". Set this to override per-request,
        /// e.g. show a hero card facing the camera and a side-profile card
        /// rotated 90°.
        /// </summary>
        public Vector3 PrefabRotation;

        /// <summary>
        /// Runs immediately after the prefab is instantiated, before camera
        /// framing. Use this to:
        /// <list type="bullet">
        /// <item>Apply skin materials (cosmetic systems).</item>
        /// <item>Pose an Animator (force the model into a specific anim frame).</item>
        /// <item>Attach accessories (hats, weapons, badges).</item>
        /// <item>Toggle child renderers (LOD picking, empty-slot indicators).</item>
        /// </list>
        /// Runs BEFORE the bounds calculation so anything you add is included in the framing.
        /// For animated thumbnails, runs ONCE before frame 0; subsequent frames re-use the same instance.
        /// </summary>
        public Action<GameObject> PreRenderCallback;

        /// <summary>
        /// Runs immediately after async GPU readback for EACH frame, before
        /// the texture is finalized and cached. Use this to:
        /// <list type="bullet">
        /// <item>Desaturate "locked" thumbnails.</item>
        /// <item>Apply a tint or vignette.</item>
        /// <item>Watermark the lower-right corner.</item>
        /// <item>Mask out a logo region.</item>
        /// </list>
        /// The Texture2D you receive is fully populated and CPU-readable. Modify in place.
        /// </summary>
        public Action<Texture2D> PostProcessCallback;

        // ─── Animation ───────────────────────────────────────────────────

        /// <summary>Animation mode. Default <see cref="ThumbnailAnimationMode.Static"/> — one frame, cached forever.</summary>
        public ThumbnailAnimationMode AnimationMode;

        /// <summary>
        /// Number of frames to capture in animated modes. Ignored when
        /// <see cref="AnimationMode"/> is <c>Static</c>. Defaults to 24 if
        /// unset (the renderer clamps below 2 → 2). Higher counts = smoother
        /// playback but linearly more capture work and memory.
        /// </summary>
        public int FrameCount;

        /// <summary>
        /// Animated playback rate hint, in frames-per-second. Pass-through
        /// metadata — the consumer's flipbook reads this to choose its tick
        /// interval. Defaults to 12 fps if unset. Does NOT affect capture
        /// cost (capture is still one render per <c>Tick</c>).
        /// </summary>
        public float PlaybackFps;

        /// <summary>
        /// Animator-mode source clip. Required when <see cref="AnimationMode"/>
        /// is <see cref="ThumbnailAnimationMode.AnimatorClip"/>. The clip is
        /// sampled at <c>i / FrameCount * clip.length</c> for each frame i.
        /// </summary>
        public AnimationClip AnimationClip;

        /// <summary>
        /// Animator-mode sample-target path under the prefab root. Kenney
        /// AC2 clips, for example, are authored relative to the <c>Root</c>
        /// child — sampling against the prefab root silently no-ops every
        /// curve binding. Leave empty to sample against the prefab root.
        /// </summary>
        public string AnimatorSampleTargetPath;
    }
}
