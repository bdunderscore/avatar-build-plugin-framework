using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    public class VirtualState : ICommitable<AnimatorState>
    {
        private AnimatorState _state;

        public List<StateMachineBehaviour> Behaviours { get; set; }

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
            throw new NotImplementedException();
        }

        void ICommitable<AnimatorState>.Commit(CommitContext context, AnimatorState obj)
        {
            throw new NotImplementedException();
        }
    }
}