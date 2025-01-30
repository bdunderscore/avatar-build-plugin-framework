#nullable enable

using System;
using UnityEngine;

namespace nadena.dev.ndmf.platform
{
    internal sealed class GenericPlatform : INDMFPlatformProvider
    {
        public string CanonicalName => "generic";
        public string DisplayName => "Generic Avatar";
        public Texture2D? Icon => null;
        public Type? AvatarRootComponentType => typeof(Animator);

        public bool HasNativeUI => false;

        public void OpenNativeUI()
        {
            throw new NotImplementedException();
        }
    }
}