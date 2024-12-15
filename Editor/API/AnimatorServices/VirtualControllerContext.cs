#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;
#if NDMF_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
#endif

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     This extension context converts all "innate" animator controllers bound to the avatar into virtual controllers.
    ///     By "innate", we mean controllers which are understood by the underlying platform (e.g. VRChat). As part of this,
    ///     the controllers and animations are cloned, so that changes to them do not affect the original assets.
    ///     This context acts as a key-value map from arbitrary context keys to virtual controllers. For VRChat controllers,
    ///     the context will be a `VRCAvatarDescriptor.AnimLayerType` enum value. For other sub-components which have
    ///     animator controllers (e.g. the unity `Animator` component), the context key will be that controller. Otherwise,
    ///     it's up to the IPlatformAnimatorBindings implementation to define the context key.
    ///     Upon deactivation, any changes to virtual controllers will be written back to their sources.
    ///     You may also add arbitrary virtual controllers to the context, by setting the value for a given key. These
    ///     controllers will be generally ignored, unless you choose to do something with them. Note that these controllers
    ///     will not be preserved across a context deactivation.
    ///     ## Limitations
    ///     When this context is active, you must not modify the original controllers or their animations. This is because
    ///     certain virtual objects may reference the original assets, and thus changes to them may result in undefined
    ///     behavior.
    ///     After deactivating this context, you must not modify the virtual controllers or their animations. This is because
    ///     subsequent NDMF processing steps may directly modify the serialized animator controllers; conversely, when the
    ///     virtual controller context is reactivated, it may or may not reuse the same virtual nodes as before.
    /// </summary>
    [PublicAPI]
    public sealed class VirtualControllerContext : IExtensionContext
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

        /// <summary>
        ///     This value is updated every time the set of virtual controllers changes.
        /// </summary>
        public long CacheInvalidationToken { get; private set; }
        
        public void OnActivate(BuildContext context)
        {
            var root = context.AvatarRootObject;

            #if NDMF_VRCSDK3_AVATARS
            if (root.TryGetComponent<VRCAvatarDescriptor>(out _))
            {
                _platformBindings = VRChatPlatformAnimatorBindings.Instance;
            }
            else
            {
                _platformBindings = GenericPlatformAnimatorBindings.Instance;
            }
            #else
            _platformBindings = GenericPlatformAnimatorBindings.Instance;
            #endif

            _cloneContext = new CloneContext(_platformBindings);

            var innateControllers = _platformBindings.GetInnateControllers(root);
            _layerStates.Clear(); // TODO - retain and reactivate virtual controllers
            CacheInvalidationToken++;

            foreach (var (type, controller, _) in innateControllers)
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
            set
            {
                CacheInvalidationToken++;
                _layerStates[key] = new LayerState(null)
                {
                    VirtualController = value
                };
            }
        }

        /// <summary>
        ///     "Forgets" a specific controller. This should usually only be done for controllers which are no longer
        ///     relevant, e.g. if the corresponding component has been removed.
        /// </summary>
        /// <param name="key"></param>
        public void ForgetController(object key)
        {
            CacheInvalidationToken++;
            _layerStates.Remove(key);
        }

        public void OnDeactivate(BuildContext context)
        {
            var root = context.AvatarRootObject;

            var commitContext = new CommitContext(_cloneContext!.PlatformBindings);

            var controllers = _layerStates
                .Where(kvp => kvp.Value.VirtualController != null)
                .ToDictionary(
                    kv => kv.Key,
                    kv =>
                    {
                        commitContext.ActiveInnateLayerKey = kv.Key;
                        return (RuntimeAnimatorController)commitContext.CommitObject(kv.Value.VirtualController!);
                    });
            
            _platformBindings!.CommitControllers(root, controllers);
        }

        public IEnumerable<VirtualAnimatorController> GetAllControllers()
        {
            return _layerStates.Select(kv => this[kv.Key]).Where(v => v != null)!;
        }


        [return: NotNullIfNotNull("controller")]
        public VirtualAnimatorController? Clone(RuntimeAnimatorController? controller)
        {
            return CloneContext.Clone(controller);
        }

        [return: NotNullIfNotNull("layer")]
        public VirtualLayer? Clone(AnimatorControllerLayer? layer, int index)
        {
            return CloneContext.Clone(layer, index);
        }

        [return: NotNullIfNotNull("stateMachine")]
        public VirtualStateMachine? Clone(AnimatorStateMachine? stateMachine)
        {
            return CloneContext.Clone(stateMachine);
        }

        [return: NotNullIfNotNull("transition")]
        public VirtualStateTransition? Clone(AnimatorStateTransition? transition)
        {
            return CloneContext.Clone(transition);
        }

        [return: NotNullIfNotNull("transition")]
        public VirtualTransition? Clone(AnimatorTransition? transition)
        {
            return CloneContext.Clone(transition);
        }

        [return: NotNullIfNotNull("state")]
        public VirtualState? Clone(AnimatorState? state)
        {
            return CloneContext.Clone(state);
        }

        [return: NotNullIfNotNull("m")]
        public VirtualMotion? Clone(Motion? m)
        {
            return CloneContext.Clone(m);
        }

        [return: NotNullIfNotNull("clip")]
        public VirtualClip? Clone(AnimationClip? clip)
        {
            return CloneContext.Clone(clip);
        }
    }
}