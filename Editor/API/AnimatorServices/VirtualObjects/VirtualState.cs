#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    [PublicAPI]
    public class VirtualState : VirtualNode, ICommitable<AnimatorState>, IDisposable
    {
        private AnimatorState _state;

        private ImmutableList<StateMachineBehaviour> _behaviours = ImmutableList<StateMachineBehaviour>.Empty;

        public ImmutableList<StateMachineBehaviour> Behaviours
        {
            get => _behaviours;
            set => _behaviours = I(value);
        }

        public static VirtualState Clone(
            CloneContext context,
            AnimatorState state
        )
        {
            if (context.TryGetValue(state, out VirtualState? clone)) return clone!;

            var clonedState = new AnimatorState();
            // We can't use Instantiate for AnimatorStates, for some reason...
            EditorUtility.CopySerialized(state, clonedState);

            return new VirtualState(context, clonedState);
        }

        public static VirtualState Create(CloneContext _, string name = "unnamed")
        {
            return new VirtualState { Name = name };
        }

        private VirtualState()
        {
            _state = new AnimatorState();
            Behaviours = ImmutableList<StateMachineBehaviour>.Empty;
            _transitions = ImmutableList<VirtualStateTransition>.Empty;
        }

        private VirtualState(CloneContext context, AnimatorState clonedState)
        {
            _state = clonedState;

            // TODO: Should we rewrite any internal properties of these StateMachineBehaviours?
            Behaviours = _state.behaviours.Select(b => Object.Instantiate(b)).ToImmutableList();

            _transitions = ImmutableList<VirtualStateTransition>.Empty;
            context.DeferCall(() =>
            {
                Transitions = _state.transitions
                    .Where(t => t != null)
                    .Select(context.Clone)
                    .ToImmutableList()!;
            });
            
            Motion = context.Clone(_state.motion);
        }

        private VirtualMotion? _motion;

        public VirtualMotion? Motion
        {
            get => _motion;
            set => _motion = I(value);
        }

        public string Name
        {
            get => _state.name;
            set => _state.name = I(value);
        }
        
        public float CycleOffset
        {
            get => _state.cycleOffset;
            set => _state.cycleOffset = I(value);
        }

        public string? CycleOffsetParameter
        {
            get => _state.cycleOffsetParameterActive ? _state.cycleOffsetParameter : null;
            set
            {
                Invalidate();
                _state.cycleOffsetParameterActive = value != null;
                _state.cycleOffsetParameter = value ?? "";
            }
        }

        public bool IKOnFeet
        {
            get => _state.iKOnFeet;
            set => _state.iKOnFeet = I(value);
        }

        public bool Mirror
        {
            get => _state.mirror;
            set => _state.mirror = I(value);
        }

        public string? MirrorParameter
        {
            get => _state.mirrorParameterActive ? _state.mirrorParameter : null;
            set
            {
                Invalidate();
                _state.mirrorParameterActive = value != null;
                _state.mirrorParameter = value ?? "";
            }
        }

        // public VirtualMotion Motion;

        public float Speed
        {
            get => _state.speed;
            set => _state.speed = I(value);
        }

        public string? SpeedParameter
        {
            get => _state.speedParameterActive ? _state.speedParameter : null;
            set
            {
                Invalidate();
                _state.speedParameterActive = value != null;
                _state.speedParameter = value ?? "";
            }
        }

        public string Tag
        {
            get => _state.tag;
            set => _state.tag = I(value);
        }

        public string? TimeParameter
        {
            get => _state.timeParameterActive ? _state.timeParameter : null;
            set
            {
                Invalidate();
                _state.timeParameterActive = value != null;
                _state.timeParameter = value ?? "";
            }
        }

        private ImmutableList<VirtualStateTransition> _transitions;

        public ImmutableList<VirtualStateTransition> Transitions
        {
            get => _transitions;
            set => _transitions = I(value);
        }

        public bool WriteDefaultValues
        {
            get => _state.writeDefaultValues;
            set => _state.writeDefaultValues = I(value);
        }

        // Helpers

        // AddExitTransition
        // AddStateMachineBehaviour
        // AddTransition
        // RemoveTransition
        
        AnimatorState ICommitable<AnimatorState>.Prepare(CommitContext context)
        {
            return _state;
        }

        void ICommitable<AnimatorState>.Commit(CommitContext context, AnimatorState obj)
        {
            obj.behaviours = Behaviours.ToArray();
            obj.transitions = Transitions.Select(t => (AnimatorStateTransition)context.CommitObject(t)).ToArray();
        }
        
        void IDisposable.Dispose()
        {
            foreach (var behaviour in Behaviours)
            {
                Object.DestroyImmediate(behaviour);
            }

            Behaviours = Behaviours.Clear();
            
            if (_state != null) Object.DestroyImmediate(_state);
        }

        public override string ToString()
        {
            return $"VirtualState({Name})";
        }

        protected override IEnumerable<VirtualNode> _EnumerateChildren()
        {
            if (Motion != null) yield return Motion;
            foreach (var transition in Transitions) yield return transition;
        }
    }
}