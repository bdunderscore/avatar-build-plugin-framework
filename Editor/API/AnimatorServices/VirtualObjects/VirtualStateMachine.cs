using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     Represents a state machine in a virtual layer.
    /// </summary>
    public class VirtualStateMachine : VirtualNode, ICommitable<AnimatorStateMachine>, IDisposable
    {
        private readonly CloneContext _context;
        private AnimatorStateMachine _stateMachine;

        public static VirtualStateMachine Clone(CloneContext context, AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) return null;
            if (context.TryGetValue(stateMachine, out VirtualStateMachine clone)) return clone;

            var vsm = new VirtualStateMachine(context, stateMachine.name);

            context.DeferCall(() =>
            {
                vsm.AnyStatePosition = stateMachine.anyStatePosition;
                vsm.AnyStateTransitions = stateMachine.anyStateTransitions
                    .Select(context.Clone).ToImmutableList();
                vsm.Behaviours = stateMachine.behaviours.Select(Object.Instantiate).ToImmutableList();
                vsm.DefaultState = context.Clone(stateMachine.defaultState);
                vsm.EntryPosition = stateMachine.entryPosition;
                vsm.EntryTransitions = stateMachine.entryTransitions
                    .Select(context.Clone).ToImmutableList();
                vsm.ExitPosition = stateMachine.exitPosition;
                vsm.ParentStateMachinePosition = stateMachine.parentStateMachinePosition;

                vsm.StateMachines = stateMachine.stateMachines.Select(sm => new VirtualChildStateMachine
                {
                    StateMachine = context.Clone(sm.stateMachine),
                    Position = sm.position
                }).ToImmutableList();

                vsm.States = stateMachine.states.Select(s => new VirtualChildState
                {
                    State = context.Clone(s.state),
                    Position = s.position
                }).ToImmutableList();
            });

            return vsm;
        }

        public static VirtualStateMachine Create(CloneContext context, string name = "")
        {
            return new VirtualStateMachine(context, name);
        }

        private VirtualStateMachine(CloneContext context, string name = "")
        {
            _context = context;
            _stateMachine = new AnimatorStateMachine();
            Name = name;
            AnyStatePosition = _stateMachine.anyStatePosition;
            EntryPosition = _stateMachine.entryPosition;
            ExitPosition = _stateMachine.exitPosition;

            EntryTransitions = ImmutableList<VirtualTransition>.Empty;
            AnyStateTransitions = ImmutableList<VirtualStateTransition>.Empty;
            Behaviours = ImmutableList<StateMachineBehaviour>.Empty;
            StateMachines = ImmutableList<VirtualChildStateMachine>.Empty;
            States = ImmutableList<VirtualChildState>.Empty;
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

            obj.behaviours = Behaviours.ToArray();
            obj.entryPosition = EntryPosition;
            obj.exitPosition = ExitPosition;
            obj.parentStateMachinePosition = ParentStateMachinePosition;
            obj.stateMachines = StateMachines.Select(sm => new ChildAnimatorStateMachine
            {
                stateMachine = context.CommitObject(sm.StateMachine),
                position = sm.Position
            }).ToArray();
            obj.states = States.Select(s => new ChildAnimatorState
            {
                state = context.CommitObject(s.State),
                position = s.Position
            }).ToArray();

            // Set transitions after registering states/state machines, in case there's some kind of validation happening
            obj.entryTransitions = EntryTransitions.Select(t => (AnimatorTransition)context.CommitObject(t)).ToArray();

            obj.anyStateTransitions = AnyStateTransitions.Select(t => (AnimatorStateTransition)context.CommitObject(t))
                .ToArray();
            // DefaultState will be overwritten if we set it too soon; set it last.
            obj.defaultState = context.CommitObject(DefaultState);
        }

        private string _name;

        public string Name
        {
            get => _name;
            set => _name = I(value);
        }

        private Vector3 _anyStatePosition;

        public Vector3 AnyStatePosition
        {
            get => _anyStatePosition;
            set => _anyStatePosition = I(value);
        }

        private Vector3 _entryPosition;

        public Vector3 EntryPosition
        {
            get => _entryPosition;
            set => _entryPosition = I(value);
        }

        private Vector3 _exitPosition;

        public Vector3 ExitPosition
        {
            get => _exitPosition;
            set => _exitPosition = I(value);
        }

        private Vector3 _parentStateMachinePosition;

        public Vector3 ParentStateMachinePosition
        {
            get => _parentStateMachinePosition;
            set => _parentStateMachinePosition = I(value);
        }

        private ImmutableList<VirtualTransition> _entryTransitions;

        public ImmutableList<VirtualTransition> EntryTransitions
        {
            get => _entryTransitions;
            set => _entryTransitions = I(value);
        }

        private ImmutableList<VirtualStateTransition> _anyStateTransitions;

        public ImmutableList<VirtualStateTransition> AnyStateTransitions
        {
            get => _anyStateTransitions;
            set => _anyStateTransitions = I(value);
        }

        private ImmutableList<StateMachineBehaviour> _behaviours;

        public ImmutableList<StateMachineBehaviour> Behaviours
        {
            get => _behaviours;
            set => _behaviours = I(value);
        }

        private ImmutableList<VirtualChildStateMachine> _stateMachines;

        public ImmutableList<VirtualChildStateMachine> StateMachines
        {
            get => _stateMachines;
            set => _stateMachines = I(value);
        }

        private ImmutableList<VirtualChildState> _states;

        public ImmutableList<VirtualChildState> States
        {
            get => _states;
            set => _states = I(value);
        }

        private VirtualState _defaultState;

        public VirtualState DefaultState
        {
            get => _defaultState;
            set => _defaultState = I(value);
        }

        public struct VirtualChildStateMachine
        {
            public VirtualStateMachine StateMachine;
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

        protected override IEnumerable<VirtualNode> _EnumerateChildren()
        {
            foreach (var sm in StateMachines)
            {
                yield return sm.StateMachine;
            }

            foreach (var state in States)
            {
                yield return state.State;
            }

            if (DefaultState != null) yield return DefaultState;

            foreach (var transition in AnyStateTransitions)
            {
                yield return transition;
            }

            foreach (var transition in EntryTransitions)
            {
                yield return transition;
            }
        }

        public VirtualState AddState(string name, [CanBeNull] VirtualMotion motion = null, Vector3? position = null)
        {
            var state = VirtualState.Create(_context, name);

            state.Motion = motion;
            var childState = new VirtualChildState
            {
                State = state,
                // TODO: Better automatic positioning
                Position = position ?? Vector3.zero
            };

            States = States.Add(childState);

            return state;
        }
    }
}