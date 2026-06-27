using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

namespace PrefabThumbnails
{
    /// <summary>
    /// Offscreen prefab → <see cref="Texture2D"/> pipeline. Builds one
    /// hidden camera + light + RenderTexture, queues prefab render
    /// requests, processes one frame per <see cref="Tick"/> call, and
    /// caches finished thumbnails by your caller-defined key.
    ///
    /// Three render modes:
    /// <list type="bullet">
    /// <item><b>Static</b> — one frame, cached forever. The classic
    /// cosmetic-card use case.</item>
    /// <item><b>YawRotation</b> — N frames captured while spinning the
    /// model around its Y axis. Result loops seamlessly (frame N meets
    /// frame 0 at 360°). For "alive-looking" roster cards.</item>
    /// <item><b>AnimatorClip</b> — N frames sampled across an
    /// AnimationClip. For idle/run/jump previews that feel like the live
    /// in-game character.</item>
    /// </list>
    ///
    /// Designed for cosmetic shops, character rosters, item catalogs,
    /// vehicle galleries — any UI that needs N thumbnails of M prefabs and
    /// can tolerate a few frames of "still rendering" before the image lands.
    ///
    /// One-render-per-Tick is intentional: rendering N prefabs in one frame
    /// would spike GPU + camera setup cost into a single visible hitch. Spreading
    /// the work across N frames is invisible to the user (the UI just
    /// gradually fills in) and absorbs into idle GPU time.
    /// </summary>
    public class PrefabThumbnailRenderer : IDisposable
    {
        // ─── Configurable at construction ────────────────────────────────
        /// <summary>Square pixel size of every thumbnail. 256 is a solid default for UI grids; bump to 512 for hero card slots.</summary>
        public readonly int ThumbnailSize;

        /// <summary>Offscreen origin in world space — must be far enough from your scene that nothing else renders into the camera's frustum. Default (2000, 2000, 0) is safe unless your map is enormous.</summary>
        public Vector3 OffscreenOrigin = new Vector3(2000f, 2000f, 0f);

        /// <summary>Camera FOV in degrees. 25 reads as a "long lens" — flatters character proportions. 60 is closer to "natural eye". Tighter FOV = more lens compression = more flattering for character cards.</summary>
        public float CameraFOV = 25f;

        /// <summary>Padding multiplier applied when auto-framing the camera to the prefab's bounds. 1.5 = 50% extra room around the model. Drop to 1.2 for tight crops, raise to 1.8 for "full body with breathing room".</summary>
        public float FramingPadding = 1.5f;

        /// <summary>Camera background color. Default is fully transparent — use a solid color if you want a baked card background.</summary>
        public Color BackgroundColor = new Color(0f, 0f, 0f, 0f);

        /// <summary>Default rotation applied to instantiated prefabs (Euler degrees). The default (0, 160, 0) shows the back-three-quarter view typical of character roster cards. Override per-request via <see cref="ThumbnailRequest.PrefabRotation"/>.</summary>
        public Vector3 DefaultPrefabRotation = new Vector3(0f, 160f, 0f);

        /// <summary>Light direction (Euler degrees). The default (50, -30, 0) is the classic three-quarter key-light angle from upper-right.</summary>
        public Vector3 LightRotation = new Vector3(50f, -30f, 0f);

        /// <summary>Light intensity. Default 1.0 reads as evenly-lit; bump to 1.4 for punchier rim shadows.</summary>
        public float LightIntensity = 1.0f;

        // ─── Scene objects ───────────────────────────────────────────────
        GameObject sceneRoot;
        Camera renderCamera;
        RenderTexture renderTexture;

        // ─── Caches & queue ──────────────────────────────────────────────
        readonly Dictionary<int, AnimatedThumbnail> cache = new();
        readonly List<ThumbnailRequest> pending = new();
        readonly HashSet<int> inFlight = new();
        readonly HashSet<int> cancelled = new();

        // ─── In-progress render state ────────────────────────────────────
        bool readbackInProgress;
        ThumbnailRequest activeRequest;
        GameObject activeInstance;
        Transform activeSampleTarget;
        Texture2D[] activeFrames;
        int activeFrameIndex;
        int activeFrameTotal;
        Vector3 activeBaseRotation;
        bool disposed;

        /// <summary>Optional callback fired whenever a thumbnail FINISHES (last frame ready). Use it to refresh UI elements; most UIs poll <see cref="GetThumbnail"/> instead. For animated thumbnails, fires once after the last frame.</summary>
        public event Action<int, AnimatedThumbnail> OnThumbnailReady;

