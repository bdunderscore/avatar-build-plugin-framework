#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
#if NDMF_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
#endif

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

        public CloneContext CloneContext =>
            _cloneContext ?? throw new InvalidOperationException("Extension context not initialized");

        public void OnActivate(BuildContext context)
        {
            var root = context.AvatarRootObject;

            #if NDMF_VRCSDK3_AVATARS
            if (root.TryGetComponent<VRCAvatarDescriptor>(out _))
            {
                _platformBindings = new VRChatPlatformAnimatorBindings();
            }
            else
            {
                _platformBindings = new GenericPlatformAnimatorBindings();
            }
            #else
            _platformBindings = new GenericPlatformAnimatorBindings();
            #endif

            _cloneContext = new CloneContext(_platformBindings);

            foreach (var (type, controller, _) in _platformBindings.GetInnateControllers(root))
            {
                _layerStates[type] = new LayerState(controller);

                // Force all layers to be processed, for now. This avoids compatibility issues with NDMF
                // plugins which assume that all layers have been cloned after MA runs.
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
                    using var _ = _cloneContext!.PushActiveInnateKey(key);
                    
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

            var commitContext = new CommitContext(_cloneContext!.PlatformBindings);

            var controllers = _layerStates.Where(kvp => kvp.Value.VirtualController != null)
                .ToDictionary(
                    kv => kv.Key,
                    kv =>
                    {
                        commitContext.ActiveInnateLayerKey = kv.Key;
                        return (RuntimeAnimatorController)commitContext.CommitObject(kv.Value.VirtualController!);
                    });

            _platformBindings!.CommitInnateControllers(root, controllers);
        }
    }
}