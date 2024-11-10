using System;
using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor.Animations;

namespace UnitTests.AnimationServices
{
    public class VirtualStateTransitionTest : TestBase
    {
        // This method is copypasted a bit due to limitations of C#'s type system - specifically,
        // I can't put this into a base class, because that would force ICommitable to be public.
        private void AssertPreservePropertyST(
            Func<AnimatorStateTransition> create,
            Action<AnimatorStateTransition> setup,
            Action<VirtualStateTransition> setupViaVirtualState,
            Action<AnimatorStateTransition> assert,
            Action<VirtualStateTransition> assertViaVirtualState
        )
        {
            var transition = create();
            setup(transition);

            var cloneContext = new CloneContext(new GenericPlatformAnimatorBindings());

            VirtualStateTransition virtualStateTransition = cloneContext.Clone(transition);
            assertViaVirtualState(virtualStateTransition);

            var commitContext = new CommitContext();
            var committed = (AnimatorStateTransition) commitContext.CommitObject(virtualStateTransition);
            Assert.AreNotEqual(transition, committed);
            assert(committed);
            
            UnityEngine.Object.DestroyImmediate(transition);
            UnityEngine.Object.DestroyImmediate(committed);

            transition = create();

            virtualStateTransition = cloneContext.Clone(transition);
            
            bool wasInvalidated = false;
            virtualStateTransition.RegisterCacheObserver(() => { wasInvalidated = true; });
            setupViaVirtualState(virtualStateTransition);
            Assert.IsTrue(wasInvalidated);
            
            committed = (AnimatorStateTransition) commitContext.CommitObject(virtualStateTransition);
            
            assert(committed);
            
            UnityEngine.Object.DestroyImmediate(transition);
            UnityEngine.Object.DestroyImmediate(committed);
        }
        
        private void AssertPreserveProperty(
            Func<AnimatorTransition> create,
            Action<AnimatorTransition> setup,
            Action<VirtualTransition> setupViaVirtualState,
            Action<AnimatorTransition> assert,
            Action<VirtualTransition> assertViaVirtualState
        )
        {
            var transition = create();
            setup(transition);

            var cloneContext = new CloneContext(new GenericPlatformAnimatorBindings());

            VirtualTransition virtualStateTransition = cloneContext.Clone(transition);
            assertViaVirtualState(virtualStateTransition);

            var commitContext = new CommitContext();
            var committed = (AnimatorTransition) commitContext.CommitObject(virtualStateTransition);
            Assert.AreNotEqual(transition, committed);
            assert(committed);
            
            UnityEngine.Object.DestroyImmediate(transition);
            UnityEngine.Object.DestroyImmediate(committed);

            transition = create();

            virtualStateTransition = cloneContext.Clone(transition);
            
            bool wasInvalidated = false;
            virtualStateTransition.RegisterCacheObserver(() => { wasInvalidated = true; });
            setupViaVirtualState(virtualStateTransition);
            Assert.IsTrue(wasInvalidated);
            
            committed = (AnimatorTransition) commitContext.CommitObject(virtualStateTransition);
            
            assert(committed);
            
            UnityEngine.Object.DestroyImmediate(transition);
            UnityEngine.Object.DestroyImmediate(committed);
        }
        
        
        [Test]
        public void PreservesCanTransitionToSelf()
        {
            AssertPreservePropertyST(
                () => new AnimatorStateTransition(),
                transition => transition.canTransitionToSelf = true,
                virtualTransition => virtualTransition.CanTransitionToSelf = true,
                transition => Assert.IsTrue(transition.canTransitionToSelf),
                virtualTransition => Assert.IsTrue(virtualTransition.CanTransitionToSelf)
            );
        }
        
        [Test]
        public void PreservesDuration()
        {
            AssertPreservePropertyST(
                () => new AnimatorStateTransition(),
                transition => transition.duration = 0.5f,
                virtualTransition => virtualTransition.Duration = 0.5f,
                transition => Assert.AreEqual(0.5f, transition.duration),
                virtualTransition => Assert.AreEqual(0.5f, virtualTransition.Duration)
            );
        }
        
        [Test]
        public void PreservesExitTime()
        {
            AssertPreservePropertyST(
                () => new AnimatorStateTransition(),
                transition =>
                {
                    transition.exitTime = 0.5f;
                    transition.hasExitTime = true;
                },
                virtualTransition => virtualTransition.ExitTime = 0.5f,
                transition =>
                {
                    Assert.IsTrue(transition.hasExitTime);
                    Assert.AreEqual(0.5f, transition.exitTime);
                },
                virtualTransition => Assert.AreEqual(0.5f, virtualTransition.ExitTime)
            );
            
            // Reset to null after creation
            AssertPreservePropertyST(
                () =>
                {
                    var t = new AnimatorStateTransition();
                    t.exitTime = 123;
                    t.hasExitTime = true;

                    return t;
                },
                transition => { transition.hasExitTime = false; },
                virtualTransition => virtualTransition.ExitTime = null,
                transition => Assert.IsFalse((bool) transition.hasExitTime),
                virtualTransition => Assert.IsNull(virtualTransition.ExitTime)
            );
        }
        
