using UnityEngine;
using UnityEngine.UIElements;

namespace PrefabThumbnails
{
    /// <summary>How an animated tile decides to advance frames.</summary>
    public enum ThumbnailPlaybackTrigger
    {
        /// <summary>Animate continuously after the strip lands. The default.</summary> 
        Continuous = 0,

        /// <summary>
        /// Animate only while the pointer is over the tile. Tile rests on
        /// frame 0 otherwise. Great for catalog grids where N animated
        /// tiles would otherwise compete for attention.
        /// </summary>
        OnHover = 1,
    }

    /// <summary>
    /// Self-contained UI Toolkit element representing one thumbnail in a
    /// grid. Handles three responsibilities most consumers re-implement:
    ///
    /// <list type="number">
    /// <item><b>Lazy loading.</b> Queues its <see cref="ThumbnailRequest"/>
    /// with the owning renderer only after the element is laid out
    /// (i.e. visible in its parent's flow). Off-screen tiles in a
    /// ScrollView never trigger a render until they scroll into view.</item>
    /// <item><b>Spinner.</b> Shows a <c>ds-spinner</c> from the bundled UI
    /// design system while waiting. The spinner disappears the moment the
    /// first frame lands.</item>
    /// <item><b>Animated playback.</b> Flips through the captured frame
    /// strip at the request's <c>PlaybackFps</c>. Static thumbnails just
    /// land their one frame. Optional hover-trigger via
    /// <see cref="PlaybackTrigger"/> = <c>OnHover</c>.</item>
    /// </list>
    ///
    /// Drop into a UXML grid or instantiate from C#. The element is a
    /// VisualElement subclass — style it via USS like any other DS component.
    /// </summary>
    [UxmlElement]
    public partial class ThumbnailTile : VisualElement
    {
        public const string UssClassName = "thumbnail-tile";
        public const string ImageUssClassName = "thumbnail-tile__image";
        public const string SpinnerUssClassName = "thumbnail-tile__spinner";
        public const string ReadyUssClassName = "is-ready";
        public const string HoverActiveUssClassName = "is-hover-active";

        // DesignSystemRuntime spins any element carrying both classes.
        // Without `is-spinning` the spinner renders as a static ring.
        const string SPINNER_BASE_CLASS   = "ds-spinner";
        const string SPINNER_ACTIVE_CLASS = "is-spinning";

        readonly VisualElement _image;
        readonly VisualElement _spinner;

        PrefabThumbnailRenderer _renderer;
        ThumbnailRequest _request;
        bool _requestSet;
        bool _queued;
        bool _attached;
        bool _pointerOver;
        bool _disposed;
        bool _subscribed;

        // Playback state — only used when the thumbnail is animated.
        AnimatedThumbnail _anim;
        double _playbackStart;
        int _lastFrameShown = -1;
        IVisualElementScheduledItem _flipbookTick;

        ThumbnailPlaybackTrigger _trigger = ThumbnailPlaybackTrigger.Continuous;

        /// <summary>The tile's image element — hook for click handlers or layered overlays.</summary>
        public VisualElement Image => _image;

        /// <summary>
        /// What drives the flipbook for animated tiles. Defaults to
        /// <see cref="ThumbnailPlaybackTrigger.Continuous"/>; set to
        /// <see cref="ThumbnailPlaybackTrigger.OnHover"/> for a catalog
        /// where only the hovered tile should animate.
        /// </summary>
        public ThumbnailPlaybackTrigger PlaybackTrigger
        {
            get => _trigger;
            set
            {
                if (_trigger == value) return;
                _trigger = value;
                // Re-evaluate playback against the new trigger immediately —
                // a switch from Continuous → OnHover should freeze tiles
                // that aren't currently hovered.
                ReevaluatePlayback();
            }
        }

