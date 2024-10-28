using UnityEditor.Animations;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     A layer within a VirtualAnimatorController
    /// </summary>
    public class VirtualLayer
    {
        private VirtualStateMachine _stateMachine;

        /// <summary>
        ///     Returns a "virtual layer index" which can be used to map to the actual layer index in the animator controller,
        ///     even if layer order changes. This will typically be a very large value (>2^16).
        /// </summary>
        public int VirtualLayerIndex { get; }

        public AvatarMask AvatarMask { get; set; }
        public AnimatorLayerBlendingMode BlendingMode { get; set; }
        public float DefaultWeight { get; set; }
        public bool IKPass { get; set; }

        public string Name { get; set; }
        // State machine
        // public VirtualStateMachine StateMachine { get; set; }

        public bool SyncedLayerAffectsTiming { get; set; }
        public VirtualLayer SyncedLayer { get; set; }
    }
}