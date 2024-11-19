using System;
using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

namespace UnitTests.AnimationServices
{
    public class VirtualBlendTreeTest : TestBase
    {
        private void AssertPreserveProperty(
            Action<BlendTree> setup,
            Action<VirtualBlendTree> setupViaVirtualState,
            Action<BlendTree> assert,
            Action<VirtualBlendTree> assertViaVirtualState
        )
        {
            var tree = new BlendTree();
            setup(tree);

            var cloneContext = new CloneContext(GenericPlatformAnimatorBindings.Instance);

            var virtTree = (VirtualBlendTree) cloneContext.Clone(tree);
            assertViaVirtualState(virtTree);

            var commitContext = new CommitContext();
            var committed = (BlendTree) commitContext.CommitObject(virtTree);
            Assert.AreNotEqual(tree, committed);
            assert(committed);

            tree = new BlendTree();

            virtTree = (VirtualBlendTree) cloneContext.Clone(tree);
            using (new AssertInvalidate(virtTree))
            {
                setupViaVirtualState(virtTree);
            }

            committed = (BlendTree) commitContext.CommitObject(virtTree);
            
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
        
        [Test]
        public void PreservesBlendParameter()
        {
            AssertPreserveProperty(
                state => state.blendParameter = "Test",
                virtualState => virtualState.BlendParameter = "Test",
                state => Assert.AreEqual("Test", state.blendParameter),
                virtualState => Assert.AreEqual("Test", virtualState.BlendParameter)
            );
        }
        
        [Test]
        public void PreservesBlendParameterY()
        {
            AssertPreserveProperty(
                state => state.blendParameterY = "Test",
                virtualState => virtualState.BlendParameterY = "Test",
                state => Assert.AreEqual("Test", state.blendParameterY),
                virtualState => Assert.AreEqual("Test", virtualState.BlendParameterY)
            );
        }
        
        [Test]
        public void PreservesBlendType()
        {
            AssertPreserveProperty(
                state => state.blendType = BlendTreeType.Simple1D,
                virtualState => virtualState.BlendType = BlendTreeType.Simple1D,
                state => Assert.AreEqual(BlendTreeType.Simple1D, state.blendType),
                virtualState => Assert.AreEqual(BlendTreeType.Simple1D, virtualState.BlendType)
            );
        }
        
        [Test]
        public void PreservesMaxThreshold()
        {
            AssertPreserveProperty(
                state => state.maxThreshold = 0.5f,
                virtualState => virtualState.MaxThreshold = 0.5f,
                state => Assert.AreEqual(0.5f, state.maxThreshold),
                virtualState => Assert.AreEqual(0.5f, virtualState.MaxThreshold)
            );
        }
        
        [Test]
        public void PreservesMinThreshold()
        {
            AssertPreserveProperty(
                state => state.minThreshold = 0.5f,
                virtualState => virtualState.MinThreshold = 0.5f,
                state => Assert.AreEqual(0.5f, state.minThreshold),
                virtualState => Assert.AreEqual(0.5f, virtualState.MinThreshold)
            );
        }
        
        [Test]
        public void PreservesUseAutomaticThresholds()
        {
            AssertPreserveProperty(
                state => state.useAutomaticThresholds = true,
                virtualState => virtualState.UseAutomaticThresholds = true,
                state => Assert.AreEqual(true, state.useAutomaticThresholds),
                virtualState => Assert.AreEqual(true, virtualState.UseAutomaticThresholds)
            );
        }
        
        [Test]
        public void PreservesBlendTreeChildren()
        {
            var tree = TrackObject(new BlendTree());
            tree.useAutomaticThresholds = false;
            tree.children = new[]
            {
                new ChildMotion()
                {
                    motion = TrackObject(new AnimationClip() { name = "1"}),
                    threshold = 0.5f,
                    cycleOffset = 0.25f,
                    directBlendParameter = "Test",
                    mirror = true,
                    position = new Vector2(0.5f, 0.5f),
                    timeScale = 0.9f
                },
                new ChildMotion()
                {
                    motion = TrackObject(new AnimationClip() { name = "2" }),
                    timeScale = 0.1f
                }
            };

            var cloneContext = new CloneContext(GenericPlatformAnimatorBindings.Instance);

            var virtTree = (VirtualBlendTree) cloneContext.Clone(tree);
            Assert.AreEqual(2, virtTree.Children.Count);
            
            virtTree.Children = virtTree.Children.Add(new VirtualBlendTree.VirtualChildMotion()
            {
                Motion = VirtualClip.Create("3")
            });

            var commitContext = new CommitContext();
            var committed = (BlendTree) commitContext.CommitObject(virtTree);
            Assert.AreNotEqual(tree, committed);
            Assert.AreEqual(3, committed.children.Length);
            Assert.AreEqual("1", committed.children[0].motion.name);
            Assert.AreEqual(0.5f, committed.children[0].threshold);
            Assert.AreEqual(0.25f, committed.children[0].cycleOffset);
            Assert.AreEqual("Test", committed.children[0].directBlendParameter);
            Assert.AreEqual(true, committed.children[0].mirror);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), committed.children[0].position);
            Assert.AreEqual(0.9f, committed.children[0].timeScale);
            
            Assert.AreEqual("2", committed.children[1].motion.name);
            Assert.AreEqual("3", committed.children[2].motion.name);
            
            commitContext.DestroyAllImmediate();
        }
    }
}