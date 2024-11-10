using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace nadena.dev.ndmf.animator
{
    public sealed class AnimationIndex
    {
        private readonly List<VirtualAnimatorController> _controllers;

        private readonly Action _invalidateAction;
        private bool _isValid;

        private readonly Dictionary<string, HashSet<VirtualClip>> _objectPathToClip = new();
        private readonly Dictionary<EditorCurveBinding, HashSet<VirtualClip>> _bindingToClip = new();
        private readonly Dictionary<VirtualClip, HashSet<EditorCurveBinding>> _lastBindings = new();

        public AnimationIndex(IEnumerable<VirtualAnimatorController> controllers)
        {
            _invalidateAction = () => _isValid = false;
            _controllers = new List<VirtualAnimatorController>(controllers);
        }

        public IEnumerable<VirtualClip> GetClipsForObjectPath(string objectPath)
        {
            if (!_isValid) RebuildCache();

            if (_objectPathToClip.TryGetValue(objectPath, out var clips))
            {
                return clips;
            }

            return Enumerable.Empty<VirtualClip>();
        }

        public IEnumerable<VirtualClip> GetClipsForBinding(EditorCurveBinding binding)
        {
            if (!_isValid) RebuildCache();

            if (_bindingToClip.TryGetValue(binding, out var clips))
            {
                return clips;
            }

            return Enumerable.Empty<VirtualClip>();
        }

        public void RewritePaths(Dictionary<string, string> rewriteRules)
        {
            if (!_isValid) RebuildCache();

            List<VirtualClip> recacheNeeded = new();
            HashSet<VirtualClip> rewriteSet = new();

            foreach (var key in rewriteRules.Keys)
            {
                if (!_objectPathToClip.TryGetValue(key, out var clips)) continue;
                rewriteSet.UnionWith(clips);
            }

            Func<string, string> rewriteFunc = rewriteRules.GetValueOrDefault;
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
            if (!_isValid) RebuildCache();

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

            foreach (var controller in _controllers)
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