        public ThumbnailTile()
        {
            AddToClassList(UssClassName);

            // Two children: the image surface (background-image driven) and
            // a spinner that fades when the first frame lands. Both share
            // the tile's bounds via position:absolute in USS.
            _image = new VisualElement { name = "thumbnail-tile-image" };
            _image.AddToClassList(ImageUssClassName);
            _image.pickingMode = PickingMode.Ignore;
            Add(_image);

            _spinner = new VisualElement { name = "thumbnail-tile-spinner" };
            _spinner.AddToClassList(SPINNER_BASE_CLASS);
            _spinner.AddToClassList(SpinnerUssClassName);
            // DesignSystemRuntime only rotates elements carrying BOTH
            // ds-spinner AND is-spinning. Without is-spinning the ring is
            // a static decorative circle — looks like the renderer hung.
            _spinner.AddToClassList(SPINNER_ACTIVE_CLASS);
            _spinner.pickingMode = PickingMode.Ignore;
            Add(_spinner);

            // Lazy-load trigger: only queue when the element receives its
            // first geometry pass (i.e. layout placed it somewhere visible).
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        /// <summary>
        /// Bind this tile to a renderer + request. Safe to call before the
        /// element is attached — the actual <c>Queue</c> call is deferred
        /// until the element has been laid out (lazy loading).
        ///
        /// Re-binding (called twice with different requests on the same tile)
        /// fully resets state: the previous animation strip is dropped, the
        /// spinner returns, and the new request goes through the lazy-load
        /// pipeline. Required for "evict + re-render" flows (skin swap,
        /// ownership change, locked-greyscale toggle).
        /// </summary>
        public void Bind(PrefabThumbnailRenderer renderer, ThumbnailRequest request)
        {
            // Drop the previous strip + flipbook BEFORE accepting the new
            // key, so a re-bind doesn't see stale `_anim != null` and skip
            // re-queuing.
            ClearAnim();
            UnsubscribeFromRenderer(_renderer);

            _renderer = renderer;
            _request = request;
            _requestSet = true;
            _queued = false;
            RemoveFromClassList(ReadyUssClassName);

            if (_renderer != null && _renderer.IsCached(request.Key))
            {
                AdoptAnimated(_renderer.GetAnimated(request.Key));
            }
            else if (_attached)
            {
                TryQueue();
            }
        }

        /// <summary>
        /// Force re-render. Use after the underlying prefab data changes
        /// (cosmetic skin swap, ownership state flip, etc.).
        /// </summary>
        public void Refresh()
        {
            if (_renderer == null || !_requestSet) return;
            _renderer.Evict(_request.Key);
            ClearAnim();
            _queued = false;
            RemoveFromClassList(ReadyUssClassName);
            if (_attached) TryQueue();
        }

        /// <summary>
        /// Drop the tile's queued / in-flight request when the consumer
        /// knows the tile is scrolling off-screen.
        /// </summary>
        public void Unqueue()
        {
            if (_renderer == null || !_requestSet) return;
            _renderer.Cancel(_request.Key);
            _queued = false;
        }

        // ─── Lifecycle ────────────────────────────────────────────────────

        void OnAttach(AttachToPanelEvent _)
        {
            _attached = true;
            if (_renderer != null && _requestSet && _renderer.IsCached(_request.Key))
                AdoptAnimated(_renderer.GetAnimated(_request.Key));
        }

        void OnDetach(DetachFromPanelEvent _)
        {
            _attached = false;
            StopFlipbook();
            UnsubscribeFromRenderer(_renderer);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width > 0f && evt.newRect.height > 0f)
                TryQueue();
        }

        void OnPointerEnter(PointerEnterEvent _)
        {
            _pointerOver = true;
            AddToClassList(HoverActiveUssClassName);
            ReevaluatePlayback();
        }

        void OnPointerLeave(PointerLeaveEvent _)
        {
            _pointerOver = false;
            RemoveFromClassList(HoverActiveUssClassName);
            ReevaluatePlayback();
        }

        void TryQueue()
        {
            if (_disposed || _queued || _renderer == null || !_requestSet) return;
            if (_renderer.IsCached(_request.Key))
            {
                AdoptAnimated(_renderer.GetAnimated(_request.Key));
                return;
            }
            // Subscribe BEFORE Queue so we can't miss the OnThumbnailReady
            // for a fast Static render that completes in the same Tick.
            SubscribeToRenderer(_renderer);
            _renderer.Queue(_request);
            _queued = true;
        }

