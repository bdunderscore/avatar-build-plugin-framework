#region

using System;
using nadena.dev.ndmf.runtime;
using UnityEngine;
#if NDMF_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
#endif

#endregion

namespace nadena.dev.ndmf
{
#if NDMF_VRCSDK3_AVATARS

    public sealed partial class BuildContext
    {
        private VRCAvatarDescriptor _avatarDescriptor;

        /// <summary>
        /// The VRChat avatar descriptor for the avatar being built.
        /// Obsolete: Platform-specific properties on BuildContext are deprecated.
        /// Please use the VRChatAvatarDescriptor() extension method instead.
        /// </summary>
        [Obsolete]
        public VRCAvatarDescriptor AvatarDescriptor => _avatarDescriptor;

        public BuildContext(VRCAvatarDescriptor obj, string assetRootPath)
            : this(obj.gameObject, assetRootPath)
        {
        }


        private void PlatformInit()
        {
            _avatarDescriptor = _avatarRootObject.GetComponent<VRCAvatarDescriptor>();
        }
    }

#endif

    internal static class PlatformExtensions
    {
        [Obsolete("Please use RuntimeUtil.FindAvatarInParents() instead.")]
        public static Transform FindAvatarInParents(Transform target)
        {
            return RuntimeUtil.FindAvatarInParents(target);
        }

        public static bool CanProcessObject(GameObject avatar)
        {
            return avatar != null && RuntimeUtil.IsAvatarRoot(avatar.transform);
        }
    }
}