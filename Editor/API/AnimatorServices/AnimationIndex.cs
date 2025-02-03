#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor;

namespace nadena.dev.ndmf.animator
{
    public sealed class AnimationIndex
    {
        private readonly Func<IEnumerable<VirtualNode>> _getRoots;
        private readonly Func<long> _getInvalidationToken;

        private long _lastInvalidationToken;

        private readonly Action _invalidateAction;
        private bool _isValid;

        private bool IsValid => _isValid && _lastInvalidationToken == _getInvalidationToken();

        private readonly Dictionary<string, HashSet<VirtualClip>> _objectPathToClip = new();
        private readonly Dictionary<EditorCurveBinding, HashSet<VirtualClip>> _bindingToClip = new();
        private readonly Dictionary<VirtualClip, HashSet<EditorCurveBinding>> _lastBindings = new();
        
        internal AnimationIndex(
            Func<IEnumerable<VirtualAnimatorController>> getRoots,
            Func<long> getInvalidationToken)
        {
            _getRoots = getRoots;
            _getInvalidationToken = getInvalidationToken;
            _invalidateAction = () => _isValid = false;
        }
        
        public AnimationIndex(IEnumerable<VirtualNode> controllers)
        {
            _invalidateAction = () => _isValid = false;
            var controllerList = new List<VirtualNode>(controllers);
            _getRoots = () => controllerList;
            _getInvalidationToken = () => _lastInvalidationToken;
        }

        public IEnumerable<VirtualClip> GetClipsForObjectPath(string objectPath)
        {
            if (!IsValid) RebuildCache();

            if (_objectPathToClip.TryGetValue(objectPath, out var clips))
            {
                return clips;
            }

            return Enumerable.Empty<VirtualClip>();
        }

        public IEnumerable<VirtualClip> GetClipsForBinding(EditorCurveBinding binding)
        {
            if (!IsValid) RebuildCache();

            if (_bindingToClip.TryGetValue(binding, out var clips))
            {
                return clips;
            }

            return Enumerable.Empty<VirtualClip>();
        }

        public void RewritePaths(Func<string, string?> rewriteRules)
        {
            if (!IsValid) RebuildCache();

            var rewriteSet = _objectPathToClip.Values.SelectMany(s => s).Distinct();
            
            RewritePaths(rewriteSet, rewriteRules);

            foreach (var root in _getRoots())
            {
                if (root is VirtualAnimatorController vac)
                {
                    foreach (var layer in vac.Layers)
                    {
                        if (layer.AvatarMask is not null)
                        {
                            RewriteAvatarMask(layer.AvatarMask, rewriteRules);
                        }
                    }
                }
            }
        }

        private void RewriteAvatarMask(VirtualAvatarMask layerAvatarMask, Func<string, string?> rewriteRules)
        {
            Dictionary<string, float> outputDict = new();

            foreach (var kvp in layerAvatarMask.Elements)
            {
                var rewritten = rewriteRules(kvp.Key);
                if (rewritten != null)
                {
                    outputDict[rewritten] = kvp.Value;
                }
            }

            layerAvatarMask.Elements = outputDict.ToImmutableDictionary();
        }

        public void RewritePaths(Dictionary<string, string?> rewriteRules)
        {
            if (!IsValid) RebuildCache();
            
            HashSet<VirtualClip> rewriteSet = new();

            foreach (var key in rewriteRules.Keys)
            {
                if (!_objectPathToClip.TryGetValue(key, out var clips)) continue;
                rewriteSet.UnionWith(clips);
            }

            Func<string, string?> rewriteFunc = k =>
            {
                // Note: We don't use GetValueOrDefault here as we want to distinguish between null and missing keys
                // ReSharper disable once CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
                if (rewriteRules.TryGetValue(k, out var v)) return v;
                return k;
            };
            
            RewritePaths(rewriteSet, rewriteFunc);
        }

        private void RewritePaths(IEnumerable<VirtualClip> rewriteSet, Func<string, string?> rewriteFunc)
        {
            List<VirtualClip> recacheNeeded = new();

            foreach (var clip in rewriteSet)
            {
                clip.EditPaths(rewriteFunc);
                if (!_isValid)
                {
                    recacheNeeded.Add(clip);
                }

                _isValid = true;
            }

            foreach (var clip in recacheNeeded)
            {
                CacheClip(clip);
            }
        }

        public void EditClipsByBinding(IEnumerable<EditorCurveBinding> binding, Action<VirtualClip> processClip)
        {
            if (!IsValid) RebuildCache();

            var clips = binding.SelectMany(GetClipsForBinding).ToHashSet();
            var toRecache = new List<VirtualClip>();
            foreach (var clip in clips)
            {
                processClip(clip);
                if (!_isValid)
                {
                    toRecache.Add(clip);
                }

                _isValid = true;
            }

            foreach (var clip in toRecache)
            {
                CacheClip(clip);
            }
        }

        private void RebuildCache()
        {
            _objectPathToClip.Clear();
            _bindingToClip.Clear();
            _lastBindings.Clear();

            foreach (var clip in EnumerateClips())
            {
                CacheClip(clip);
            }

            _isValid = true;
        }

        private void CacheClip(VirtualClip clip)
        {
            if (_lastBindings.TryGetValue(clip, out var lastBindings))
            {
                foreach (var binding in lastBindings)
                {
                    _bindingToClip[binding].Remove(clip);
                    _objectPathToClip[binding.path].Remove(clip);
                }
            }
            else
            {
                lastBindings = new HashSet<EditorCurveBinding>();
                _lastBindings[clip] = lastBindings;
            }

            lastBindings.Clear();
            lastBindings.UnionWith(clip.GetObjectCurveBindings());
            lastBindings.UnionWith(clip.GetFloatCurveBindings());

            foreach (var binding in lastBindings)
            {
                if (!_bindingToClip.TryGetValue(binding, out var clips))
                {
                    clips = new HashSet<VirtualClip>();
                    _bindingToClip[binding] = clips;
                }

                clips.Add(clip);

                if (!_objectPathToClip.TryGetValue(binding.path, out var pathClips))
                {
                    pathClips = new HashSet<VirtualClip>();
                    _objectPathToClip[binding.path] = pathClips;
                }

                pathClips.Add(clip);
            }
        }

        private IEnumerable<VirtualClip> EnumerateClips()
        {
            HashSet<object> visited = new();
            Queue<VirtualNode> queue = new();

            _lastInvalidationToken = _getInvalidationToken();
            foreach (var controller in _getRoots())
            {
                queue.Enqueue(controller);
            }

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                node.RegisterCacheObserver(_invalidateAction);
                
                if (!visited.Add(node))
                {
                    continue;
                }

                foreach (var child in node.EnumerateChildren())
                {
                    if (!visited.Contains(child)) queue.Enqueue(child);
                }

                if (node is VirtualClip clip)
                {
                    yield return clip;
                }
            }
        }
    }
}