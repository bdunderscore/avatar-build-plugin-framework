using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf.animator;
using nadena.dev.ndmf.UnitTestSupport;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

namespace UnitTests.AnimationServices
{
    public class SyncedLayerOverrideAccessTest : TestBase
    {
        [Test]
        public void Test_ExtractStateMotionPairs()
        {
            var ac = CreateTestController(out var clip1, out var clip2, out var s1);

            var l1 = ac.layers[1];
            l1.SetOverrideMotion(s1, clip1);
            ac.layers = new[]
            {
                ac.layers[0],
                l1
            };
            
            var pairs = SyncedLayerOverrideAccess.ExtractStateMotionPairs(ac.layers[1]).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            Assert.AreEqual(1, pairs.Count);
            Assert.AreEqual(clip1, pairs[s1]);
        }
        
        [Test]
        public void Test_ExtractStateBehaviorPairs()
        {
            var ac = CreateTestController(out var clip1, out var clip2, out var s1);

            var l1 = ac.layers[1];
            l1.SetOverrideBehaviours(s1, new StateMachineBehaviour[] { ScriptableObject.CreateInstance<TestStateBehavior>() });
            ac.layers = new[]
            {
                ac.layers[0],
                l1
            };
            
            var pairs = SyncedLayerOverrideAccess.ExtractStateBehaviourPairs(ac.layers[1]).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            Assert.AreEqual(1, pairs.Count);
            Assert.AreEqual(1, pairs[s1].Length);
            Assert.AreEqual(typeof(TestStateBehavior), pairs[s1][0].GetType());
        }


        [Test]
        public void Test_SetStateMotionPairs()
        {
            var ac = CreateTestController(out var clip1, out var clip2, out var s1);

            var l1 = ac.layers[1];
            SyncedLayerOverrideAccess.SetStateMotionPairs(l1, new Dictionary<AnimatorState, Motion>
            {
                {s1, clip2}
            });
            
            // Make sure we can save back to the controller (Unity native code) and read back
            ac.layers = new[]
            {
                ac.layers[0],
                l1
            };
            l1 = ac.layers[1];
            
            Assert.AreEqual(clip2, l1.GetOverrideMotion(s1));
        }

        [Test]
        public void Test_SetStateBehaviourPairs()
        {
            var ac = CreateTestController(out var clip1, out var clip2, out var s1);
            
            var l1 = ac.layers[1];
            SyncedLayerOverrideAccess.SetStateBehaviourPairs(l1, new Dictionary<AnimatorState, ScriptableObject[]>
            {
                {s1, new ScriptableObject[] {ScriptableObject.CreateInstance<TestStateBehavior>()}}
            });
            
            // Make sure we can save back to the controller (Unity native code) and read back
            ac.layers = new[]
            {
                ac.layers[0],
                l1
            };
            l1 = ac.layers[1];
            
            Assert.AreEqual(1, l1.GetOverrideBehaviours(s1).Length);
        }
        

        private AnimatorController CreateTestController(out AnimationClip clip1, out AnimationClip clip2, out AnimatorState s1)
        {
            var ac = TrackObject(new AnimatorController());
            var sm = TrackObject(new AnimatorStateMachine());
            ac.layers = new[]
            {
                new AnimatorControllerLayer {stateMachine = sm},
                new AnimatorControllerLayer {syncedLayerIndex = 0}
            };
            
            clip1 = TrackObject(new AnimationClip {name = "c1"});
            clip2 = TrackObject(new AnimationClip {name = "c2"});
            
            s1 = TrackObject(new AnimatorState {name = "s1", motion = clip1});
            var s2 = TrackObject(new AnimatorState {name = "s2", motion = clip2});
            
            sm.states = new[]
            {
                new ChildAnimatorState {state = s1},
                new ChildAnimatorState {state = s2}
            };
            return ac;
        }
    }
}