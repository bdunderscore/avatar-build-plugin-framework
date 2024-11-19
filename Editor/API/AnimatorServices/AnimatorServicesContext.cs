#nullable enable

using System;
using JetBrains.Annotations;

namespace nadena.dev.ndmf.animator
{
    /// <summary>
    ///     Provides a number of NDMF services based on virtualizing animator controllers.
    ///     While this context is active, NDMF will automatically track object renames, and apply them to all known
    ///     animators. It will also keep animators cached in the VirtualControllerContext (which is also, as a convenience,
    ///     available through this class).
    ///     Note that any new objects created should be registered in the ObjectPathRemapper if they'll be used in animations;
    ///     this ensures that subsequent movements will be tracked properly. Likewise, use ObjectPathRemapper to obtain
    ///     (virtual) object paths for newly created objects.
    /// </summary>
    [DependsOnContext(typeof(VirtualControllerContext))]
    [PublicAPI]
    public sealed class AnimatorServicesContext : IExtensionContext
    {
        private VirtualControllerContext? _controllerContext;

        public VirtualControllerContext ControllerContext => _controllerContext ??
                                                             throw new InvalidOperationException(
                                                                 "ControllerContext is not available outside of the AnimatorServicesContext");

        private AnimationIndex? _animationIndex;

        public AnimationIndex AnimationIndex => _animationIndex ??
                                                throw new InvalidOperationException(
                                                    "AnimationIndex is not available outside of the AnimatorServicesContext");

        private ObjectPathRemapper? _objectPathRemapper;

        public ObjectPathRemapper ObjectPathRemapper => _objectPathRemapper ??
                                                        throw new InvalidOperationException(
                                                            "ObjectPathRemapper is not available outside of the AnimatorServicesContext");

        public void OnActivate(BuildContext context)
        {
            _controllerContext = context.Extension<VirtualControllerContext>();
            _animationIndex = new AnimationIndex(
                () => ControllerContext.GetAllControllers(),
                () => ControllerContext.CacheInvalidationToken
            );
            _objectPathRemapper = new ObjectPathRemapper(context.AvatarRootTransform);
        }

        public void OnDeactivate(BuildContext context)
        {
            AnimationIndex.RewritePaths(ObjectPathRemapper.GetVirtualToRealPathMap());

            _objectPathRemapper = null;
            _animationIndex = null;
            _controllerContext = null;
        }
    }
}