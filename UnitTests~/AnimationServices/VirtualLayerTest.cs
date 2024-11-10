using System;
using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor.Animations;

namespace UnitTests.AnimationServices
{
    public class VirtualLayerTest : TestBase
    {
        private void AssertPreserveProperty(
            Action<AnimatorControllerLayer> setup,
            Action<VirtualLayer> setupViaVirtualState,
            Action<AnimatorControllerLayer> assert,
            Action<VirtualLayer> assertViaVirtualState
        )
        {
            var layer = new AnimatorControllerLayer();
            setup(layer);

            var cloneContext = new CloneContext(new GenericPlatformAnimatorBindings());

            var virtVal = cloneContext.Clone(layer, 0);
            assertViaVirtualState(virtVal);

            var commitContext = new CommitContext();
            var committed = commitContext.CommitObject(virtVal);
            Assert.AreNotEqual(layer, committed);
            assert(committed);

            layer = new AnimatorControllerLayer();

            virtVal = cloneContext.Clone(layer, 0);
            using (new AssertInvalidate(virtVal))
            {
                setupViaVirtualState(virtVal);
            }

            committed = commitContext.CommitObject(virtVal);
            
            assert(committed);

            commitContext.DestroyAllImmediate();
        }
        
        [Test]
        public void PreservesName()
        {
            AssertPreserveProperty(
                state => state.name = "Test",
                virtualState => virtualState.Name = "Test",
                state => Assert.AreEqual("Test", state.name),
                virtualState => Assert.AreEqual("Test", virtualState.Name)
            );
        }
        
        // TODO: AvatarMask
        
        [Test]
        public void PreservesBlendingMode()
        {
            AssertPreserveProperty(
                state => state.blendingMode = AnimatorLayerBlendingMode.Override,
                virtualState => virtualState.BlendingMode = AnimatorLayerBlendingMode.Override,
                state => Assert.AreEqual(AnimatorLayerBlendingMode.Override, state.blendingMode),
                virtualState => Assert.AreEqual(AnimatorLayerBlendingMode.Override, virtualState.BlendingMode)
            );
        }
        
        [Test]
        public void PreservesDefaultWeight()
        {
            AssertPreserveProperty(
                state => state.defaultWeight = 0.5f,
                virtualState => virtualState.DefaultWeight = 0.5f,
                state => Assert.AreEqual(0.5f, state.defaultWeight),
                virtualState => Assert.AreEqual(0.5f, virtualState.DefaultWeight)
            );
        }
        
        [Test]
        public void PreservesIKPass()
        {
            AssertPreserveProperty(
                state => state.iKPass = true,
                virtualState => virtualState.IKPass = true,
                state => Assert.AreEqual(true, state.iKPass),
                virtualState => Assert.AreEqual(true, virtualState.IKPass)
            );
        }
        
        [Test]
        public void DoesNotPreserveSyncedLayerIndex()
        {
            // We'll demonstrate preservation of this in the VirtualAnimatorControllerTest
            AssertPreserveProperty(
                state => state.syncedLayerIndex = 123,
                virtualState => virtualState.SyncedLayerIndex = 123,
                state => Assert.AreEqual(-1, state.syncedLayerIndex),
                virtualState => Assert.AreEqual(-1, virtualState.SyncedLayerIndex)
            );
        }
        
        [Test]
        public void PreservesSyncedLayerAffectsTiming()
        {
            AssertPreserveProperty(
                state => state.syncedLayerAffectsTiming = true,
                virtualState => virtualState.SyncedLayerAffectsTiming = true,
                state => Assert.AreEqual(true, state.syncedLayerAffectsTiming),
                virtualState => Assert.AreEqual(true, virtualState.SyncedLayerAffectsTiming)
            );
        }

        [Test]
        public void PreservesStateMachine()
        {
            AssertPreserveProperty(
                state => state.stateMachine = TrackObject(new AnimatorStateMachine() { name = "x" }),
                state => state.StateMachine = VirtualStateMachine.Create(new CloneContext(new GenericPlatformAnimatorBindings()), "x"),
                state => Assert.AreEqual("x", state.stateMachine.name),
                state => Assert.AreEqual("x", state.StateMachine.Name)
            );
        }
    }
}