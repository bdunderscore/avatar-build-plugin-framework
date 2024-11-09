using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    public class CloneContext
    {
        public IPlatformAnimatorBindings PlatformBindings { get; private set; }
        private readonly Dictionary<object, IDisposable> _clones = new();

        private int _cloneDepth;
        private readonly Queue<Action> _deferredCalls = new();

        public CloneContext(IPlatformAnimatorBindings platformBindings)
        {
            PlatformBindings = platformBindings;
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
                Add(key, value);
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

        public VirtualStateMachine Clone(AnimatorStateMachine stateMachine)
        {
            return GetOrClone(stateMachine, VirtualStateMachine.Clone);
        }

        public VirtualStateTransition Clone(AnimatorStateTransition transition)
        {
            return GetOrClone(transition, VirtualStateTransition.Clone);
        }

        public VirtualTransition Clone(AnimatorTransition transition)
        {
            return GetOrClone(transition, VirtualTransition.Clone);
        }

        public VirtualState Clone(AnimatorState state)
        {
            return GetOrClone(state, VirtualState.Clone);
        }

        public VirtualMotion Clone(Motion m)
        {
            return GetOrClone(m, VirtualMotion.Clone);
        }

        public VirtualClip Clone(AnimationClip clip)
        {
            return GetOrClone(clip, VirtualClip.Clone);
        }

        public void DeferCall(Action action)
        {
            if (_cloneDepth > 0) _deferredCalls.Enqueue(action);
            else action();
        }
    }
}