        [Test]
        public void PreservesHasFixedDuration()
        {
            AssertPreservePropertyST(
                () => new AnimatorStateTransition(),
                transition => transition.hasFixedDuration = true,
                virtualTransition => virtualTransition.HasFixedDuration = true,
                transition => Assert.IsTrue(transition.hasFixedDuration),
                virtualTransition => Assert.IsTrue(virtualTransition.HasFixedDuration)
            );
        }
        
        [Test]
        public void PreservesInterruptionSource()
        {
            AssertPreservePropertyST(
                () => new AnimatorStateTransition(),
                transition => transition.interruptionSource = TransitionInterruptionSource.Destination,
                virtualTransition => virtualTransition.InterruptionSource = TransitionInterruptionSource.Destination,
                transition => Assert.AreEqual(TransitionInterruptionSource.Destination, transition.interruptionSource),
                virtualTransition => Assert.AreEqual(TransitionInterruptionSource.Destination, virtualTransition.InterruptionSource)
            );
        }
        
        [Test]
        public void PreservesOffset()
        {
            AssertPreservePropertyST(
                () => new AnimatorStateTransition(),
                transition => transition.offset = 0.5f,
                virtualTransition => virtualTransition.Offset = 0.5f,
                transition => Assert.AreEqual(0.5f, transition.offset),
                virtualTransition => Assert.AreEqual(0.5f, virtualTransition.Offset)
            );
        }
        
        [Test]
        public void PreservesOrderedInterruption()
        {
            AssertPreservePropertyST(
                () => new AnimatorStateTransition(),
                transition => transition.orderedInterruption = true,
                virtualTransition => virtualTransition.OrderedInterruption = true,
                transition => Assert.IsTrue(transition.orderedInterruption),
                virtualTransition => Assert.IsTrue(virtualTransition.OrderedInterruption)
            );
        }
        
        [Test]
        public void PreservesConditions()
        {
            var conditions = new[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.Equals,
                    parameter = "Test",
                    threshold = 0.5f
                }
            };
            
            AssertPreserveProperty(
                () => new AnimatorTransition() { conditions = conditions },
                transition => { },
                virtualTransition => { virtualTransition.Invalidate(); },
                transition =>
                {
                    Assert.AreEqual(1, transition.conditions.Length);
                    Assert.AreEqual(AnimatorConditionMode.Equals, transition.conditions[0].mode);
                    Assert.AreEqual("Test", transition.conditions[0].parameter);
                    Assert.AreEqual(0.5f, transition.conditions[0].threshold);
                },
                virtualTransition =>
                {
                    Assert.AreEqual(1, virtualTransition.Conditions.Count);
                    Assert.AreEqual(AnimatorConditionMode.Equals, virtualTransition.Conditions[0].mode);
                    Assert.AreEqual("Test", virtualTransition.Conditions[0].parameter);
                    Assert.AreEqual(0.5f, virtualTransition.Conditions[0].threshold);
                }
            );
            
            // TODO: Should we clone the conditions list?
        }
        
        [Test]
        public void PreservesDestinationStateMachine()
        {
            // TODO
        }
        
        [Test]
        public void PreservesDestinationState()
        {
            // TODO
        }
        
        [Test]
        public void PreservesExitIsDestination()
        {
            // TODO
        }

        [Test]
        public void PreservesMute()
        {
            AssertPreserveProperty(
                () => new AnimatorTransition(),
                transition => transition.mute = true,
                virtualTransition => virtualTransition.Mute = true,
                transition => Assert.IsTrue(transition.mute),
                virtualTransition => Assert.IsTrue(virtualTransition.Mute)
            );
        }
        
        [Test]
        public void PreservesSolo()
        {
            AssertPreserveProperty(
                () => new AnimatorTransition(),
                transition => transition.solo = true,
                virtualTransition => virtualTransition.Solo = true,
                transition => Assert.IsTrue(transition.solo),
                virtualTransition => Assert.IsTrue(virtualTransition.Solo)
            );
        }
        
        [Test]
        public void PreservesName()
        {
            AssertPreserveProperty(
                () => new AnimatorTransition(),
                transition => transition.name = "Test",
                virtualTransition => virtualTransition.Name = "Test",
                transition => Assert.AreEqual("Test", transition.name),
                virtualTransition => Assert.AreEqual("Test", virtualTransition.Name)
            );
        }
    }
}