        public PrefabThumbnailRenderer(int thumbnailSize = 256)
        {
            ThumbnailSize = thumbnailSize;
        }

        /// <summary>
        /// Build the offscreen camera + light + RenderTexture. Call once
        /// after construction and before the first Queue / Tick. Idempotent.
        /// </summary>
        public void Setup()
        {
            if (sceneRoot != null) return;

            // Pick a readback-friendly format. ARGB32 maps to B8G8R8A8_SRGB
            // on iOS Metal, which doesn't support synchronous ReadPixels on
            // some device classes. GetCompatibleFormat asks the runtime.
            var colorFmt = SystemInfo.GetCompatibleFormat(
                GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormatUsage.ReadPixels);
            renderTexture = new RenderTexture(ThumbnailSize, ThumbnailSize, 16, colorFmt);
            renderTexture.Create();

            sceneRoot = new GameObject("[PrefabThumbnailRenderer]");
            sceneRoot.transform.position = OffscreenOrigin;
            UnityEngine.Object.DontDestroyOnLoad(sceneRoot);

            var camGO = new GameObject("ThumbCam");
            camGO.transform.SetParent(sceneRoot.transform, false);
            renderCamera = camGO.AddComponent<Camera>();
            renderCamera.targetTexture = renderTexture;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = BackgroundColor;
            renderCamera.nearClipPlane = 0.01f;
            renderCamera.farClipPlane = 500f;
            renderCamera.fieldOfView = CameraFOV;
            renderCamera.depth = -20;       // Render BEFORE main scene cameras
            renderCamera.enabled = false;   // We call Render() manually
            camGO.transform.localPosition = new Vector3(0, 1, -5);
            camGO.transform.LookAt(OffscreenOrigin + Vector3.up);

#if UNITY_RENDER_PIPELINE_UNIVERSAL
            // URP enables post-FX per-camera; offscreen thumbnail cameras
            // should not run the main camera's bloom / tonemap stack.
            var urp = renderCamera.GetUniversalAdditionalCameraData();
            if (urp != null) urp.renderPostProcessing = false;
#endif

            var lightGO = new GameObject("ThumbLight");
            lightGO.transform.SetParent(sceneRoot.transform, false);
            lightGO.transform.rotation = Quaternion.Euler(LightRotation);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = LightIntensity;
        }

        /// <summary>
        /// Queue a thumbnail render. Safe to call multiple times for the
        /// same key — duplicates are silently dropped while the first
        /// request is pending or its result is cached. To force a re-render,
        /// call <see cref="Evict"/> first.
        /// </summary>
        public void Queue(ThumbnailRequest req)
        {
            if (disposed) return;
            if (inFlight.Contains(req.Key)) return;
            if (cache.ContainsKey(req.Key)) return;

            inFlight.Add(req.Key);
            cancelled.Remove(req.Key);
            pending.Add(req);
        }

        /// <summary>
        /// Cancel a pending or in-flight request. Pending: removed from the
        /// queue. In-flight: the current frame finishes (can't abort an
        /// async readback) but no further frames are captured and any
        /// partial frames are discarded. Useful for scrolled-off-screen
        /// lazy-loaded tiles.
        /// </summary>
        public void Cancel(int key)
        {
            if (disposed) return;
            for (int i = pending.Count - 1; i >= 0; i--)
                if (pending[i].Key == key) pending.RemoveAt(i);
            inFlight.Remove(key);
            cancelled.Add(key);
        }

        /// <summary>
        /// Drive the queue. Call from your hub MonoBehaviour's <c>Update</c>.
        /// At most one FRAME is captured per call — multiple frames =
        /// multiple Ticks. For an 8-frame yaw rotation, the thumbnail
        /// completes after 8 Ticks (≈130 ms at 60 fps).
        /// </summary>
        public void Tick()
        {
            if (disposed || sceneRoot == null || readbackInProgress) return;
            if (activeInstance != null)
            {
                AdvanceActiveFrame();
                return;
            }
            if (pending.Count == 0) return;

            // LIFO ordering — last-queued renders first. This matches what
            // UI typically wants: the user just scrolled to a section, you
            // want THAT section's tiles to fill in before you finish the
            // off-screen ones from earlier.
            int last = pending.Count - 1;
            var req = pending[last];
            pending.RemoveAt(last);

            if (cache.ContainsKey(req.Key) || cancelled.Contains(req.Key))
            {
                cancelled.Remove(req.Key);
                inFlight.Remove(req.Key);
                return;
            }

            BeginRender(req);
        }

