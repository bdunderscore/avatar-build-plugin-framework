#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     A layer within a VirtualAnimatorController
    /// </summary>
    [PublicAPI]
    public class VirtualLayer : VirtualNode, ICommitable<AnimatorControllerLayer>, IDisposable
    {
        /// <summary>
        ///     Returns a "virtual layer index" which can be used to map to the actual layer index in the animator controller,
        ///     even if layer order changes. This will typically be a very large value (>2^16).
        /// </summary>
        public int VirtualLayerIndex { get; }
        
        private VirtualStateMachine _stateMachine;

        public VirtualStateMachine StateMachine
        {
            get => _stateMachine;
            set => _stateMachine = I(value);
        }

        private AvatarMask? _avatarMask;

        public AvatarMask? AvatarMask
        {
            get => _avatarMask;
            set => _avatarMask = I(value);
        }

        private AnimatorLayerBlendingMode _blendingMode;

        public AnimatorLayerBlendingMode BlendingMode
        {
            get => _blendingMode;
            set => _blendingMode = I(value);
        }

        private float _defaultWeight;

        public float DefaultWeight
        {
            get => _defaultWeight;
            set => _defaultWeight = I(value);
        }

        private bool _ikPass;

        public bool IKPass
        {
            get => _ikPass;
            set => _ikPass = I(value);
        }

        private string _name;

        public string Name
        {
            get => _name;
            set => _name = I(value);
        }

        private bool _syncedLayerAffectsTiming;

        public bool SyncedLayerAffectsTiming
        {
            get => _syncedLayerAffectsTiming;
            set => _syncedLayerAffectsTiming = I(value);
        }

        private int _syncedLayerIndex;

        public int SyncedLayerIndex
        {
            get => _syncedLayerIndex;
            set => _syncedLayerIndex = I(value);
        }

        private bool _isOriginalLayer;

        public bool IsOriginalLayer
        {
            get => _isOriginalLayer;
            set => _isOriginalLayer = I(value);
        }

        private ImmutableDictionary<VirtualState, VirtualMotion> _syncedLayerMotionOverrides;

        public ImmutableDictionary<VirtualState, VirtualMotion> SyncedLayerMotionOverrides
        {
            get => _syncedLayerMotionOverrides;
            set => _syncedLayerMotionOverrides = I(value);
        }

        private ImmutableDictionary<VirtualState, ImmutableList<StateMachineBehaviour>> _syncedLayerBehaviourOverrides;

        public ImmutableDictionary<VirtualState, ImmutableList<StateMachineBehaviour>> SyncedLayerBehaviourOverrides
        {
            get => _syncedLayerBehaviourOverrides;
            set => _syncedLayerBehaviourOverrides = I(value);
        }

        internal static VirtualLayer Clone(CloneContext context, AnimatorControllerLayer layer, int physicalLayerIndex)
        {
            var clone = new VirtualLayer(context, layer, physicalLayerIndex);

            return clone;
        }

        public static VirtualLayer Create(CloneContext context, string name = "(unnamed)")
        {
            return new VirtualLayer(context, name);
        }

        private VirtualLayer(CloneContext context, AnimatorControllerLayer layer, int physicalLayerIndex)
        {
            VirtualLayerIndex = context.CloneSourceToVirtualLayerIndex(physicalLayerIndex);
            _name = layer.name;
            AvatarMask = layer.avatarMask == null ? null : Object.Instantiate(layer.avatarMask);
            BlendingMode = layer.blendingMode;
            DefaultWeight = layer.defaultWeight;
            IKPass = layer.iKPass;
            SyncedLayerAffectsTiming = layer.syncedLayerAffectsTiming;
            SyncedLayerIndex = context.CloneSourceToVirtualLayerIndex(layer.syncedLayerIndex);

            _stateMachine = context.Clone(layer.stateMachine);

            _syncedLayerMotionOverrides = SyncedLayerOverrideAccess.ExtractStateMotionPairs(layer)
                                              ?.ToImmutableDictionary(kvp => context.Clone(kvp.Key),
                                                  kvp => context.Clone(kvp.Value))
                                          ?? ImmutableDictionary<VirtualState, VirtualMotion>.Empty;

            // TODO: Apply state behavior import processing
            _syncedLayerBehaviourOverrides = SyncedLayerOverrideAccess.ExtractStateBehaviourPairs(layer)
                ?.ToImmutableDictionary(kvp => context.Clone(kvp.Key),
                    kvp => kvp.Value.Cast<StateMachineBehaviour>().ToImmutableList())
                                             ?? ImmutableDictionary<VirtualState, ImmutableList<StateMachineBehaviour>>
                                                 .Empty;
        }

        private VirtualLayer(CloneContext context, string name)
        {
            VirtualLayerIndex = context.AllocateSingleVirtualLayer();
            _name = name;
            AvatarMask = null;
            BlendingMode = AnimatorLayerBlendingMode.Override;
            DefaultWeight = 1;
            IKPass = false;
            SyncedLayerAffectsTiming = false;
            SyncedLayerIndex = -1;

            _stateMachine = VirtualStateMachine.Create(context, name);
            _syncedLayerMotionOverrides = ImmutableDictionary<VirtualState, VirtualMotion>.Empty;
            _syncedLayerBehaviourOverrides =
                ImmutableDictionary<VirtualState, ImmutableList<StateMachineBehaviour>>.Empty;
        }

        AnimatorControllerLayer ICommitable<AnimatorControllerLayer>.Prepare(CommitContext context)
        {
            var layer = new AnimatorControllerLayer
            {
                name = Name,
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

            SyncedLayerOverrideAccess.SetStateMotionPairs(obj, SyncedLayerMotionOverrides.Select(kvp =>
                new KeyValuePair<AnimatorState, Motion>(
                    context.CommitObject(kvp.Key),
                    context.CommitObject(kvp.Value)
                )));

            // TODO: commit state behaviours
            SyncedLayerOverrideAccess.SetStateBehaviourPairs(obj, SyncedLayerBehaviourOverrides.Select(kvp =>
                new KeyValuePair<AnimatorState, ScriptableObject[]>(
                    context.CommitObject(kvp.Key),
                    kvp.Value.Cast<ScriptableObject>().ToArray()
                )));
        }

        public void Dispose()
        {
            // no-op
        }

        public override string ToString()
        {
            return $"VirtualLayer[{VirtualLayerIndex}]: {Name}";
        }

        protected override IEnumerable<VirtualNode> _EnumerateChildren()
        {
            yield return StateMachine;
        }
    }
}