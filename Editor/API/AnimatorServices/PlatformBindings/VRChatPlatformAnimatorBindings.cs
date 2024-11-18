#if NDMF_VRCSDK3_AVATARS
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace nadena.dev.ndmf.animator
{
    public sealed class VRChatPlatformAnimatorBindings : IPlatformAnimatorBindings
    {
        private const string SAMPLE_PATH_PACKAGE =
            "Packages/com.vrchat.avatars";

        private const string CONTROLLER_PATH_PACKAGE =
            "Packages/com.vrchat.avatars/Samples/AV3 Demo Assets/Animation/Controllers";

        private HashSet<Motion>? _specialMotions;

        private AnimatorController? GetFallbackController(VRCAvatarDescriptor.AnimLayerType ty)
        {
            string name;
            switch (ty)
            {
                case VRCAvatarDescriptor.AnimLayerType.Action:
                    name = "ActionLayer";
                    break;
                case VRCAvatarDescriptor.AnimLayerType.Additive:
                    name = "IdleLayer";
                    break;
                case VRCAvatarDescriptor.AnimLayerType.Base:
                    name = "LocomotionLayer";
                    break;
                case VRCAvatarDescriptor.AnimLayerType.Gesture:
                    name = "HandsLayer";
                    break;
                case VRCAvatarDescriptor.AnimLayerType.Sitting:
                    name = "SittingLayer";
                    break;
                case VRCAvatarDescriptor.AnimLayerType.FX:
                    name = "FaceLayer";
                    break;
                case VRCAvatarDescriptor.AnimLayerType.TPose:
                    name = "UtilityTPose";
                    break;
                case VRCAvatarDescriptor.AnimLayerType.IKPose:
                    name = "UtilityIKPose";
                    break;
                default:
                    name = null;
                    break;
            }

            if (name != null)
            {
                name = "/vrc_AvatarV3" + name + ".controller";

                return AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH_PACKAGE + name);
            }

            return null;
        }
        
        public bool IsSpecialMotion(Motion m)
        {
            if (_specialMotions == null)
            {
                // https://creators.vrchat.com/avatars/#proxy-animations
                _specialMotions = new HashSet<Motion>(
                    AssetDatabase.FindAssets("t:AnimationClip", new[] { SAMPLE_PATH_PACKAGE })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<Motion>)
                        .Where(asset => asset.name.StartsWith("proxy_"))
                );
            }

            return _specialMotions.Contains(m);
        }

        public IEnumerable<(object, RuntimeAnimatorController, bool)> GetInnateControllers(GameObject root)
        {
            if (!root.TryGetComponent<VRCAvatarDescriptor>(out var vrcAvatarDescriptor)) yield break;

            // TODO: Fallback layers
            foreach (var layer in vrcAvatarDescriptor.baseAnimationLayers)
            {
                var ac = layer.isDefault ? null : layer.animatorController;

                // Can't use ?? here as we sometimes have unity-null objects sneaking in...
                if (ac == null) ac = GetFallbackController(layer.type);

                if (ac == null) continue;

                yield return (layer.type, ac, layer.isDefault);
            }

            foreach (var layer in vrcAvatarDescriptor.specialAnimationLayers)
            {
                var ac = layer.isDefault ? null : layer.animatorController;

                // Can't use ?? here as we sometimes have unity-null objects sneaking in...
                if (ac == null) ac = GetFallbackController(layer.type);

                if (ac == null) continue;

                yield return (layer.type, ac, layer.isDefault);
            }
        }

        public void CommitInnateControllers(
            GameObject root,
            IDictionary<object, RuntimeAnimatorController> controllers
        )
        {
            if (!root.TryGetComponent<VRCAvatarDescriptor>(out var vrcAvatarDescriptor)) return;

            EditLayers(vrcAvatarDescriptor.baseAnimationLayers);
            EditLayers(vrcAvatarDescriptor.specialAnimationLayers);

            void EditLayers(VRCAvatarDescriptor.CustomAnimLayer[] layers)
            {
                for (var i = 0; i < layers.Length; i++)
                {
                    if (controllers.TryGetValue(layers[i].type, out var controller))
                    {
                        layers[i].animatorController = controller;
                        layers[i].isDefault = false;
                    }
                }
            }
        }

        public void VirtualizeStateBehaviour(CloneContext context, StateMachineBehaviour behaviour)
        {
            var key = context.ActiveInnateLayerKey;

            switch (behaviour)
            {
                case VRCAnimatorLayerControl alc:
                    // null or equals
                    if (key?.Equals(ConvertLayer(alc.playable)) != false)
                    {
                        alc.layer = context.CloneSourceToVirtualLayerIndex(alc.layer);
                    }

                    break;
            }
        }

        public void CommitStateBehaviour(CommitContext context, StateMachineBehaviour behaviour)
        {
            var key = context.ActiveInnateLayerKey;

            switch (behaviour)
            {
                case VRCAnimatorLayerControl alc:
                    if (key?.Equals(ConvertLayer(alc.playable)) != false)
                    {
                        alc.layer = context.VirtualToPhysicalLayerIndex(alc.layer);
                    }

                    break;
            }
        }

        private object ConvertLayer(VRC_AnimatorLayerControl.BlendableLayer playable)
        {
            switch (playable)
            {
                case VRC_AnimatorLayerControl.BlendableLayer.Action: return VRCAvatarDescriptor.AnimLayerType.Action;
                case VRC_AnimatorLayerControl.BlendableLayer.Additive:
                    return VRCAvatarDescriptor.AnimLayerType.Additive;
                case VRC_AnimatorLayerControl.BlendableLayer.FX: return VRCAvatarDescriptor.AnimLayerType.FX;
                case VRC_AnimatorLayerControl.BlendableLayer.Gesture: return VRCAvatarDescriptor.AnimLayerType.Gesture;
                default: throw new ArgumentOutOfRangeException("Unknown blendable layer type: " + playable);
            }
        }
    }
}
#endif