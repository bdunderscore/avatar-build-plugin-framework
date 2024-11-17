using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEditor.Animations;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    public class VirtualTransitionBase : VirtualNode, ICommitable<AnimatorTransitionBase>, IDisposable
    {
        protected AnimatorTransitionBase _transition;
        private ImmutableList<AnimatorCondition> _conditions;

        internal VirtualTransitionBase(CloneContext context, AnimatorTransitionBase cloned)
        {
            _transition = cloned;

            context?.DeferCall(() =>
            {
                if (cloned.destinationState != null)
                {
                    SetDestination(context.Clone(cloned.destinationState));
                }
                else if (cloned.destinationStateMachine != null)
                {
                    SetDestination(context.Clone(cloned.destinationStateMachine));
                }
                else if (cloned.isExit)
                {
                    SetExitDestination();
                }
            });
        }

        public string Name
        {
            get => _transition.name;
            set => _transition.name = I(value);
        }

        public ImmutableList<AnimatorCondition> Conditions
        {
            get
            {
                _conditions ??= _transition.conditions.ToImmutableList();
                return _conditions;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                Invalidate();
                _conditions = value;
            }
        }

        private VirtualState _destinationState;

        public VirtualState DestinationState
        {
            get => _destinationState;
            private set
            {
                _destinationState = value;
                Invalidate();
            }
        }

        private VirtualStateMachine _destinationStateMachine;

        public VirtualStateMachine DestinationStateMachine
        {
            get => _destinationStateMachine;
            private set
            {
                _destinationStateMachine = value;
                Invalidate();
            }
        }
        public bool IsExit => _transition.isExit;

        public bool Mute
        {
            get => _transition.mute;
            set => _transition.mute = I(value);
        }

        public bool Solo
        {
            get => _transition.solo;
            set => _transition.solo = I(value);
        }

        protected static VirtualTransitionBase CloneInternal(
            CloneContext context,
            AnimatorTransitionBase transition
        )
        {
            if (transition == null) return null;

            if (context.TryGetValue(transition, out VirtualStateTransition clone)) return clone;

            var cloned = Object.Instantiate(transition);
            cloned.name = transition.name;

            switch (cloned)
            {
                case AnimatorStateTransition ast: return new VirtualStateTransition(context, ast);
                default: return new VirtualTransition(context, cloned);
            }
        }

        public void SetDestination(VirtualState state)
        {
            Invalidate();
            DestinationState = state;
            DestinationStateMachine = null;
            _transition.isExit = false;
        }

        public void SetDestination(VirtualStateMachine stateMachine)
        {
            Invalidate();
            DestinationState = null;
            DestinationStateMachine = stateMachine;
            _transition.isExit = false;
        }

        public void SetExitDestination()
        {
            Invalidate();
            DestinationState = null;
            DestinationStateMachine = null;
            _transition.isExit = true;
        }

        AnimatorTransitionBase ICommitable<AnimatorTransitionBase>.Prepare(CommitContext context)
        {
            return _transition;
        }

        void ICommitable<AnimatorTransitionBase>.Commit(CommitContext context, AnimatorTransitionBase obj)
        {
            _transition = null;

            if (DestinationState != null)
            {
                obj.destinationState = context.CommitObject(DestinationState);
                obj.destinationStateMachine = null;
            }
            else if (DestinationStateMachine != null)
            {
                obj.destinationState = null;
                obj.destinationStateMachine = context.CommitObject(DestinationStateMachine);
            }
            else
            {
                obj.destinationState = null;
                obj.destinationStateMachine = null;
            }
        }

        public void Dispose()
        {
            if (_transition != null) Object.DestroyImmediate(_transition);
            _transition = null;
        }

        protected override IEnumerable<VirtualNode> _EnumerateChildren()
        {
            if (DestinationState != null) yield return DestinationState;
            if (DestinationStateMachine != null) yield return DestinationStateMachine;
        }
    }
}