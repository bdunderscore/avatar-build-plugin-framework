using System;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     A layer within a VirtualAnimatorController
    /// </summary>
    public class VirtualLayer : ICommitable<AnimatorControllerLayer>, IDisposable
    {
        /// <summary>
        ///     Returns a "virtual layer index" which can be used to map to the actual layer index in the animator controller,
        ///     even if layer order changes. This will typically be a very large value (>2^16).
        /// </summary>
        public int VirtualLayerIndex { get; }

        public VirtualStateMachine StateMachine { get; set; }

        public AvatarMask AvatarMask { get; set; }
        public AnimatorLayerBlendingMode BlendingMode { get; set; }
        public float DefaultWeight { get; set; }
        public bool IKPass { get; set; }

        public string Name { get; set; }

        public bool SyncedLayerAffectsTiming { get; set; }
        public int SyncedLayerIndex { get; set; }


        public static VirtualLayer Clone(CloneContext context, AnimatorControllerLayer layer, int physicalLayerIndex)
        {
            if (layer == null) return null;

            var clone = new VirtualLayer(context, layer, physicalLayerIndex);

            // TODO: motion, behavior overrides

            return clone;
        }

        public static VirtualLayer NewLayer(CloneContext context, string name = "(unnamed)")
        {
            return new VirtualLayer(context, name);
        }

        private VirtualLayer(CloneContext context, AnimatorControllerLayer layer, int physicalLayerIndex)
        {
            VirtualLayerIndex = context.CloneSourceToVirtualLayerIndex(physicalLayerIndex);
            Name = layer.name;
            AvatarMask = layer.avatarMask == null ? null : Object.Instantiate(layer.avatarMask);
            BlendingMode = layer.blendingMode;
            DefaultWeight = layer.defaultWeight;
            IKPass = layer.iKPass;
            SyncedLayerAffectsTiming = layer.syncedLayerAffectsTiming;
            SyncedLayerIndex = context.CloneSourceToVirtualLayerIndex(layer.syncedLayerIndex);

            StateMachine = VirtualStateMachine.Clone(context, layer.stateMachine);
        }

        private VirtualLayer(CloneContext context, string name)
        {
            VirtualLayerIndex = context.AllocateSingleVirtualLayer();
            Name = name;
            AvatarMask = null;
            BlendingMode = AnimatorLayerBlendingMode.Override;
            DefaultWeight = 1;
            IKPass = false;
            SyncedLayerAffectsTiming = false;
            SyncedLayerIndex = -1;

            StateMachine = new VirtualStateMachine(name);
        }

        AnimatorControllerLayer ICommitable<AnimatorControllerLayer>.Prepare(CommitContext context)
        {
            var layer = new AnimatorControllerLayer
            {
                name = "VX " + Name,
                avatarMask = AvatarMask,
                blendingMode = BlendingMode,
                defaultWeight = DefaultWeight,
                iKPass = IKPass,
                syncedLayerAffectsTiming = SyncedLayerAffectsTiming
            };

            context.RegisterVirtualLayerMapping(this, VirtualLayerIndex);

            return layer;
        }

        void ICommitable<AnimatorControllerLayer>.Commit(CommitContext context, AnimatorControllerLayer obj)
        {
            obj.syncedLayerIndex = context.VirtualToPhysicalLayerIndex(SyncedLayerIndex);
            obj.stateMachine = context.CommitObject(StateMachine);
        }

        public void Dispose()
        {
            // no-op
        }

        public override string ToString()
        {
            return $"VirtualLayer[{VirtualLayerIndex}]: {Name}";
        }
    }
}