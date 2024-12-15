#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private Dictionary<string, string?> _originalToMappedPath = new();

        internal ObjectPathRemapper(Transform root)
        {
            _root = root;
            RecordObjectTree(root);
        }

        /// <summary>
        ///     Clears the path remapping cache. This should be called after making changes to the hierarchy,
        ///     such as moving objects around.
        /// </summary>
        public void ClearCache()
        {
            _cacheValid = false;
        }

        /// <summary>
        ///     Returns a dictionary mapping from virtual paths (ie - those currently in use in animations) to the corresponding
        ///     object's current paths.
        ///     Deleted objects are represented by a null value.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string?> GetVirtualToRealPathMap()
        {
            ClearCache();
            UpdateCache();

            var result = _originalToMappedPath;
            _originalToMappedPath = new Dictionary<string, string?>();

            return result;
        }

        private void UpdateCache()
        {
            if (_cacheValid) return;

            _originalToMappedPath.Clear();

            foreach (var kvp in _objectToOriginalPaths)
            {
                var realPath = kvp.Key != null ? RuntimeUtil.RelativePath(_root, kvp.Key) : null;
                
                foreach (var path in kvp.Value)
                {
                    if (path == "") continue;
                    _originalToMappedPath[path] = realPath;
                }
            }
        }

        /// <summary>
        ///     Ensures all objects in this object and its children are recorded in the object path mapper.
        /// </summary>
        /// <param name="subtree"></param>
        public void RecordObjectTree(Transform subtree)
        {
            GetVirtualPathForObject(subtree);

            foreach (Transform child in subtree)
            {
                RecordObjectTree(child);
            }
        }

        /// <summary>
        ///     Returns the GameObject corresponding to an animation path, if any. This is based on where the object
        ///     was located at the time it was first discovered, _not_ its current location.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public GameObject? GetObjectForPath(string path)
        {
            var xform = _pathToObject.GetValueOrDefault(path);
            return xform ? xform.gameObject : null;
        }

        /// <summary>
        ///     Returns a virtual path for the given GameObject. For most objects, this will be their actual path; however,
        ///     if that path is unusable (e.g. another object was previously at that path), a new path will be generated
        ///     instead.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string GetVirtualPathForObject(GameObject obj)
        {
            return GetVirtualPathForObject(obj.transform);
        }

        /// <summary>
        ///     Returns a virtual path for the given Transform. For most objects, this will be their actual path; however,
        ///     if that path is unusable (e.g. another object was previously at that path), a new path will be generated
        ///     instead.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public string GetVirtualPathForObject(Transform t)
        {
            if (_objectToOriginalPaths.TryGetValue(t, out var paths))
            {
                return paths[0];
            }

            var path = RuntimeUtil.RelativePath(_root, t);
            if (path == null) path = t.gameObject.name + "###UNROOTED_" + t.GetInstanceID();

            if (_pathToObject.ContainsKey(path))
            {
                path += "###PENDING_" + t.GetInstanceID();
            }

            _objectToOriginalPaths[t] = new List<string> { path };
            _pathToObject[path] = t;
            _cacheValid = false;

            return path;
        }

        /// <summary>
        ///     Replaces all references to `old` with `newObject`.
        /// </summary>
        /// <param name="old"></param>
        /// <param name="newObject"></param>
        public void ReplaceObject(GameObject old, GameObject newObject)
        {
            ReplaceObject(old.transform, newObject.transform);
        }

        /// <summary>
        ///     Replaces all references to `old` with `newObject`.
        /// </summary>
        /// <param name="old"></param>
        /// <param name="newObject"></param>
        public void ReplaceObject(Transform old, Transform newObject)
        {
            if (!_objectToOriginalPaths.TryGetValue(old, out var paths)) return;

            ClearCache();

            if (_objectToOriginalPaths.TryGetValue(newObject, out var originalPaths))
            {
                originalPaths.AddRange(paths);
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
    }
}