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
            
            var cloneContext = new CloneContext(new GenericPlatformAnimatorBindings());
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
    }
}