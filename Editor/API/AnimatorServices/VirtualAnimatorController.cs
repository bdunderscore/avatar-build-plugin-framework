using UnityEditor.Animations;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     Represents an animator controller that has been indexed by NDMF for faster manipulation. This class also
    ///     guarantees that certain assets have been cloned, specifically:
    ///     - AnimatorController
    ///     - StateMachine
    ///     - AnimatorState
    ///     - AnimatorStateTransition
    ///     - BlendTree
    ///     - AnimationClip
    ///     - Any state behaviors attached to the animator controller
    /// </summary>
    public class VirtualAnimatorController
    {
        private BuildContext _context;
        private AnimatorController _controller;
    }
}