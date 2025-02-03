using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     Implement this interface on your component to request that an animator attached to this component be
    ///     automatically registered in the VirtualControllerContext, with the component being the context key.
    ///     This is, for example, implemented by ModularAvatarMergeAnimator to ensure that object renames are tracked
    ///     appropriately at all times.
    /// </summary>
    public interface IVirtualizeAnimatorController
    {
        public RuntimeAnimatorController AnimatorController { get; set; }
    }
}