        // ─── Renderer event plumbing ─────────────────────────────────────

        void SubscribeToRenderer(PrefabThumbnailRenderer r)
        {
            if (r == null || _subscribed) return;
            r.OnThumbnailReady += OnRendererReady;
            _subscribed = true;
        }

        void UnsubscribeFromRenderer(PrefabThumbnailRenderer r)
        {
            if (r == null || !_subscribed) return;
            r.OnThumbnailReady -= OnRendererReady;
            _subscribed = false;
        }

        void OnRendererReady(int key, AnimatedThumbnail anim)
        {
            if (_disposed) return;
            if (!_requestSet || key != _request.Key) return;
            AdoptAnimated(anim);
            // Once adopted, we don't care about further events for OTHER
            // tiles' keys; unsubscribe to keep the renderer's event list
            // proportional to in-flight tiles, not total tile count.
            UnsubscribeFromRenderer(_renderer);
        }

        // ─── Adopt finished thumbnail ────────────────────────────────────

        void AdoptAnimated(AnimatedThumbnail anim)
        {
            if (anim == null) return;
            _anim = anim;
            AddToClassList(ReadyUssClassName);

            if (anim.FrameCount <= 1 || anim.Mode == ThumbnailAnimationMode.Static)
            {
                _image.style.backgroundImage = new StyleBackground(anim.FirstFrame);
                StopFlipbook();
                return;
            }

            // Drop frame 0 immediately so consumers don't see a flash of
            // spinner-on-top-of-blank while the flipbook tick spools up.
            _image.style.backgroundImage = new StyleBackground(anim.Frames[0]);
            _lastFrameShown = 0;
            ReevaluatePlayback();
        }

        void ReevaluatePlayback()
        {
            if (_anim == null || _anim.FrameCount <= 1)
            {
                StopFlipbook();
                return;
            }

            bool shouldPlay = _trigger switch
            {
                ThumbnailPlaybackTrigger.OnHover => _pointerOver,
                _ => true,
            };

            if (shouldPlay) StartFlipbook();
            else
            {
                StopFlipbook();
                // Resting on frame 0 keeps the OnHover tiles in their
                // canonical "idle pose" between hovers — feels like the
                // model "wakes up" on hover rather than freezing wherever
                // the previous hover left it.
                if (_anim != null && _anim.FrameCount > 0 && _lastFrameShown != 0)
                {
                    _image.style.backgroundImage = new StyleBackground(_anim.Frames[0]);
                    _lastFrameShown = 0;
                }
            }
        }

        void StartFlipbook()
        {
            if (_anim == null || _anim.FrameCount <= 1) return;
            if (_flipbookTick != null) { _flipbookTick.Resume(); return; }
            _playbackStart = Time.realtimeSinceStartupAsDouble
                             - (_lastFrameShown / Mathf.Max(0.0001f, _anim.Fps));
            int intervalMs = Mathf.Max(16, (int)(1000f / _anim.Fps));
            _flipbookTick = schedule.Execute(StepFlipbook).Every(intervalMs);
        }

        void StepFlipbook()
        {
            if (_disposed || _anim == null) return;
            float elapsed = (float)(Time.realtimeSinceStartupAsDouble - _playbackStart);
            int frameIdx = ((int)Mathf.Floor(elapsed * _anim.Fps)) % _anim.FrameCount;
            if (frameIdx < 0) frameIdx += _anim.FrameCount;
            if (frameIdx == _lastFrameShown) return;
            _lastFrameShown = frameIdx;
            _image.style.backgroundImage = new StyleBackground(_anim.Frames[frameIdx]);
        }

        void StopFlipbook()
        {
            _flipbookTick?.Pause();
            _flipbookTick = null;
        }

        void ClearAnim()
        {
            _anim = null;
            _lastFrameShown = -1;
            StopFlipbook();
            _image.style.backgroundImage = new StyleBackground();
        }
    }
}
