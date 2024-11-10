using System;
using System.Collections.Generic;
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
    public class VirtualAnimatorController : ICommitable<AnimatorController>, IDisposable
    {
        public string Name { get; set; }
        public Dictionary<string, AnimatorControllerParameter> Parameters { get; }
        public List<VirtualLayer> Layers { get; set; }

        public VirtualAnimatorController(string name = "")
        {
            Name = name;
            Parameters = new Dictionary<string, AnimatorControllerParameter>();
            Layers = new List<VirtualLayer>();
        }

        public static VirtualAnimatorController Clone(CloneContext context, RuntimeAnimatorController controller)
        {
            // TODO: AnimatorOverrideController support
            if (controller is not AnimatorController)
            {
                return null;
            }

            return new VirtualAnimatorController(context, (AnimatorController)controller);
        }

        private VirtualAnimatorController(CloneContext context, AnimatorController controller)
        {
            Name = controller.name;
            Parameters = controller.parameters.ToDictionary(p => p.name);

            var srcLayers = controller.layers;
            context.AllocateVirtualLayerSpace(srcLayers.Length);

            Layers = srcLayers.Select((l, i) => VirtualLayer.Clone(context, l, i)).ToList();
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
    }
}