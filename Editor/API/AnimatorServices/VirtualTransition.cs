using UnityEditor.Animations;

namespace nadena.dev.ndmf.animator
{
    public class VirtualTransition : VirtualTransitionBase
    {
        internal VirtualTransition(CloneContext context, AnimatorTransitionBase cloned) : base(context, cloned)
        {
        }

        public static VirtualTransition Clone(
            CloneContext context,
            AnimatorTransition transition
        )
        {
            return (VirtualTransition)CloneInternal(context, transition);
        }
    }
}