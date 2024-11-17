#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace nadena.dev.ndmf.animator
{
    [PublicAPI]
    public sealed class AnimatorServicesContext : IExtensionContext
    {
        private class LayerState
        {
            internal readonly RuntimeAnimatorController? OriginalController;
            internal VirtualAnimatorController? VirtualController;

            public LayerState(RuntimeAnimatorController? originalController)
            {
                OriginalController = originalController;
            }
        }
        
        private readonly Dictionary<object, LayerState> _layerStates = new();

        // initialized on activate
        private IPlatformAnimatorBindings? _platformBindings;
        private CloneContext? _cloneContext;

        public void OnActivate(BuildContext context)
        {
            var root = context.AvatarRootObject;

            if (root.TryGetComponent<VRCAvatarDescriptor>(out _))
            {
                _platformBindings = new VRChatPlatformAnimatorBindings();
            }
            else
            {
                _platformBindings = new GenericPlatformAnimatorBindings();
            }

            _cloneContext = new CloneContext(_platformBindings);

            foreach (var (type, controller, _) in _platformBindings.GetInnateControllers(root))
            {
                _layerStates[type] = new LayerState(controller);

                // TEMP - force all layers to be processed
                _ = this[type];
            }
        }

        public VirtualAnimatorController? this[object key]
        {
            get
            {
                if (!_layerStates.TryGetValue(key, out var state))
                {
                    return null;
                }

                if (state.VirtualController == null)
                {
                    state.VirtualController = _cloneContext!.Clone(state.OriginalController);
                }

                return state.VirtualController;
            }
            set => _layerStates[key] = new LayerState(null)
            {
                VirtualController = value
            };
        }

        public void OnDeactivate(BuildContext context)
        {
            var root = context.AvatarRootObject;

            var commitContext = new CommitContext();

            var controllers = _layerStates.Where(kvp => kvp.Value.VirtualController != null)
                .ToDictionary(
                    k => k.Key,
                    v => (RuntimeAnimatorController)commitContext.CommitObject(v.Value.VirtualController!)
                );

            _platformBindings!.CommitInnateControllers(root, controllers);
        }
    }
}