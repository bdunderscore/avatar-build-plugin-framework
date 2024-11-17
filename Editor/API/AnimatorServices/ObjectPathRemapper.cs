#nullable enable

using System.Collections.Generic;
using JetBrains.Annotations;
using nadena.dev.ndmf.runtime;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     The ObjectPathRemapper is used to track GameObject movement in the hierarchy, and to update animation paths
    ///     accordingly.
    ///     While the ObjectPathRemapper is active, there are a few important rules around hierarchy and animation
    ///     maniuplation that must be observed.
    ///     1. The ObjectPathRemapper takes a snapshot of the object paths that were present at time of activation;
    ///     as such, any new animations added while it is active must use those paths, not those of the current
    ///     hierarchy. To help with this, you can use the `GetVirtualPathForObject` method to get the path that
    ///     should be used for newly generated animations.
    ///     2. Objects can be moved freely within the hierarchy; however, if you want to _remove_ an object, you must call
    ///     `ReplaceObject` on it first.
    ///     3. Objects can be freely added; if you want to use those objects in animations, use `GetVirtualPathForObject`
    ///     to get the path that should be used. This will automatically register that object, if necessary. If you'd
    ///     like to use animation clips with pre-existing paths on, for example, a newly instantiated prefab hierarchy,
    ///     use `RecordObjectTree` to ensure that those objects have their current paths recorded first. This ensures
    ///     that if those objects are moved in later stages, the paths will be updated appropriately.
    ///     Note that it's possible that these paths may collide with paths that _previously_ existed, so it's still
    ///     recommended to use `GetVirtualPathForObject` to ensure that the path is unique.
    /// </summary>
    [PublicAPI]
    public sealed class ObjectPathRemapper
    {
        private readonly Transform _root;
        private readonly Dictionary<Transform, List<string>> _objectToOriginalPaths = new();
        private readonly Dictionary<string, Transform> _pathToObject = new();

        private bool _cacheValid;
        private readonly Dictionary<string, string> _originalToMappedPath = new();

        internal ObjectPathRemapper(Transform root)
        {
            _root = root;
            RecordObjectTree(root);
        }

        public void ApplyChanges(AnimationIndex index)
        {
            UpdateCache();

            index.RewritePaths(_originalToMappedPath);
        }

        private void UpdateCache()
        {
            if (_cacheValid) return;

            _originalToMappedPath.Clear();

            foreach (var kvp in _objectToOriginalPaths)
            {
                var virtualPath = GetVirtualPathForObject(kvp.Key);

                if (virtualPath == null) continue;
                
                foreach (var path in kvp.Value)
                {
                    _originalToMappedPath[path] = virtualPath;
                }
            }
        }

        public void RecordObjectTree(Transform subtree)
        {
            GetVirtualPathForObject(subtree);

            foreach (Transform child in subtree)
            {
                RecordObjectTree(child);
            }
        }

        public GameObject? GetObjectForPath(string path)
        {
            var xform = _pathToObject.GetValueOrDefault(path);
            return xform ? xform.gameObject : null;
        }

        public string? GetVirtualPathForObject(GameObject obj)
        {
            return GetVirtualPathForObject(obj.transform);
        }

        public string? GetVirtualPathForObject(Transform t)
        {
            if (_objectToOriginalPaths.TryGetValue(t, out var paths))
            {
                return paths[0];
            }

            var path = RuntimeUtil.RelativePath(_root, t);
            if (path == null) return null;

            if (_pathToObject.ContainsKey(path))
            {
                path += "###PENDING_" + t.GetInstanceID();
            }

            _objectToOriginalPaths[t] = new List<string> { path };
            _pathToObject[path] = t;
            _cacheValid = false;

            return path;
        }

        public void ReplaceObject(GameObject old, GameObject newObject)
        {
            ReplaceObject(old.transform, newObject.transform);
        }

        public void ReplaceObject(Transform old, Transform newObject)
        {
            if (!_objectToOriginalPaths.TryGetValue(old, out var paths)) return;

            if (_objectToOriginalPaths.ContainsKey(newObject))
            {
                _objectToOriginalPaths[newObject].AddRange(paths);
            }
            else
            {
                _objectToOriginalPaths[newObject] = paths;
            }

            _objectToOriginalPaths.Remove(old);
            foreach (var path in paths)
            {
                _pathToObject[path] = newObject;
            }
        }

        public string MapPath(string originalPath)
        {
            UpdateCache();

            return _originalToMappedPath.GetValueOrDefault(originalPath, originalPath);
        }
    }
}