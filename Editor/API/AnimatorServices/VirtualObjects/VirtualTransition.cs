#nullable enable

using JetBrains.Annotations;
using UnityEditor.Animations;

namespace nadena.dev.ndmf.animator
{
    [PublicAPI]
    public sealed class VirtualTransition : VirtualTransitionBase
    {
        internal VirtualTransition(CloneContext context, AnimatorTransitionBase cloned) : base(context, cloned)
        {
        }

        public static VirtualTransition Create()
        {
            return new VirtualTransition(null, new AnimatorTransition());
        }

        internal static VirtualTransition Clone(
            CloneContext context,
            AnimatorTransition transition
        )
        {
            return (VirtualTransition)CloneInternal(context, transition);
        }
    }
}