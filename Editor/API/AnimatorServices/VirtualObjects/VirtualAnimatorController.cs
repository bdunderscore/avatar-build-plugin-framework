using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     Represents an animator controller that has been indexed by NDMF for faster manipulation. This class also
    ///     guarantees that certain assets have been cloned, specifically:
    ///     - AnimatorController
    ///     - StateMachine
    ///     - AnimatorState
    ///     - AnimatorStateTransition
    ///     - BlendTree
    ///     - AnimationClip
    ///     - Any state behaviors attached to the animator controller
    /// </summary>
    public class VirtualAnimatorController : VirtualNode, ICommitable<AnimatorController>, IDisposable
    {
        private readonly CloneContext _context;
        public string Name { get; set; }

        private ImmutableDictionary<string, AnimatorControllerParameter> _parameters;

        public ImmutableDictionary<string, AnimatorControllerParameter> Parameters
        {
            get => _parameters;
            set => _parameters = I(value ?? throw new ArgumentNullException(nameof(value)));
        }

        private readonly SortedDictionary<LayerPriority, LayerGroup> _layers = new();

        private struct LayerGroup
        {
            public List<VirtualLayer> Layers;
        }

        public VirtualAnimatorController(CloneContext context, string name = "")
        {
            _context = context;
            Name = name;
            Parameters = ImmutableDictionary<string, AnimatorControllerParameter>.Empty;
        }

        public void AddLayer(LayerPriority priority, VirtualLayer layer)
        {
            Invalidate();

            if (!_layers.TryGetValue(priority, out var group))
            {
                group = new LayerGroup { Layers = new List<VirtualLayer>() };
                _layers.Add(priority, group);
            }

            group.Layers.Add(layer);
        }

        public VirtualLayer AddLayer(LayerPriority priority, string name)
        {
            // implicitly creates state machine
            var layer = VirtualLayer.Create(_context, name);

            AddLayer(priority, layer);

            return layer;
        }

        public IEnumerable<VirtualLayer> Layers
        {
            get { return _layers.Values.SelectMany(l => l.Layers); }
        }

        public static VirtualAnimatorController Clone(CloneContext context, RuntimeAnimatorController controller)
        {
            switch (controller)
            {
                case AnimatorController ac: return new VirtualAnimatorController(context, ac);
                case AnimatorOverrideController aoc:
                {
                    using var _ = context.PushOverrideController(aoc);

                    return Clone(context, aoc.runtimeAnimatorController);
                }
                default: throw new NotImplementedException($"Unknown controller type {controller.GetType()}");
            }
        }

        private VirtualAnimatorController(CloneContext context, AnimatorController controller)
        {
            Name = controller.name;
            Parameters = controller.parameters.ToImmutableDictionary(p => p.name);

            var srcLayers = controller.layers;
            context.AllocateVirtualLayerSpace(srcLayers.Length);

            var p0Layers = srcLayers.Select((l, i) => VirtualLayer.Clone(context, l, i)).ToList();
            foreach (var layer in p0Layers)
            {
                layer.IsOriginalLayer = true;
            }

            _layers[new LayerPriority(0)] = new LayerGroup { Layers = p0Layers };
        }

        AnimatorController ICommitable<AnimatorController>.Prepare(CommitContext context)
        {
            var controller = new AnimatorController
            {
                name = Name,
                parameters = Parameters
                    .OrderBy(p => p.Key)
                    .Select(p =>
                    {
                        p.Value.name = p.Key;
                        return p.Value;
                    })
                    .ToArray()
            };

            foreach (var (layer, index) in Layers.Select((l, i) => (l, i)))
            {
                context.RegisterPhysicalLayerMapping(index, layer);
            }

            return controller;
        }

        void ICommitable<AnimatorController>.Commit(CommitContext context, AnimatorController obj)
        {
            obj.layers = Layers.Select(context.CommitObject).ToArray();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<VirtualNode> _EnumerateChildren()
        {
            return Layers;
        }
    }
}