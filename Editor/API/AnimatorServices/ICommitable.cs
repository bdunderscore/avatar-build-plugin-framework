using System.Collections.Generic;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    internal interface ICommitable<T>
    {
        /// <summary>
        ///     Allocates the destination unity object, but does not recurse back into the CommitContext.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        T Prepare(CommitContext context);

        /// <summary>
        ///     Fills in all fields of the destination unity object. This may recurse back into the CommitContext.
        /// </summary>
        /// <param name="context"></param>
        void Commit(CommitContext context, T obj);
    }

    internal class CommitContext
    {
        private readonly Dictionary<object, object> _commitCache = new();
        private readonly Dictionary<int, VirtualLayer> _virtIndexToVirtLayer = new();
        private readonly Dictionary<VirtualLayer, int> _virtLayerToPhysIndex = new();

        internal R CommitObject<R>(ICommitable<R> obj) where R : class
        {
            if (obj == null) return null;
            if (_commitCache.TryGetValue(obj, out var result)) return (R)result;

            var resultObj = obj.Prepare(this);
            _commitCache[obj] = resultObj;

            obj.Commit(this, resultObj);

            return resultObj;
        }

        public void RegisterVirtualLayerMapping(VirtualLayer virtualLayer, int virtualLayerIndex)
        {
            _virtIndexToVirtLayer[virtualLayerIndex] = virtualLayer;
        }

        public void RegisterPhysicalLayerMapping(int physicalLayerIndex, VirtualLayer virtualLayer)
        {
            _virtLayerToPhysIndex[virtualLayer] = physicalLayerIndex;
        }

        public int VirtualToPhysicalLayerIndex(int index)
        {
            if (_virtIndexToVirtLayer.TryGetValue(index, out var virtLayer)
                && _virtLayerToPhysIndex.TryGetValue(virtLayer, out var physIndex)
               )
            {
                return physIndex;
            }

            return -1;
        }

        /// <summary>
        ///     Destroys all objects committed in this context. Primarily intended for test cleanup.
        /// </summary>
        public void DestroyAllImmediate()
        {
            foreach (var obj in _commitCache.Values)
            {
                if (obj is Object unityObj)
                {
                    Object.DestroyImmediate(unityObj);
                }
            }
        }
    }
}