        /// <summary>Lookup a finished thumbnail's first frame. Returns null if not yet rendered (queue still working) or never queued.</summary>
        public Texture2D GetThumbnail(int key) => cache.TryGetValue(key, out var t) ? t.FirstFrame : null;

        /// <summary>Lookup a finished animated thumbnail. Returns null if not yet rendered. Static thumbnails return a 1-frame strip — consumers can treat both modes uniformly.</summary>
        public AnimatedThumbnail GetAnimated(int key) => cache.TryGetValue(key, out var t) ? t : null;

        /// <summary>True if the cache has a finished thumbnail for this key.</summary>
        public bool IsCached(int key) => cache.ContainsKey(key);

        /// <summary>True if a thumbnail is queued but not yet finished. Includes the in-flight render's frames-in-progress.</summary>
        public bool IsPending(int key) => inFlight.Contains(key);

        /// <summary>
        /// Drop a single cached thumbnail. Use when the underlying prefab
        /// or its dependencies (skin material, attachments) change and you
        /// want the next <see cref="Queue"/> to re-render.
        /// </summary>
        public void Evict(int key)
        {
            if (cache.TryGetValue(key, out var anim))
            {
                anim.Dispose();
                cache.Remove(key);
            }
            inFlight.Remove(key);
            for (int i = pending.Count - 1; i >= 0; i--)
                if (pending[i].Key == key) pending.RemoveAt(i);
        }

        /// <summary>
        /// Bulk-evict thumbnails matching a predicate. Useful for invalidating
        /// "currently locked" thumbnails in one pass after the user buys a
        /// pack — the next <see cref="Queue"/> re-renders them in full color.
        /// </summary>
        public void EvictWhere(Predicate<int> shouldEvict)
        {
            List<int> toEvict = null;
            foreach (var key in cache.Keys)
                if (shouldEvict(key))
                {
                    toEvict ??= new List<int>(cache.Count);
                    toEvict.Add(key);
                }
            if (toEvict == null) return;
            foreach (var key in toEvict) Evict(key);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            foreach (var anim in cache.Values) anim.Dispose();
            cache.Clear();
            pending.Clear();
            inFlight.Clear();
            cancelled.Clear();

            DestroyActiveInstance();
            DiscardActiveFrames();

            if (renderTexture != null)
            {
                renderTexture.Release();
                UnityEngine.Object.Destroy(renderTexture);
                renderTexture = null;
            }
            if (sceneRoot != null)
            {
                UnityEngine.Object.Destroy(sceneRoot);
                sceneRoot = null;
            }
            renderCamera = null;
        }

        // ─── Internal ────────────────────────────────────────────────────

