using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace nadena.dev.ndmf.animator
{
    public sealed class AnimatorServicesContext : IExtensionContext
    {
        private class LayerState
        {
            internal RuntimeAnimatorController originalController;
            internal VirtualAnimatorController virtualController;
        }

        private CloneContext _cloneContext;
        private readonly Dictionary<object, LayerState> _layerStates = new();

        private IPlatformAnimatorBindings _platformBindings;

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

            foreach (var (type, controller, isDefault) in _platformBindings.GetInnateControllers(root))
            {
                _layerStates[type] = new LayerState
                {
                    originalController = controller,
                    virtualController = null
                };

                // TEMP
                _ = this[type];
            }
        }

        public VirtualAnimatorController this[object key]
        {
            get
            {
                if (!_layerStates.TryGetValue(key, out var state))
                {
                    return null;
                }

                if (state.virtualController == null)
                {
                    state.virtualController = _cloneContext.Clone(state.originalController);
                }

                return state.virtualController;
            }
            set => _layerStates[key] = new LayerState
            {
                virtualController = value
            };
        }

        public void OnDeactivate(BuildContext context)
        {
            var root = context.AvatarRootObject;

            var commitContext = new CommitContext();

            var controllers = _layerStates.Where(kvp => kvp.Value.virtualController != null)
                .ToDictionary(
                    k => k.Key,
                    v => (RuntimeAnimatorController)commitContext.CommitObject(v.Value.virtualController)
                );

            _platformBindings.CommitInnateControllers(root, controllers);
        }
    }
}