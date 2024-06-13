﻿#region

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nadena.dev.ndmf.rq;
using UnityEngine;

#endregion

namespace nadena.dev.ndmf.preview
{
    internal enum PipelineStatus
    {
        NotReady,
        Ready,
        Invalidated,
        Disposed
    }

    class StageDescriptor
    {
        #region Initial configuration

        public readonly IRenderFilter Filter;
        public readonly ImmutableList<ImmutableList<Renderer>> Originals;

        public StageDescriptor(IRenderFilter filter, ComputeContext context)
        {
            context.TryObserve(filter.TargetGroups, out var unsorted);

            if (unsorted == null) unsorted = ImmutableList<IImmutableList<Renderer>>.Empty;

            Originals = unsorted
                .Select(group => group.OrderBy(o => o.GetInstanceID()).ToImmutableList())
                .OrderBy(group => group.First().GetInstanceID())
                .ToImmutableList();
        }

        #endregion

        public List<Task<NodeController>> NodeTasks = new();
    } 

    /// <summary>
    /// Represents a single, instantiated pipeline for building and maintaining all proxy objects.
    /// </summary>
    internal class ProxyPipeline
    {
        private List<StageDescriptor> _stages = new();
        private Dictionary<Renderer, ProxyObjectController> _proxies = new();
        private List<NodeController> _nodes = new(); // in OnFrame execution order

        private Task _buildTask;

        private TaskCompletionSource<object> _invalidater = new(), _completedBuild = new();
        internal bool IsInvalidated => _invalidater.Task.IsCompleted;

        internal ImmutableDictionary<Renderer, Renderer> OriginalToProxyRenderer =
            ImmutableDictionary<Renderer, Renderer>.Empty;

        internal ImmutableDictionary<GameObject, GameObject> OriginalToProxyObject =
            ImmutableDictionary<GameObject, GameObject>.Empty;

        internal ImmutableDictionary<GameObject, GameObject> ProxyToOriginalObject =
            ImmutableDictionary<GameObject, GameObject>.Empty;

        // ReSharper disable once NotAccessedField.Local
        // needed to prevent GC of the ComputeContext
        private ComputeContext _ctx;
        
        internal void Invalidate()
        {
            Debug.Log("=== pipeline invalidate ===");
            using (new SyncContextScope(ReactiveQueryScheduler.SynchronizationContext))
            {
                _invalidater.TrySetResult(null);
            }
        }

        private readonly Action InvalidateAction;

        public bool IsReady => _buildTask.IsCompletedSuccessfully;
        public bool IsFailed => _buildTask.IsFaulted;

        public IEnumerable<(Renderer, Renderer)> Renderers
            => _proxies.Select(kvp => (kvp.Key, kvp.Value.Renderer));

        public ProxyPipeline(IEnumerable<IRenderFilter> filters, ProxyPipeline priorPipeline = null)
        {
            InvalidateAction = Invalidate;
            
            using (new SyncContextScope(ReactiveQueryScheduler.SynchronizationContext))
            {
                _buildTask = Task.Factory.StartNew(
                    _ => Build(filters, priorPipeline),
                    null,
                    CancellationToken.None,
                    0,
                    TaskScheduler.FromCurrentSynchronizationContext()
                ).Unwrap();
            }
        }

        private async Task Build(IEnumerable<IRenderFilter> filters, ProxyPipeline priorPipeline)
        {
            var context = new ComputeContext(() => "ProxyPipeline construction");
            context.Invalidate = InvalidateAction;
            context.OnInvalidate = _invalidater.Task;
            _ctx = context; // prevent GC

            List<IRenderFilter> filterList = filters.ToList();
            List<StageDescriptor> priorStages = priorPipeline?._stages;

            Dictionary<Renderer, Task<NodeController>> nodeTasks = new();

            for (int i = 0; i < filterList.Count(); i++)
            {
                // TODO: Reuse logic

                var filter = filterList[i];
                var stage = new StageDescriptor(filter, context);
                _stages.Add(stage);

                var prior = priorPipeline?._stages.ElementAtOrDefault(i);
                if (prior?.Filter != filter)
                {
                    prior = null;
                }

                int groupIndex = -1;
                foreach (var group in stage.Originals)
                {
                    groupIndex++;
                    var resolved = group.Select(r =>
                    {
                        if (nodeTasks.TryGetValue(r, out var task))
                        {
                            return task.ContinueWith(task1 => (r, task1.Result.GetProxyFor(r)));
                        }
                        else
                        {
                            var proxy = new ProxyObjectController(r);
                            proxy.OnPreFrame();
                            _proxies.Add(r, proxy);

                            OriginalToProxyRenderer = OriginalToProxyRenderer.Add(r, proxy.Renderer);
                            OriginalToProxyObject = OriginalToProxyObject.Add(r.gameObject, proxy.Renderer.gameObject);
                            ProxyToOriginalObject = ProxyToOriginalObject.Add(proxy.Renderer.gameObject, r.gameObject);
                            
                            return Task.FromResult((r, proxy));
                        }
                    });


                    var node = Task.WhenAll(resolved).ContinueWith(items =>
                        {
                            // TODO - prior node handling

                            return NodeController.Create(filter, items.Result.ToList());
                        })
                        .Unwrap();

                    stage.NodeTasks.Add(node);

                    foreach (var renderer in group)
                    {
                        nodeTasks[renderer] = node;
                    }
                }
            }

            await Task.WhenAll(_stages.SelectMany(s => s.NodeTasks))
                .ContinueWith(result => { _completedBuild.TrySetResult(null); });

            foreach (var stage in _stages)
            {
                foreach (var node in stage.NodeTasks)
                {
                    var resolvedNode = await node;
                    _nodes.Add(resolvedNode);
                    _ = resolvedNode.OnInvalidate.ContinueWith(_ => Invalidate());
                }
            }
        }

        public void OnFrame()
        {
            if (!IsReady) return;

            foreach (var pair in _proxies)
            {
                pair.Value.OnPreFrame();
            }

            foreach (var node in _nodes)
            {
                node.OnFrame();
            }
        }

        public void Dispose()
        {
            using (new SyncContextScope(ReactiveQueryScheduler.SynchronizationContext))
            {
                // We need to make sure this task runs on the unity main thread so it can delete the proxy objects
                _completedBuild.Task.ContinueWith(_ =>
                    {
                        foreach (var stage in _stages)
                        {
                            foreach (var node in stage.NodeTasks)
                            {
                                if (node.IsCompletedSuccessfully)
                                {
                                    node.Result.Dispose();
                                }
                            }

                            foreach (var proxy in _proxies.Values)
                            {
                                proxy.Dispose();
                            }
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.RunContinuationsAsynchronously,
                    TaskScheduler.FromCurrentSynchronizationContext()
                );
            }
        }

        public void ShowError()
        {
            Debug.LogException(_buildTask.Exception);
        }
    }
}