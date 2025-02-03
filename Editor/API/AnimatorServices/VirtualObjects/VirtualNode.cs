#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     Base class for all virtual animation nodes. Contains common functionality for cache invalidation.
    ///     Generally, external libraries should not use this class directly.
    /// </summary>
    [PublicAPI]
    public abstract class VirtualNode
    {
        private Action? _lastCacheObserver;

        internal VirtualNode()
        {
        }

        internal void Invalidate()
        {
            _lastCacheObserver?.Invoke();
            _lastCacheObserver = null;
        }

        internal T I<T>(T val)
        {
            Invalidate();
            return val;
        }

        internal void RegisterCacheObserver(Action? observer)
        {
            if (observer != _lastCacheObserver && _lastCacheObserver != null)
            {
                _lastCacheObserver.Invoke();
            }

            _lastCacheObserver = observer;
        }

        public IEnumerable<VirtualNode> AllReachableNodes()
        {
            var visited = new HashSet<VirtualNode>();
            var queue = new Queue<VirtualNode>();
            
            queue.Enqueue(this);
            visited.Add(this);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                yield return node;

                foreach (var child in node.EnumerateChildren())
                {
                    if (visited.Add(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }
        
        public IEnumerable<VirtualNode> EnumerateChildren()
        {
            return _EnumerateChildren();
        }

        protected virtual IEnumerable<VirtualNode> _EnumerateChildren()
        {
            yield break;
        }
    }
}