        void BeginRender(ThumbnailRequest req)
        {
            // Instantiate the source. Either Prefab or ResourcePath wins.
            GameObject prefab = req.Prefab;
            if (prefab == null && !string.IsNullOrEmpty(req.ResourcePath))
                prefab = Resources.Load<GameObject>(req.ResourcePath);
            if (prefab == null)
            {
                // Source missing — drop the request silently and let the
                // caller's GetThumbnail keep returning null.
                inFlight.Remove(req.Key);
                return;
            }

            activeInstance = UnityEngine.Object.Instantiate(prefab, sceneRoot.transform);
            Vector3 baseRot = (req.PrefabRotation == default) ? DefaultPrefabRotation : req.PrefabRotation;
            activeBaseRotation = baseRot;
            activeInstance.transform.localPosition = Vector3.zero;
            activeInstance.transform.localRotation = Quaternion.Euler(baseRot);
            activeInstance.SetActive(true);

            // Force every SkinnedMeshRenderer to re-bake its mesh every
            // frame regardless of the cached bounding box. Unity's
            // multi-camera culling decides which SMRs need re-baking based
            // on their PRE-DEFORM bounds; for a long-lens thumbnail camera
            // at OffscreenOrigin the SMR can be "culled" even when our
            // render frustum sees it — the captured frame then shows the
            // bind pose regardless of how the bones were sampled. Set on
            // every render (cheap; idempotent) so YawRotation / AnimatorClip
            // both benefit, and a re-used SMR clip doesn't keep a stale
            // value from the prefab asset's serialized state.
            foreach (var smr in activeInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                smr.updateWhenOffscreen = true;

            // Animator-mode: locate the sample target up front. Empty path
            // = sample against the prefab root (cheapest).
            activeSampleTarget = activeInstance.transform;
            if (req.AnimationMode == ThumbnailAnimationMode.AnimatorClip)
            {
                if (!string.IsNullOrEmpty(req.AnimatorSampleTargetPath))
                {
                    var found = activeInstance.transform.Find(req.AnimatorSampleTargetPath);
                    if (found != null) activeSampleTarget = found;
                }
                PrepareAnimatorForSampling(activeInstance, activeSampleTarget);
            }

            // Caller hook: skin, pose, attach. Runs ONCE before frame 0;
            // the same instance is re-used across animated sub-frames so
            // skin material assignments persist.
            req.PreRenderCallback?.Invoke(activeInstance);

            activeRequest = req;
            activeFrameTotal = ResolveFrameCount(req);
            activeFrames = new Texture2D[activeFrameTotal];
            activeFrameIndex = 0;

            CaptureCurrentFrame();
        }

        void AdvanceActiveFrame()
        {
            // Caller cancelled mid-sequence — abandon the in-flight render
            // and clean up. The frames already captured are destroyed.
            if (cancelled.Contains(activeRequest.Key))
            {
                cancelled.Remove(activeRequest.Key);
                inFlight.Remove(activeRequest.Key);
                DestroyActiveInstance();
                DiscardActiveFrames();
                return;
            }

            activeFrameIndex++;
            if (activeFrameIndex >= activeFrameTotal)
            {
                FinalizeActive();
                return;
            }
            CaptureCurrentFrame();
        }

        void CaptureCurrentFrame()
        {
            ApplyFrameState(activeRequest, activeFrameIndex, activeFrameTotal);

            // Frame the camera ONCE at frame 0 using a yaw-invariant
            // bounding radius. Re-framing per sub-frame would visibly pulse
            // the model's size as its bounds rotate under the camera — a
            // wobble in the otherwise-clean YawRotation loop.
            if (activeFrameIndex == 0)
                FrameCameraYawSafe(renderCamera, activeInstance, FramingPadding);

            renderCamera.Render();
            StartAsyncReadback();
        }

        void ApplyFrameState(ThumbnailRequest req, int frameIdx, int frameTotal)
        {
            switch (req.AnimationMode)
            {
                case ThumbnailAnimationMode.YawRotation:
                {
                    // Frame i sits at +(i / N) * 360° from the base rotation.
                    // Frame N would meet frame 0, so the strip loops cleanly.
                    float yawStep = frameTotal > 0 ? 360f / frameTotal : 0f;
                    Vector3 rot = activeBaseRotation + new Vector3(0f, yawStep * frameIdx, 0f);
                    activeInstance.transform.localRotation = Quaternion.Euler(rot);
                    break;
                }
                case ThumbnailAnimationMode.AnimatorClip:
                {
                    var clip = req.AnimationClip;
                    if (clip == null || activeSampleTarget == null) break;
                    float t = frameTotal > 1
                        ? (frameIdx / (float)frameTotal) * clip.length
                        : 0f;
                    clip.SampleAnimation(activeSampleTarget.gameObject, t);
                    break;
                }
                case ThumbnailAnimationMode.Static:
                default:
                    // Static is a single frame at the base rotation —
                    // nothing to mutate per-frame.
                    break;
            }
        }

        void StartAsyncReadback()
        {
            readbackInProgress = true;
            AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGBA32, OnReadbackComplete);
        }

        void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            readbackInProgress = false;

            // Renderer was Disposed during the in-flight GPU readback —
            // every reference below this point is dead. Bail.
            if (disposed) return;

            if (request.hasError)
            {
                // Skip this frame but advance the sequence so we don't
                // wedge on a recurring driver failure.
                return;
            }

            var data = request.GetData<byte>();
            var tex = new Texture2D(ThumbnailSize, ThumbnailSize, TextureFormat.RGBA32, false);
            tex.LoadRawTextureData(data);

            // Optional caller hook — desaturate "locked" thumbnails, apply
            // a tint, watermark, whatever fits your UX. Runs per-frame so
            // animated thumbnails get the same treatment on every frame.
            activeRequest.PostProcessCallback?.Invoke(tex);

            tex.Apply(false, true);  // markNoLongerReadable = true → frees CPU copy

