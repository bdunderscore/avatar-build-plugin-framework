using UnityEditor.Animations;

namespace nadena.dev.ndmf.animator
{
    public class VirtualStateTransition : VirtualTransitionBase
    {
        private readonly AnimatorStateTransition _stateTransition;

        internal VirtualStateTransition(CloneContext context, AnimatorStateTransition cloned) : base(context, cloned)
        {
            _stateTransition = cloned;
        }

        public static VirtualStateTransition Clone(
            CloneContext context,
            AnimatorStateTransition transition
        )
        {
            return (VirtualStateTransition)CloneInternal(context, transition);
        }

        // AnimatorStateTransition
        public bool CanTransitionToSelf
        {
            get => _stateTransition.canTransitionToSelf;
            set => _stateTransition.canTransitionToSelf = I(value);
        }

        public float Duration
        {
            get => _stateTransition.duration;
            set => _stateTransition.duration = I(value);
        }

        public float? ExitTime
        {
            get => _stateTransition.hasExitTime ? _stateTransition.exitTime : null;
            set
            {
                Invalidate();
                _stateTransition.hasExitTime = value.HasValue;
                _stateTransition.exitTime = value ?? 0;
            }
        }

        public bool HasFixedDuration
        {
            get => _stateTransition.hasFixedDuration;
            set => _stateTransition.hasFixedDuration = I(value);
        }

        public TransitionInterruptionSource InterruptionSource
        {
            get => _stateTransition.interruptionSource;
            set => _stateTransition.interruptionSource = I(value);
        }

        public float Offset
        {
            get => _stateTransition.offset;
            set => _stateTransition.offset = I(value);
        }

        public bool OrderedInterruption
        {
            get => _stateTransition.orderedInterruption;
            set => _stateTransition.orderedInterruption = I(value);
        }
    }
}