using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     An abstraction over Unity AnimationClips. This class is designed to allow for low-overhead mutation of animation
    ///     clips, and in particular provides helpers for common operations (e.g. rewriting all paths in a clip).
    /// </summary>
    [PublicAPI]
    public class VirtualClip : VirtualMotion, IDisposable
    {
        private AnimationClip _clip;

        public override string Name
        {
            get => _clip.name;
            set => _clip.name = I(value);
        }

        /// <summary>
        ///     True if this is a marker clip; in this case, the clip is immutable and any attempt to mutate it will be
        ///     ignored. The clip will not be cloned on commit.
        /// </summary>
        public bool IsMarkerClip { get; private set; }

        /// <summary>
        ///     True if this clip has been modified since it was cloned or created.
        /// </summary>
        public bool IsDirty { get; private set; }

        private bool _useHighQualityCurves;
        /// <summary>
        ///     Controls the (unexposed) High Quality Curve setting on the animation clip.
        /// </summary>
        public bool UseHighQualityCurves
        {
            get => _useHighQualityCurves;
            set => _useHighQualityCurves = I(value);
        }

        /// <summary>
        ///     Controls the `legacy` setting on the animation clip.
        /// </summary>
        public bool Legacy
        {
            get => _clip.legacy;
            set => _clip.legacy = I(value);
        }

        public Bounds LocalBounds
        {
            get => _clip.localBounds;
            set => _clip.localBounds = I(value);
        }

        public AnimationClipSettings Settings
        {
            get => AnimationUtility.GetAnimationClipSettings(_clip);
            set
            {
                if (value.additiveReferencePoseClip != null)
                {
                    throw new ArgumentException("Use the AdditiveReferencePoseClip property instead",
                        nameof(value.additiveReferencePoseClip));
                }

                AnimationUtility.SetAnimationClipSettings(_clip, value);
            }
        }

        private VirtualMotion _additiveReferencePoseClip;

        public VirtualMotion AdditiveReferencePoseClip
        {
            get => _additiveReferencePoseClip;
            set => _additiveReferencePoseClip = I(value);
        }
        
        public float AdditiveReferencePoseTime
        {
            get => Settings.additiveReferencePoseTime;
            set
            {
                var settings = Settings;
                settings.additiveReferencePoseTime = I(value);
                Settings = settings;
            }
        }

        public WrapMode WrapMode
        {
            get => _clip.wrapMode;
            set => _clip.wrapMode = I(value);
        }

        public float FrameRate
        {
            get => _clip.frameRate;
            set => _clip.frameRate = I(value);
        }

        private Dictionary<EditorCurveBinding, CachedCurve<AnimationCurve>> _curveCache = new(ECBComparator.Instance);

        private Dictionary<EditorCurveBinding, CachedCurve<ObjectReferenceKeyframe[]>> _pptrCurveCache =
            new(ECBComparator.Instance);

        private struct CachedCurve<T>
        {
            // If null and Dirty is false, the curve has not been cached yet.
            // If null and Dirty is true, the curve has been deleted.
            public T Value;
            public bool Dirty;

            public override string ToString()
            {
                return $"CachedCurve<{typeof(T).Name}> {{ Value = {Value}, Dirty = {Dirty} }}";
            }
        }

        /// <summary>
        ///     Creates a VirtualClip representing a "marker" clip. This is a clip which must be preserved, as-is, in the
        ///     final avatar. For example, VRChat's proxy animations fall under this category. Any attempt to mutate a
        ///     marker clip will be ignored.
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static VirtualClip FromMarker(AnimationClip clip)
        {
            return new VirtualClip(clip, true);
        }

        /// <summary>
        ///     Clones an animation clip into a VirtualClip. The provided BuildContext is used to determine which platform
        ///     to use to query for marker clips; if a marker clip is found, it will be treated as immutable.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static VirtualClip Clone(
            CloneContext cloneContext,
            AnimationClip clip
        )
        {
            if (clip == null) return null;

            clip = cloneContext.MapClipOnClone(clip);

            if (cloneContext.PlatformBindings.IsSpecialMotion(clip))
            {
                return FromMarker(clip);
            }

            // workaround Rider warning bug
            VirtualClip clonedClip = null;
            if (cloneContext?.TryGetValue(clip, out clonedClip) == true)
            {
                return clonedClip;
            }

            var newClip = Object.Instantiate(clip);
            newClip.name = clip.name;
            
            var virtualClip = new VirtualClip(newClip, false);
            
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.additiveReferencePoseClip != null)
            {
                // defer call until after we register this VirtualClip, to avoid infinite recursion
                cloneContext.DeferCall(() =>
                {
                    var refPoseClip = cloneContext.Clone(settings.additiveReferencePoseClip);
                    settings.additiveReferencePoseClip = null;
                    AnimationUtility.SetAnimationClipSettings(newClip, settings);
                    virtualClip.AdditiveReferencePoseClip = refPoseClip;
                });
            }

            return virtualClip;
        }

        /// <summary>
        ///     Clones a VirtualClip. The new VirtualClip is backed by an independent copy of the original clip.
        /// </summary>
        /// <returns></returns>
        public VirtualClip Clone()
        {
            var newClip = Object.Instantiate(_clip);
            newClip.name = _clip.name;

            var virtualClip = new VirtualClip(newClip, IsMarkerClip);
            virtualClip.UseHighQualityCurves = UseHighQualityCurves;
            virtualClip.AdditiveReferencePoseClip = AdditiveReferencePoseClip;
            virtualClip.AdditiveReferencePoseTime = AdditiveReferencePoseTime;
            virtualClip.IsDirty = IsDirty;
            virtualClip._curveCache =
                _curveCache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, ECBComparator.Instance);
            virtualClip._pptrCurveCache =
                _pptrCurveCache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, ECBComparator.Instance);

            return virtualClip;
        }

        public static VirtualClip Create(string name)
        {
            var clip = new AnimationClip { name = name };
            return new VirtualClip(clip, false);
        }

        private VirtualClip(AnimationClip clip, bool isMarker)
        {
            _clip = clip;
            IsDirty = false;
            IsMarkerClip = isMarker;

            // This secret property can be changed by SetCurves calls, so preserve its current value.
            UseHighQualityCurves = new SerializedObject(clip).FindProperty("m_UseHighQualityCurve").boolValue;

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                _curveCache.Add(binding, new CachedCurve<AnimationCurve>());
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                _pptrCurveCache.Add(binding, new CachedCurve<ObjectReferenceKeyframe[]>());
            }
        }

        public IEnumerable<EditorCurveBinding> GetFloatCurveBindings()
        {
            return _curveCache
                .Where(kvp => kvp.Value.Value != null || !kvp.Value.Dirty)
                .Select(kvp => kvp.Key).ToList();
        }

        public IEnumerable<EditorCurveBinding> GetObjectCurveBindings()
        {
            return _pptrCurveCache
                .Where(kvp => kvp.Value.Value != null || !kvp.Value.Dirty)
                .Select(kvp => kvp.Key).ToList();
        }

        /// <summary>
        ///     Edit the paths of all bindings in this clip using the provided function. If this results in a path collision,
        ///     it is indeterminate which binding will be preserved. If null is returned, the binding will be deleted.
        /// </summary>
        /// <param name="pathEditor"></param>
        public void EditPaths(Func<string, string> pathEditor)
        {
            if (IsMarkerClip) return;

            _curveCache = Transform(_curveCache, AnimationUtility.GetEditorCurve);
            _pptrCurveCache = Transform(_pptrCurveCache, AnimationUtility.GetObjectReferenceCurve);

            Dictionary<EditorCurveBinding, CachedCurve<T>> Transform<T>(
                Dictionary<EditorCurveBinding, CachedCurve<T>> cache, Func<AnimationClip, EditorCurveBinding, T> getter)
            {
                var newCache = new Dictionary<EditorCurveBinding, CachedCurve<T>>(ECBComparator.Instance);
                foreach (var kvp in cache)
                {
                    var binding = kvp.Key;
                    var newBinding = binding;
                    newBinding.path = pathEditor(binding.path);

                    if (ECBComparator.Instance.Equals(binding, newBinding))
                    {
                        newCache[newBinding] = kvp.Value;
                        continue;
                    }

                    IsDirty = true;
                    Invalidate();

                    // Any binding originally present needs some kind of presence in the new cache; start off by
                    // inserting a deleted entry, we'll overwrite it later if appropriate.
                    if (!newCache.ContainsKey(binding))
                    {
                        newCache[binding] = new CachedCurve<T>
                        {
                            Dirty = true
                        };
                    }

                    if (newBinding.path == null)
                    {
                        // Delete the binding
                        continue;
                    }

                    // Load cache entry if not loaded
                    var entry = kvp.Value;
                    if (!entry.Dirty && entry.Value == null)
                    {
                        entry.Value = getter(_clip, binding);
                        entry.Dirty = true;
                    }

                    newCache[newBinding] = entry;
                }

                return newCache;
            }
        }

        public AnimationCurve GetFloatCurve(EditorCurveBinding binding)
        {
            if (_curveCache.TryGetValue(binding, out var cached))
            {
                if (cached.Dirty == false && cached.Value == null)
                {
                    cached.Value = AnimationUtility.GetEditorCurve(_clip, binding);
                    _curveCache[binding] = cached;
                }
            }

            return cached.Value;
        }

        public ObjectReferenceKeyframe[] GetObjectCurve(EditorCurveBinding binding)
        {
            if (_pptrCurveCache.TryGetValue(binding, out var cached))
            {
                if (cached.Dirty == false && cached.Value == null)
                {
                    cached.Value = AnimationUtility.GetObjectReferenceCurve(_clip, binding);
                    _pptrCurveCache[binding] = cached;
                }
            }

            return cached.Value;
        }

        public void SetFloatCurve(EditorCurveBinding binding, AnimationCurve curve)
        {
            if (binding.isPPtrCurve || binding.isDiscreteCurve)
            {
                throw new ArgumentException("Binding must be a float curve", nameof(binding));
            }

            if (IsMarkerClip) return;

            Invalidate();

            if (!_curveCache.TryGetValue(binding, out var cached))
            {
                cached = new CachedCurve<AnimationCurve>();
            }

            cached.Value = curve;
            cached.Dirty = true;
            IsDirty = true;

            _curveCache[binding] = cached;
        }

        public void SetObjectCurve(EditorCurveBinding binding, ObjectReferenceKeyframe[] curve)
        {
            if (!binding.isPPtrCurve)
            {
                throw new ArgumentException("Binding must be a PPtr curve", nameof(binding));
            }

            if (IsMarkerClip) return;

            Invalidate();
            
            if (!_pptrCurveCache.TryGetValue(binding, out var cached))
            {
                cached = new CachedCurve<ObjectReferenceKeyframe[]>();
            }

            cached.Value = curve;
            cached.Dirty = true;
            IsDirty = true;

            _pptrCurveCache[binding] = cached;
        }

        protected override Motion Prepare(object context)
        {
            return _clip;
        }

        protected override void Commit(object context_, Motion obj)
        {
            if (IsMarkerClip || !IsDirty) return;
            
            var context = (CommitContext)context_;

            var clip = (AnimationClip)obj;

            // WORKAROUND: AnimationUtility.SetEditorCurves doesn't actually delete curves when null, despite the
            // documentation claiming it will. Fault in all uncached curves, then clear everything.
            foreach (var curve in _curveCache.ToList())
            {
                if (!curve.Value.Dirty && curve.Value.Value == null) GetFloatCurve(curve.Key);
            }

            foreach (var curve in _pptrCurveCache.ToList())
            {
                if (!curve.Value.Dirty && curve.Value.Value == null) GetObjectCurve(curve.Key);
            }

            clip.ClearCurves();

            var changedBindings = _curveCache.Where(c => c.Value.Dirty || c.Value.Value != null).ToList();
            var changedPptrBindings = _pptrCurveCache.Where(c => c.Value.Dirty || c.Value.Value != null).ToList();

            if (changedBindings.Count > 0)
            {
                var bindings = changedBindings.Select(c => c.Key).ToArray();
                var curves = changedBindings.Select(c => c.Value.Value).ToArray();

                AnimationUtility.SetEditorCurves(clip, bindings, curves);
            }

            if (changedPptrBindings.Count > 0)
            {
                var bindings = changedPptrBindings.Select(c => c.Key).ToArray();
                var curves = changedPptrBindings.Select(c => c.Value.Value).ToArray();

                AnimationUtility.SetObjectReferenceCurves(clip, bindings, curves);
            }

            // Restore HighQualityCurves value
            var serializedObject = new SerializedObject(clip);
            serializedObject.FindProperty("m_UseHighQualityCurve").boolValue = UseHighQualityCurves;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            // Restore additive reference pose
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.additiveReferencePoseClip = AdditiveReferencePoseClip != null
                ? (AnimationClip)context.CommitObject(AdditiveReferencePoseClip)
                : null;
            settings.additiveReferencePoseTime = AdditiveReferencePoseTime;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            
            this._clip = null;
        }

        public AnimationCurve GetFloatCurve(string path, Type type, string prop)
        {
            return GetFloatCurve(EditorCurveBinding.FloatCurve(path, type, prop));
        }

        public ObjectReferenceKeyframe[] GetObjectCurve(string path, Type type, string prop)
        {
            return GetObjectCurve(EditorCurveBinding.PPtrCurve(path, type, prop));
        }

        public void SetFloatCurve(string path, Type type, string prop, AnimationCurve curve)
        {
            SetFloatCurve(EditorCurveBinding.FloatCurve(path, type, prop), curve);
        }

        public void SetObjectCurve(string path, Type type, string prop, ObjectReferenceKeyframe[] curve)
        {
            SetObjectCurve(EditorCurveBinding.PPtrCurve(path, type, prop), curve);
        }

        public override void Dispose()
        {
            if (_clip != null) Object.DestroyImmediate(_clip);
            this._clip = null;
        }
    }
}