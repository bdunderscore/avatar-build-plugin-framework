using System;
using UnityEditor.Animations;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     Represents a state machine in a virtual layer.
    /// </summary>
    public class VirtualStateMachine : ICommitable<AnimatorStateMachine>
    {
        private AnimatorStateMachine _stateMachine;


        AnimatorStateMachine ICommitable<AnimatorStateMachine>.Prepare(CommitContext context)
        {
            throw new NotImplementedException();
        }

        void ICommitable<AnimatorStateMachine>.Commit(CommitContext context, AnimatorStateMachine obj)
        {
            throw new NotImplementedException();
        }
    }
}