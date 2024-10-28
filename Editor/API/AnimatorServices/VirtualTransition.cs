using System;
using System.Collections.Generic;
using UnityEditor.Animations;

namespace nadena.dev.ndmf.animator
{
    public class VirtualTransition : ICommitable<AnimatorTransitionBase>
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

        public List<AnimatorCondition> Conditions { get; set; }

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

        public bool ExitIsDestination => _stateTransition.isExit;

        public bool Mute
        {
            get => _stateTransition.mute;
            set => _stateTransition.mute = value;
        }

        public bool Solo
        {
            get => _stateTransition.solo;
            set => _stateTransition.solo = value;
        }

        AnimatorTransitionBase ICommitable<AnimatorTransitionBase>.Prepare(CommitContext context)
        {
            return _transition;
        }

        void ICommitable<AnimatorTransitionBase>.Commit(CommitContext context, AnimatorTransitionBase _)
        {
            if (DestinationState != null)
            {
                _stateTransition.destinationState = context.CommitObject(DestinationState);
            }
            else if (DestinationStateMachine != null)
            {
                _stateTransition.destinationStateMachine = context.CommitObject(DestinationStateMachine);
            }
        }
    }
}