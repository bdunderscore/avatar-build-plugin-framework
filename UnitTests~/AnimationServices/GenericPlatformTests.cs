using System;
using System.Collections.Generic;
using nadena.dev.ndmf.animator;
using nadena.dev.ndmf.UnitTestSupport;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

namespace UnitTests.AnimationServices
{
    public class GenericPlatformTests : TestBase
    {
        public static IEnumerable<(string, Func<GenericPlatformTests, GameObject>)> CreateAvatarSource()
        {
            yield return ("Generic", t =>
            {
                var obj = t.TrackObject(new GameObject("test"));
                obj.AddComponent<Animator>();
                return obj;
            });

#if NDMF_VRCSDK3_AVATARS
            yield return ("VRChat", t => t.CreateRoot("VRChat"));
#endif
        }
        
        [Test]
        public void TracksAnimationsForAnimators(
            [ValueSource(nameof(CreateAvatarSource))]
            (string, Func<GenericPlatformTests, GameObject>) createAvatar
        )
        {
            var root = createAvatar.Item2(this);
            var animator = root.GetComponent<Animator>();
            
            var child = TrackObject(new GameObject("child"));
            child.transform.parent = root.transform;
            var childAnimator = child.AddComponent<Animator>();

            var startingController = new AnimatorController();
            childAnimator.runtimeAnimatorController = startingController;

            var buildContext = CreateContext(root);
            var ctx = buildContext.ActivateExtensionContext<VirtualControllerContext>();
            
            Assert.IsNotNull(ctx[childAnimator]);
            Assert.IsNull(ctx[animator]);
            
            buildContext.DeactivateExtensionContext<VirtualControllerContext>();
            
            Assert.AreNotEqual(startingController, childAnimator.runtimeAnimatorController);
            Assert.NotNull(childAnimator.runtimeAnimatorController);
        }

        [Test]
        public void TracksAnimationsForCustomComponents(
            [ValueSource(nameof(CreateAvatarSource))]
            (string, Func<GenericPlatformTests, GameObject>) createAvatar
        )
        {
            var root = createAvatar.Item2(this);
            
            var child = TrackObject(new GameObject("child"));
            child.transform.parent = root.transform;
            var childComponent = child.AddComponent<VirtualizedComponent>();

            var startingController = new AnimatorController();
            childComponent.AnimatorController = startingController;

            var buildContext = CreateContext(root);
            var ctx = buildContext.ActivateExtensionContext<VirtualControllerContext>();
            
            Assert.IsNotNull(ctx[childComponent]);
            
            buildContext.DeactivateExtensionContext<VirtualControllerContext>();
            
            Assert.AreNotEqual(startingController, childComponent.AnimatorController);
        }
    }
}