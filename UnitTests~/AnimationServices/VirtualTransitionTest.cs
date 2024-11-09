using System;
using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor.Animations;

namespace UnitTests.AnimationServices
{
    public class VirtualTransitionTest : TestBase
    {
        // This method is copypasted a bit due to limitations of C#'s type system - specifically,
        // I can't put this into a base class, because that would force ICommitable to be public.
        private void AssertPreserveProperty<T>(
            Func<T> create,
            Action<T> setup,
            Action<VirtualTransition> setupViaVirtualState,
            Action<T> assert,
            Action<VirtualTransition> assertViaVirtualState
        ) where T: AnimatorTransitionBase
        {
            var transition = create();
            setup(transition);

            var cloneContext = new CloneContext(new GenericPlatformAnimatorBindings());

            VirtualTransition virtualTransition = cloneContext.Clone(transition);
            assertViaVirtualState(virtualTransition);

            var commitContext = new CommitContext();
            var committed = commitContext.CommitObject(virtualTransition);
            Assert.AreNotEqual(transition, committed);
            assert((T) committed);
            
            UnityEngine.Object.DestroyImmediate(transition);
            UnityEngine.Object.DestroyImmediate(committed);

            transition = create();

            virtualTransition = cloneContext.Clone(transition);
            setupViaVirtualState(virtualTransition);
            
            committed = commitContext.CommitObject(virtualTransition);
            
            assert((T) committed);
            
            UnityEngine.Object.DestroyImmediate(transition);
            UnityEngine.Object.DestroyImmediate(committed);
            
            // For properties specific to AnimatorStateTransition, make sure we throw on AnimatorTransitions
            if (committed is not AnimatorStateTransition) return;
            
            var genericTransition = new AnimatorTransition();
            virtualTransition = cloneContext.Clone(genericTransition);
            Assert.Throws<InvalidOperationException>(() => setupViaVirtualState(virtualTransition));
        }
        
        
        [Test]
        public void PreservesCanTransitionToSelf()
        {
            AssertPreserveProperty(
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
            AssertPreserveProperty(
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
            AssertPreserveProperty(
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
            AssertPreserveProperty(
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
            AssertPreserveProperty(
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
            AssertPreserveProperty(
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
            AssertPreserveProperty(
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
            AssertPreserveProperty(
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
                virtualTransition => { },
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