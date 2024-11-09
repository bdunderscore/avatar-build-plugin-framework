using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    public class VirtualTransition : ICommitable<AnimatorTransitionBase>, IDisposable
    {
        private AnimatorTransitionBase _transition;

        private AnimatorStateTransition _stateTransition
        {
            get
            {
                if (_transition is AnimatorStateTransition ast) return ast;
                throw new InvalidOperationException("Transition is not an AnimatorStateTransition");
            }
        }

        public static VirtualTransition Clone(
            CloneContext context,
            AnimatorTransitionBase transition
        )
        {
            if (transition == null) return null;

            if (context.TryGetValue(transition, out VirtualTransition clone)) return clone;

            var cloned = Object.Instantiate(transition);
            cloned.name = transition.name;
            
            return new VirtualTransition(context, cloned);
        }
        
        private VirtualTransition(CloneContext context, AnimatorTransitionBase cloned)
        {
            _transition = cloned;

            context.DeferCall(() =>
            {
                if (cloned.destinationState != null)
                {
                    SetDestination(context.Clone(cloned.destinationState));
                }
                else if (cloned.destinationStateMachine != null)
                {
                    // SetDestination(context.Clone(cloned.destinationStateMachine));
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
            set => _transition.name = value;
        }
        
        // AnimatorStateTransition
        public bool CanTransitionToSelf
        {
            get => _stateTransition.canTransitionToSelf;
            set => _stateTransition.canTransitionToSelf = value;
        }

        public float Duration
        {
            get => _stateTransition.duration;
            set => _stateTransition.duration = value;
        }

        public float? ExitTime
        {
            get => _stateTransition.hasExitTime ? _stateTransition.exitTime : null;
            set
            {
                _stateTransition.hasExitTime = value.HasValue;
                _stateTransition.exitTime = value ?? 0;
            }
        }

        public bool HasFixedDuration
        {
            get => _stateTransition.hasFixedDuration;
            set => _stateTransition.hasFixedDuration = value;
        }

        public TransitionInterruptionSource InterruptionSource
        {
            get => _stateTransition.interruptionSource;
            set => _stateTransition.interruptionSource = value;
        }

        public float Offset
        {
            get => _stateTransition.offset;
            set => _stateTransition.offset = value;
        }

        public bool OrderedInterruption
        {
            get => _stateTransition.orderedInterruption;
            set => _stateTransition.orderedInterruption = value;
        }

        // AnimatorTransitionBase

        private List<AnimatorCondition> _conditions;

        public List<AnimatorCondition> Conditions
        {
            get
            {
                _conditions ??= new List<AnimatorCondition>(_transition.conditions);
                return _conditions;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _conditions = value;
            }
        }

        public void SetDestination(VirtualState state)
        {
            DestinationState = state;
            DestinationStateMachine = null;
            _stateTransition.isExit = false;
        }

        public void SetDestination(VirtualStateMachine stateMachine)
        {
            DestinationState = null;
            DestinationStateMachine = stateMachine;
            _stateTransition.isExit = false;
        }

        public void SetExitDestination()
        {
            DestinationState = null;
            DestinationStateMachine = null;
            _stateTransition.isExit = true;
        }

        public VirtualState DestinationState { get; private set; }
        public VirtualStateMachine DestinationStateMachine { get; private set; }

        public bool IsExit => _stateTransition.isExit;

        public bool Mute
        {
            get => _transition.mute;
            set => _transition.mute = value;
        }

        public bool Solo
        {
            get => _transition.solo;
            set => _transition.solo = value;
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
    }
}