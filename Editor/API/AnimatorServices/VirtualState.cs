using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    public class VirtualState : ICommitable<AnimatorState>, IDisposable
    {
        private AnimatorState _state;

        public List<StateMachineBehaviour> Behaviours { get; set; }

        public static VirtualState Clone(
            CloneContext context,
            AnimatorState state
        )
        {
            if (state == null) return null;

            if (context.TryGetValue(state, out VirtualState clone)) return clone;

            var clonedState = new AnimatorState();
            // We can't use Instantiate for AnimatorStates, for some reason...
            EditorUtility.CopySerialized(state, clonedState);

            return new VirtualState(context, clonedState);
        }

        private VirtualState(CloneContext context, AnimatorState clonedState)
        {
            _state = clonedState;

            // TODO: Should we rewrite any internal properties of these StateMachineBehaviours?
            Behaviours = _state.behaviours.Select(b => Object.Instantiate(b)).ToList();
            context.DeferCall(() => { Transitions = _state.transitions.Select(context.Clone).ToList(); });
            Motion = context.Clone(_state.motion);
        }

        public VirtualMotion Motion { get; set; }

        public string Name
        {
            get => _state.name;
            set => _state.name = value;
        }
        
        public float CycleOffset
        {
            get => _state.cycleOffset;
            set => _state.cycleOffset = value;
        }

        [CanBeNull]
        public string CycleOffsetParameter
        {
            get => _state.cycleOffsetParameterActive ? _state.cycleOffsetParameter : null;
            set
            {
                _state.cycleOffsetParameterActive = value != null;
                _state.cycleOffsetParameter = value ?? "";
            }
        }

        public bool IKOnFeet
        {
            get => _state.iKOnFeet;
            set => _state.iKOnFeet = value;
        }

        public bool Mirror
        {
            get => _state.mirror;
            set => _state.mirror = value;
        }

        [CanBeNull]
        public string MirrorParameter
        {
            get => _state.mirrorParameterActive ? _state.mirrorParameter : null;
            set
            {
                _state.mirrorParameterActive = value != null;
                _state.mirrorParameter = value ?? "";
            }
        }

        // public VirtualMotion Motion;

        public float Speed
        {
            get => _state.speed;
            set => _state.speed = value;
        }

        [CanBeNull]
        public string SpeedParameter
        {
            get => _state.speedParameterActive ? _state.speedParameter : null;
            set
            {
                _state.speedParameterActive = value != null;
                _state.speedParameter = value ?? "";
            }
        }

        public string Tag
        {
            get => _state.tag;
            set => _state.tag = value;
        }

        [CanBeNull]
        public string TimeParameter
        {
            get => _state.timeParameterActive ? _state.timeParameter : null;
            set
            {
                _state.timeParameterActive = value != null;
                _state.timeParameter = value ?? "";
            }
        }

        public List<VirtualTransition> Transitions { get; set; }

        public bool WriteDefaultValues
        {
            get => _state.writeDefaultValues;
            set => _state.writeDefaultValues = value;
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

            _state = null;
        }
        
        void IDisposable.Dispose()
        {
            foreach (var behaviour in Behaviours)
            {
                Object.DestroyImmediate(behaviour);
            }
            
            Behaviours.Clear();
            
            if (_state != null) Object.DestroyImmediate(_state);
            _state = null;
        }
    }
}