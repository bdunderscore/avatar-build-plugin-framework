using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace nadena.dev.ndmf.animator
{
    public class VRChatPlatformAnimatorBindings : IPlatformAnimatorBindings
    {
        private const string SAMPLE_PATH_PACKAGE =
            "Packages/com.vrchat.avatars/Samples/AV3 Demo Assets/Animation/Controllers";

        private HashSet<Motion> _specialMotions;

        public bool IsSpecialMotion(Motion m)
        {
            if (_specialMotions == null)
            {
                _specialMotions = new HashSet<Motion>(
                    AssetDatabase.FindAssets("t:AnimationClip", new[] { SAMPLE_PATH_PACKAGE })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<Motion>)
                );
            }

            return _specialMotions.Contains(m);
        }

        public IEnumerable<(object, RuntimeAnimatorController, bool)> GetInnateControllers(GameObject root)
        {
            var vrcAvatarController = root.GetComponent<VRCAvatarDescriptor>();
            if (vrcAvatarController == null) yield break;

            // TODO: Fallback layers
            foreach (var layer in vrcAvatarController.baseAnimationLayers)
            {
                yield return (layer.type, layer.animatorController, layer.isDefault);
            }

            foreach (var layer in vrcAvatarController.specialAnimationLayers)
            {
                yield return (layer.type, layer.animatorController, layer.isDefault);
            }
        }

        public void CommitInnateControllers(
            GameObject root,
            IDictionary<object, RuntimeAnimatorController> controllers
        )
        {
            var vrcAvatarController = root.GetComponent<VRCAvatarDescriptor>();
            if (vrcAvatarController == null) return;

            EditLayers(vrcAvatarController.baseAnimationLayers);
            EditLayers(vrcAvatarController.specialAnimationLayers);

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
    }
}