using System;
using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace UnitTests.AnimationServices
{
    public class VirtualStateTest
    {
        private void AssertPreserveProperty(
            Action<AnimatorState> setup,
            Action<VirtualState> setupViaVirtualState,
            Action<AnimatorState> assert,
            Action<VirtualState> assertViaVirtualState
        )
        {
            var state = new AnimatorState();
            setup(state);

            var cloneContext = new CloneContext(new GenericPlatformAnimatorBindings());

            VirtualState virtualState = cloneContext.Clone(state);
            assertViaVirtualState(virtualState);

            var commitContext = new CommitContext();
            var committed = commitContext.CommitObject(virtualState);
            Assert.AreNotEqual(state, committed);
            assert(committed);
            
            UnityEngine.Object.DestroyImmediate(state);
            UnityEngine.Object.DestroyImmediate(committed);

            state = new AnimatorState();

            virtualState = cloneContext.Clone(state);
            setupViaVirtualState(virtualState);
            
            committed = commitContext.CommitObject(virtualState);
            
            assert(committed);
            
            UnityEngine.Object.DestroyImmediate(state);
            UnityEngine.Object.DestroyImmediate(committed);
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
        public void PreservesCycleOffset()
        {
            AssertPreserveProperty(
                state => state.cycleOffset = 0.5f,
                virtualState => virtualState.CycleOffset = 0.5f,
                state => Assert.AreEqual(0.5f, state.cycleOffset),
                virtualState => Assert.AreEqual(0.5f, virtualState.CycleOffset)
            );
        }
        
        [Test]
        public void PreservesCycleOffsetParameter()
        {
            AssertPreserveProperty(
                state =>
                {
                    state.cycleOffsetParameterActive = true;
                    state.cycleOffsetParameter = "Test";
                },
                virtualState => virtualState.CycleOffsetParameter = "Test",
                state =>
                {
                    Assert.IsTrue(state.cycleOffsetParameterActive);
                    Assert.AreEqual("Test", state.cycleOffsetParameter);
                },
                virtualState =>
                {
                    Assert.AreEqual("Test", virtualState.CycleOffsetParameter);
                }
            );
        }
        
        [Test]
        public void PreservesIKOnFeet()
        {
            AssertPreserveProperty(
                state => state.iKOnFeet = true,
                virtualState => virtualState.IKOnFeet = true,
                state => Assert.IsTrue(state.iKOnFeet),
                virtualState => Assert.IsTrue(virtualState.IKOnFeet)
            );
        }
        
        [Test]
        public void PreservesMirror()
        {
            AssertPreserveProperty(
                state => state.mirror = true,
                virtualState => virtualState.Mirror = true,
                state => Assert.IsTrue(state.mirror),
                virtualState => Assert.IsTrue(virtualState.Mirror)
            );
        }
        
        [Test]
        public void PreservesMirrorParameter()
        {
            AssertPreserveProperty(
                state =>
                {
                    state.mirrorParameterActive = true;
                    state.mirrorParameter = "Test";
                },
                virtualState => virtualState.MirrorParameter = "Test",
                state =>
                {
                    Assert.IsTrue(state.mirrorParameterActive);
                    Assert.AreEqual("Test", state.mirrorParameter);
                },
                virtualState =>
                {
                    Assert.AreEqual("Test", virtualState.MirrorParameter);
                }
            );
        }
        
        [Test]
        public void PreservesSpeed()
        {
            AssertPreserveProperty(
                state => state.speed = 0.5f,
                virtualState => virtualState.Speed = 0.5f,
                state => Assert.AreEqual(0.5f, state.speed),
                virtualState => Assert.AreEqual(0.5f, virtualState.Speed)
            );
        }
        
        [Test]
        public void PreservesSpeedParameter()
        {
            AssertPreserveProperty(
                state =>
                {
                    state.speedParameterActive = true;
                    state.speedParameter = "Test";
                },
                virtualState => virtualState.SpeedParameter = "Test",
                state =>
                {
                    Assert.IsTrue(state.speedParameterActive);
                    Assert.AreEqual("Test", state.speedParameter);
                },
                virtualState =>
                {
                    Assert.AreEqual("Test", virtualState.SpeedParameter);
                }
            );
        }
        
        [Test]
        public void PreservesTag()
        {
            AssertPreserveProperty(
                state => state.tag = "Test",
                virtualState => virtualState.Tag = "Test",
                state => Assert.AreEqual("Test", state.tag),
                virtualState => Assert.AreEqual("Test", virtualState.Tag)
            );
        }
        
        [Test]
        public void PreservesTimeParameter()
        {
            AssertPreserveProperty(
                state =>
                {
                    state.timeParameterActive = true;
                    state.timeParameter = "Test";
                },
                virtualState => virtualState.TimeParameter = "Test",
                state =>
                {
                    Assert.IsTrue(state.timeParameterActive);
                    Assert.AreEqual("Test", state.timeParameter);
                },
                virtualState =>
                {
                    Assert.AreEqual("Test", virtualState.TimeParameter);
                }
            );
        }
        
        [Test]
        public void PreservesWriteDefaultValues()
        {
            AssertPreserveProperty(
                state =>
                {
                    state.writeDefaultValues = true;
                },
                virtualState => virtualState.WriteDefaultValues = true,
                state =>
                {
                    Assert.IsTrue(state.writeDefaultValues);
                },
                virtualState =>
                {
                    Assert.IsTrue(virtualState.WriteDefaultValues);
                }
            );
        }
    }
}