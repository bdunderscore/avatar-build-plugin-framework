﻿using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    public class CloneContext
    {
        public IPlatformAnimatorBindings PlatformBindings { get; private set; }
        private readonly Dictionary<object, IDisposable> _clones = new();

        private int _cloneDepth, _nextVirtualLayer, _virtualLayerBase, _maxMappedPhysLayer;
        private readonly Queue<Action> _deferredCalls = new();

        public CloneContext(IPlatformAnimatorBindings platformBindings)
        {
            PlatformBindings = platformBindings;
            _nextVirtualLayer = _virtualLayerBase = 0x10_0000;
        }

        public bool TryGetValue<T, U>(T key, out U value) where U: IDisposable
        {
            var rv = _clones.TryGetValue(key, out var tmp);

            if (rv) value = (U)tmp;
            else value = default;

            return rv;
        }

        public void Add<T, U>(T key, U value) where U: IDisposable
        {
            _clones.Add(key, value);
        }

        private U GetOrClone<T, U>(T key, Func<CloneContext, T, U> clone) where U : class, IDisposable
        {
            try
            {
                _cloneDepth++;

                if (key == null) return null;
                if (TryGetValue(key, out U value)) return value;
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

        public VirtualAnimatorController Clone(AnimatorController controller)
        {
            using var _ = new ProfilerScope("Clone Animator Controller", controller);
            return GetOrClone(controller, VirtualAnimatorController.Clone);
        }
        
        public VirtualLayer Clone(AnimatorControllerLayer layer, int index)
        {
            using var _ = new ProfilerScope("Clone Animator Layer");
            return GetOrClone(layer, (ctx, obj) => VirtualLayer.Clone(ctx, obj, index));
        }
        
        public VirtualStateMachine Clone(AnimatorStateMachine stateMachine)
        {
            using var _ = new ProfilerScope("Clone State Machine", stateMachine);
            return GetOrClone(stateMachine, VirtualStateMachine.Clone);
        }

        public VirtualStateTransition Clone(AnimatorStateTransition transition)
        {
            using var _ = new ProfilerScope("Clone State Transition", transition);
            return GetOrClone(transition, VirtualStateTransition.Clone);
        }

        public VirtualTransition Clone(AnimatorTransition transition)
        {
            using var _ = new ProfilerScope("Clone Transition", transition);
            return GetOrClone(transition, VirtualTransition.Clone);
        }

        public VirtualState Clone(AnimatorState state)
        {
            using var _ = new ProfilerScope("Clone State", state);
            return GetOrClone(state, VirtualState.Clone);
        }

        public VirtualMotion Clone(Motion m)
        {
            using var _ = new ProfilerScope("Clone Motion", m);
            return GetOrClone(m, VirtualMotion.Clone);
        }

        public VirtualClip Clone(AnimationClip clip)
        {
            using var _ = new ProfilerScope("Clone Clip", clip);
            return GetOrClone(clip, VirtualClip.Clone);
        }

        public void DeferCall(Action action)
        {
            if (_cloneDepth > 0) _deferredCalls.Enqueue(action);
            else action();
        }

        public int CloneSourceToVirtualLayerIndex(int layerSyncedLayerIndex)
        {
            return layerSyncedLayerIndex < _maxMappedPhysLayer && layerSyncedLayerIndex >= 0
                ? layerSyncedLayerIndex + _virtualLayerBase
                : -1;
        }
    }
}