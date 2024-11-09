using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     Represents a state machine in a virtual layer.
    /// </summary>
    public class VirtualStateMachine : ICommitable<AnimatorStateMachine>, IDisposable
    {
        private AnimatorStateMachine _stateMachine;

        public static VirtualStateMachine Clone(CloneContext context, AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) return null;
            if (context.TryGetValue(stateMachine, out VirtualStateMachine clone)) return clone;

            var vsm = new VirtualStateMachine();

            context.DeferCall(() =>
            {
                vsm.Name = stateMachine.name;
                vsm.AnyStatePosition = stateMachine.anyStatePosition;
                vsm.AnyStateTransitions = stateMachine.anyStateTransitions
                    .Select(t => VirtualStateTransition.Clone(context, t)).ToList();
                vsm.Behaviours = stateMachine.behaviours.Select(Object.Instantiate).ToList();
                vsm.DefaultState = VirtualState.Clone(context, stateMachine.defaultState);
                vsm.EntryPosition = stateMachine.entryPosition;
                vsm.EntryTransitions = stateMachine.entryTransitions
                    .Select(t => VirtualTransition.Clone(context, t)).ToList();
                vsm.ExitPosition = stateMachine.exitPosition;
                vsm.ParentStateMachinePosition = stateMachine.parentStateMachinePosition;

                vsm.StateMachines = stateMachine.stateMachines.Select(sm => new VirtualChildStateMachine
                {
                    State = Clone(context, sm.stateMachine),
                    Position = sm.position
                }).ToList();

                vsm.States = stateMachine.states.Select(s => new VirtualChildState
                {
                    State = VirtualState.Clone(context, s.state),
                    Position = s.position
                }).ToList();
            });

            return vsm;
        }

        public VirtualStateMachine()
        {
            _stateMachine = new AnimatorStateMachine();
            AnyStatePosition = _stateMachine.anyStatePosition;
            EntryPosition = _stateMachine.entryPosition;
            ExitPosition = _stateMachine.exitPosition;

            EntryTransitions = new List<VirtualTransition>();
            AnyStateTransitions = new List<VirtualStateTransition>();
            Behaviours = new List<StateMachineBehaviour>();
            StateMachines = new List<VirtualChildStateMachine>();
            States = new List<VirtualChildState>();
        }
        
        AnimatorStateMachine ICommitable<AnimatorStateMachine>.Prepare(CommitContext context)
        {
            var tmp = _stateMachine;
            _stateMachine = null;
            return tmp;
        }

        void ICommitable<AnimatorStateMachine>.Commit(CommitContext context, AnimatorStateMachine obj)
        {
            obj.name = Name;
            obj.anyStatePosition = AnyStatePosition;
            obj.anyStateTransitions = AnyStateTransitions.Select(t => (AnimatorStateTransition)context.CommitObject(t))
                .ToArray();
            obj.behaviours = Behaviours.ToArray();
            obj.defaultState = context.CommitObject(DefaultState);
            obj.entryPosition = EntryPosition;
            obj.entryTransitions = EntryTransitions.Select(t => (AnimatorTransition)context.CommitObject(t)).ToArray();
            obj.exitPosition = ExitPosition;
            obj.parentStateMachinePosition = ParentStateMachinePosition;
            obj.stateMachines = StateMachines.Select(sm => new ChildAnimatorStateMachine
            {
                stateMachine = context.CommitObject(sm.State),
                position = sm.Position
            }).ToArray();
            obj.states = States.Select(s => new ChildAnimatorState
            {
                state = context.CommitObject(s.State),
                position = s.Position
            }).ToArray();
        }

        public string Name { get; set; }
        public Vector3 AnyStatePosition { get; set; }
        public Vector3 EntryPosition { get; set; }
        public Vector3 ExitPosition { get; set; }
        public Vector3 ParentStateMachinePosition { get; set; }

        public List<VirtualTransition> EntryTransitions { get; set; }
        public List<VirtualStateTransition> AnyStateTransitions { get; set; }
        public List<StateMachineBehaviour> Behaviours { get; set; }

        public List<VirtualChildStateMachine> StateMachines { get; set; }
        public List<VirtualChildState> States { get; set; }
        public VirtualState DefaultState { get; set; }

        public struct VirtualChildStateMachine
        {
            public VirtualStateMachine State;
            public Vector3 Position;
        }

        public struct VirtualChildState
        {
            public VirtualState State;
            public Vector3 Position;
        }

        public void Dispose()
        {
            if (_stateMachine != null)
            {
                Object.DestroyImmediate(_stateMachine);
                _stateMachine = null;
            }
        }
    }
}