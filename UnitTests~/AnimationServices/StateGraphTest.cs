using System.Collections.Immutable;
using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor.Animations;

namespace UnitTests.AnimationServices
{
    public class StateGraphTest : TestBase
    {
        [Test]
        public void TestStateGraphConvergence()
        {
            var s1 = new AnimatorState();
            var s2 = new AnimatorState();
            var s3 = new AnimatorState();

            s1.transitions = new[]
            {
                new AnimatorStateTransition()
                {
                    destinationState = s2
                },
                new AnimatorStateTransition()
                {
                    conditions = new []
                    {
                        new AnimatorCondition()
                        {
                            parameter = "x"
                        }
                    },
                    destinationState = s3
                },
                new AnimatorStateTransition()
                {
                    destinationState = s2
                },
                new AnimatorStateTransition()
                {
                    destinationState = s1
                }
            };

            s2.transitions = new[]
            {
                new AnimatorStateTransition() { destinationState = s1 },
                new AnimatorStateTransition() { destinationState = s3 },
                new AnimatorStateTransition() { isExit = true },
            };
            
            var cloneContext = new CloneContext(GenericPlatformAnimatorBindings.Instance);
            var clonedS1 = cloneContext.Clone(s1);
            
            Assert.AreEqual(clonedS1.Transitions.Count, 4);
            var clonedS2 = clonedS1.Transitions[0].DestinationState;
            var clonedS3 = clonedS1.Transitions[1].DestinationState;
            Assert.AreEqual(clonedS2, clonedS1.Transitions[2].DestinationState);
            Assert.AreEqual(clonedS1, clonedS1.Transitions[3].DestinationState);
            
            Assert.AreEqual(clonedS2.Transitions.Count, 3);
            Assert.AreEqual(clonedS1, clonedS2.Transitions[0].DestinationState);
            Assert.AreEqual(clonedS3, clonedS2.Transitions[1].DestinationState);
            Assert.IsTrue(clonedS2.Transitions[2].IsExit);
            
            // Check that we cache clones appropriately
            Assert.AreEqual(clonedS1, cloneContext.Clone(s1));
            Assert.AreEqual(clonedS2, cloneContext.Clone(s2));
            Assert.AreEqual(clonedS3, cloneContext.Clone(s3));
            
            // Commit and check that we preserve the graph appropriately
            var commitContext = new CommitContext();
            
            var committedS1 = commitContext.CommitObject(clonedS1);
            Assert.AreNotEqual(s1, committedS1);
            
            var committedS2 = committedS1.transitions[0].destinationState;
            Assert.AreNotEqual(s2, committedS2);
            
            var committedS3 = committedS1.transitions[1].destinationState;
            Assert.AreNotEqual(s3, committedS3);
            
            Assert.AreEqual(committedS2, committedS1.transitions[2].destinationState);
            Assert.AreEqual(committedS1, committedS1.transitions[3].destinationState);
            
            Assert.AreEqual(committedS2.transitions.Length, 3);
            Assert.AreEqual(committedS1, committedS2.transitions[0].destinationState);
            Assert.AreEqual(committedS3, committedS2.transitions[1].destinationState);
            Assert.IsTrue(committedS2.transitions[2].isExit);
        }

        [Test]
        public void TestStateMachineTransitions()
        {
            var sm1 = new AnimatorStateMachine() { name = "sm1" };
            var sm2 = new AnimatorStateMachine() { name = "sm2" };
            var sm3 = new AnimatorStateMachine() { name = "sm3" };
            
            var s1 = new AnimatorState() { name = "s1" };
            var s2 = new AnimatorState() { name = "s2" };
            
            sm1.stateMachines = new[]
            {
                new ChildAnimatorStateMachine()
                {
                    stateMachine = sm2
                },
                new ChildAnimatorStateMachine()
                {
                    stateMachine = sm3
                }
            };
            sm1.states = new[]
            {
                new ChildAnimatorState()
                {
                    state = s1
                },
                new ChildAnimatorState()
                {
                    state = s2
                }
            };

            sm1.SetStateMachineTransitions(sm2, new[]
            {
                new AnimatorTransition()
                {
                    destinationState = s1,
                    conditions = new AnimatorCondition[0]
                }
            });
            
            var cloneContext = new CloneContext(GenericPlatformAnimatorBindings.Instance);
            var clonedSM1 = cloneContext.Clone(sm1);
            
            Assert.AreEqual(clonedSM1.StateMachines.Count, 2);
            var clonedSM2 = clonedSM1.StateMachines[0].StateMachine;
            var clonedSM3 = clonedSM1.StateMachines[1].StateMachine;

            var clonedS1 = clonedSM1.States[0].State;
            var clonedS2 = clonedSM1.States[1].State;
            
            Assert.AreEqual(clonedSM1.StateMachineTransitions.Count, 2);
            Assert.AreEqual(clonedS1, clonedSM1.StateMachineTransitions[clonedSM2][0].DestinationState);
            Assert.AreEqual(0, clonedSM1.StateMachineTransitions[clonedSM3].Count);

            var vt = VirtualTransition.Create();
            vt.SetDestination(clonedS2);
            
            clonedSM1.StateMachineTransitions = clonedSM1.StateMachineTransitions.SetItem(
                clonedSM3,
                ImmutableList<VirtualTransition>.Empty.Add(vt)
            );
            
            var commitContext = new CommitContext();
            var outSM1 = commitContext.CommitObject(clonedSM1);
            
            var outSM2 = outSM1.stateMachines[0].stateMachine;
            var outSM3 = outSM1.stateMachines[1].stateMachine;

            var outS1 = outSM1.states[0].state;
            var outS2 = outSM1.states[1].state;
            
            var stateTransitions2 = outSM1.GetStateMachineTransitions(outSM2);
            Assert.AreEqual(stateTransitions2.Length, 1);
            Assert.AreEqual(outS1, stateTransitions2[0].destinationState);
            
            var stateTransitions3 = outSM1.GetStateMachineTransitions(outSM3);
            Assert.AreEqual(stateTransitions3.Length, 1);
            Assert.AreEqual(outS2, stateTransitions3[0].destinationState);
        }
    }
}