            if (activeFrames != null && activeFrameIndex < activeFrames.Length)
                activeFrames[activeFrameIndex] = tex;
            else
                UnityEngine.Object.Destroy(tex);
        }

        void FinalizeActive()
        {
            int key = activeRequest.Key;
            float fps = activeRequest.PlaybackFps > 0f ? activeRequest.PlaybackFps : 12f;
            var anim = new AnimatedThumbnail(activeFrames, fps, activeRequest.AnimationMode);

            cache[key] = anim;
            inFlight.Remove(key);

            // Null the field BEFORE destroying so a re-entrant
            // OnThumbnailReady callback can't see a stale instance.
            DestroyActiveInstance();
            activeFrames = null;
            activeFrameIndex = 0;
            activeFrameTotal = 0;
            activeRequest = default;

            OnThumbnailReady?.Invoke(key, anim);
        }

        void DestroyActiveInstance()
        {
            if (activeInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(activeInstance);
                activeInstance = null;
            }
            activeSampleTarget = null;
        }

        void DiscardActiveFrames()
        {
            if (activeFrames == null) return;
            for (int i = 0; i < activeFrames.Length; i++)
                if (activeFrames[i] != null) UnityEngine.Object.Destroy(activeFrames[i]);
            activeFrames = null;
        }

        /// <summary>
        /// Wire the instance for <c>AnimationClip.SampleAnimation</c>:
        /// <list type="bullet">
        /// <item>The sample target must carry an <see cref="Animator"/> — Generic
        /// and Humanoid clips silently no-op when sampled against an
        /// Animator-less GameObject. (Legacy clips work without one, but
        /// Kenney AC1 / AC2 ships Generic.) The controller is intentionally
        /// null and root motion is off: SampleAnimation drives the pose
        /// directly, the controller would race it.</item>
        /// <item>Every OTHER Animator in the subtree is destroyed. Most FBX
        /// imports add one on the prefab root; left in place it would tick
        /// its own state machine each frame and stomp the SampleAnimation
        /// output between our render and the readback.</item>
        /// <item><see cref="AnimatorCullingMode.AlwaysAnimate"/> on the
        /// kept Animator — the prefab is at <see cref="OffscreenOrigin"/>
        /// far from the main camera, so the default culling mode disables
        /// animation as soon as we hit Play and the pose freezes at the
        /// bind pose.</item>
        /// </list>
        /// This is the same setup the sibling <c>unity-3d-to-sprite-baker</c>
        /// uses for its live-mesh demo cards — vetted across iOS / Android /
        /// WebGL / desktop.
        /// </summary>
        static void PrepareAnimatorForSampling(GameObject instance, Transform sampleTarget)
        {
            if (instance == null || sampleTarget == null) return;

            foreach (var existing in instance.GetComponentsInChildren<Animator>(true))
            {
                if (existing == null) continue;
                if (existing.gameObject == sampleTarget.gameObject)
                {
                    existing.runtimeAnimatorController = null;
                    existing.applyRootMotion = false;
                    existing.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                }
            }
            if (sampleTarget.gameObject.GetComponent<Animator>() == null)
            {
                var a = sampleTarget.gameObject.AddComponent<Animator>();
                a.runtimeAnimatorController = null;
                a.applyRootMotion = false;
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        static int ResolveFrameCount(ThumbnailRequest req)
        {
            if (req.AnimationMode == ThumbnailAnimationMode.Static) return 1;
            // Sensible default: 24 frames is one rotation per ~2 s at 12 fps,
            // matches the demo's "alive but not frantic" feel.
            int n = req.FrameCount > 0 ? req.FrameCount : 24;
            return Mathf.Max(2, n);
        }

        static void FrameCameraYawSafe(Camera cam, GameObject model, float padding)
        {
            if (model == null || cam == null) return;

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Use the largest XZ diagonal in place of bounds.size.x/z so the
            // framing survives Y-axis rotation — when the model spins 90°
            // its XZ extents swap, and a per-frame .x distance would pulse.
            // Y is unaffected by yaw, so we keep its raw extent.
            float xz = Mathf.Sqrt(bounds.size.x * bounds.size.x + bounds.size.z * bounds.size.z);
            float maxDim = Mathf.Max(xz, bounds.size.y);
            float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
            float distance = (maxDim / 2f) / Mathf.Tan(fovRad / 2f) * padding;

            Vector3 center = bounds.center;
            cam.transform.position = center + new Vector3(0, 0, -distance);
            cam.transform.LookAt(center);
        }
    }
}
