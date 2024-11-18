#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    [PublicAPI]
    public sealed class CloneContext
    {
        public IPlatformAnimatorBindings PlatformBindings { get; private set; }
        private readonly Dictionary<object, object> _clones = new();

        private int _cloneDepth, _nextVirtualLayer, _virtualLayerBase, _maxMappedPhysLayer;
        private readonly Queue<Action> _deferredCalls = new();

        private struct DynamicScopeState
        {
            public ImmutableList<AnimatorOverrideController> OverrideControllers;
            public object? InnateAnimatorKey;
        }

        private DynamicScopeState _curDynScope = new()
        {
            OverrideControllers = ImmutableList<AnimatorOverrideController>.Empty
        };

        private ImmutableList<AnimatorOverrideController> OverrideControllers => _curDynScope.OverrideControllers;

        /// <summary>
        ///     When cloning an innate animator, this property will be set to the key of the animator.
        ///     In the case of VRChat, this contains the layer type while cloning.
        /// </summary>
        public object? ActiveInnateLayerKey => _curDynScope.InnateAnimatorKey;

        public CloneContext(IPlatformAnimatorBindings platformBindings)
        {
            PlatformBindings = platformBindings;
            _nextVirtualLayer = _virtualLayerBase = 0x10_0000;
        }

        private class DynamicScope : IDisposable
        {
            private readonly CloneContext _context;
            private readonly DynamicScopeState _priorStack;

            public DynamicScope(CloneContext context)
            {
                _context = context;
                _priorStack = context._curDynScope;
            }

            public void Dispose()
            {
                _context._curDynScope = _priorStack;
            }
        }

        internal IDisposable PushOverrideController(AnimatorOverrideController controller)
        {
            var scope = new DynamicScope(this);

            _curDynScope.OverrideControllers = _curDynScope.OverrideControllers.Add(controller);

            return scope;
        }

        internal IDisposable PushActiveInnateKey(object key)
        {
            var scope = new DynamicScope(this);

            _curDynScope.InnateAnimatorKey = key;

            return scope;
        }

        /// <summary>
        ///     Applies any in-scope AnimationOverrideControllers to the given motion to get the effective motion.
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        public AnimationClip MapClipOnClone(AnimationClip clip)
        {
            foreach (var controller in OverrideControllers)
            {
                clip = controller[clip];
            }

            return clip;
        }

        internal bool TryGetValue<T, U>(T key, out U? value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            var rv = _clones.TryGetValue(key, out var tmp);

            if (rv) value = (U?)tmp;
            else value = default;

            return rv;
        }

        private U? GetOrClone<T, U>(T? key, Func<CloneContext, T, U> clone) where U : class
        {
            try
            {
                _cloneDepth++;

                if (key == null || (key is Object obj && obj == null)) return null;
                if (TryGetValue(key, out U? value)) return value;
                value = clone(this, key);
                _clones[key] = value;
                return value;
            }
            finally
            {
                if (--_cloneDepth == 0)
                {
                    // Flush deferred actions. Note that deferred actions might spawn other clones, so be careful not
                    // to recurse while flushing.
                    try
                    {
                        _cloneDepth++;

                        while (_deferredCalls.TryDequeue(out var action)) action();
                    }
                    finally
                    {
                        _cloneDepth--;
                    }
                }
            }
        }

        internal int AllocateVirtualLayerSpace(int n)
        {
            var layerStart = _nextVirtualLayer;
            _nextVirtualLayer += n;
            _virtualLayerBase = layerStart;
            _maxMappedPhysLayer = n;

            return layerStart;
        }

        internal int AllocateSingleVirtualLayer()
        {
            return _nextVirtualLayer++;
        }

        internal StateMachineBehaviour ImportBehaviour(StateMachineBehaviour behaviour)
        {
            behaviour = Object.Instantiate(behaviour);
            PlatformBindings.VirtualizeStateBehaviour(this, behaviour);
            return behaviour;
        }

        [return: NotNullIfNotNull("controller")]
        public VirtualAnimatorController? Clone(RuntimeAnimatorController? controller)
        {
            using var _ = new ProfilerScope("Clone Animator Controller", controller);
            return GetOrClone(controller, VirtualAnimatorController.Clone);
        }

        [return: NotNullIfNotNull("layer")]
        public VirtualLayer? Clone(AnimatorControllerLayer? layer, int index)
        {
            using var _ = new ProfilerScope("Clone Animator Layer");
            return GetOrClone(layer, (ctx, obj) => VirtualLayer.Clone(ctx, obj, index));
        }

        [return: NotNullIfNotNull("stateMachine")]
        public VirtualStateMachine? Clone(AnimatorStateMachine? stateMachine)
        {
            using var _ = new ProfilerScope("Clone State Machine", stateMachine);
            return GetOrClone(stateMachine, VirtualStateMachine.Clone);
        }

        [return: NotNullIfNotNull("transition")]
        public VirtualStateTransition? Clone(AnimatorStateTransition? transition)
        {
            using var _ = new ProfilerScope("Clone State Transition", transition);
            return GetOrClone(transition, VirtualStateTransition.Clone);
        }

        [return: NotNullIfNotNull("transition")]
        public VirtualTransition? Clone(AnimatorTransition? transition)
        {
            using var _ = new ProfilerScope("Clone Transition", transition);
            return GetOrClone(transition, VirtualTransition.Clone);
        }

        [return: NotNullIfNotNull("state")]
        public VirtualState? Clone(AnimatorState? state)
        {
            using var _ = new ProfilerScope("Clone State", state);
            return GetOrClone(state, VirtualState.Clone);
        }

        [return: NotNullIfNotNull("m")]
        public VirtualMotion? Clone(Motion? m)
        {
            using var _ = new ProfilerScope("Clone Motion", m);
            return GetOrClone(m, VirtualMotion.Clone);
        }

        [return: NotNullIfNotNull("clip")]
        public VirtualClip? Clone(AnimationClip? clip)
        {
            using var _ = new ProfilerScope("Clone Clip", clip);
            return GetOrClone(clip, VirtualClip.Clone);
        }

        public void DeferCall(Action action)
        {
            var overrideStack = _curDynScope;
            // Preserve ambient AnimatorOverrideController context when we defer calls
            if (_cloneDepth > 0)
                _deferredCalls.Enqueue(() =>
                {
                    using var _ = new DynamicScope(this);
                    _curDynScope = overrideStack;

                    action();
                });
            else action();
        }

        public int CloneSourceToVirtualLayerIndex(int layerIndex)
        {
            return layerIndex < _maxMappedPhysLayer && layerIndex >= 0
                ? layerIndex + _virtualLayerBase
                : -1;
        }
    }
}