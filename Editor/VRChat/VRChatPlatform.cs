#nullable enable

using System;
using System.Reflection;
using HarmonyLib;
using nadena.dev.ndmf.platform;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace nadena.dev.ndmf.vrchat
{
    internal sealed class VRChatPlatform : INDMFPlatformProvider
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            AmbientPlatform.DefaultPlatform = Instance;
        }

        public static INDMFPlatformProvider Instance { get; } = new VRChatPlatform();

        private readonly MethodInfo? VRCSDK_ShowControlPanel;

        private VRChatPlatform()
        {
            VRCSDK_ShowControlPanel = AccessTools.Method(typeof(VRCSdkControlPanel), "ShowControlPanel");
        }

        public string CanonicalName => "ndmf.vrchat.avatar-3.0";
        public string DisplayName => "VRChat";
        public Texture2D Icon => null;
        public Type AvatarRootComponentType => typeof(VRCAvatarDescriptor);

        public bool HasNativeUI => VRCSDK_ShowControlPanel != null;

        public void OpenNativeUI()
        {
            VRCSDK_ShowControlPanel?.Invoke(null, null);
        }
    }

    internal static class VRChatContextExtensions
    {
        public static VRCAvatarDescriptor VRChatAvatarDescriptor(this BuildContext context)
        {
            return context.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
        }
    }
}