#nullable enable

using System;
using UnityEngine;

namespace nadena.dev.ndmf.platform
{
    // [PublicAPI] - pre-alpha API
    internal interface INDMFPlatformProvider
    {
        string CanonicalName { get; }
        string DisplayName { get; }
        Texture2D? Icon { get; }

        Type? AvatarRootComponentType { get; }

        // IBuildUI? CreateBuildUI();

        bool HasNativeUI => false;

        void OpenNativeUI()
        {
        }

        // CommonAvatarInfo ExtractCommonAvatarInfo(GameObject avatarRoot);

        /// Return true if we can initialize this platform's native config from this common config structure
        // bool CanInitFromCommonAvatarInfo(GameObject avatarRoot, CommonAvatarInfo info);
        /// Initialize this platform's native config from this common config structure (destructive operation)
        // void InitFromCommonAvatarInfo(GameObject avatarRoot, CommonAvatarInfo info);
        [ExcludeFromDocs]
        void ___require_csharp_default_member_support()
        {
        }
    }
}