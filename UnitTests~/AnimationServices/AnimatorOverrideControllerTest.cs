using System.Linq;
using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

namespace UnitTests.AnimationServices
{
    public class AnimatorOverrideControllerTest
    {
        [Test]
        public void TestSimpleOverride()
        {
            var cloneContext = new CloneContext(new GenericPlatformAnimatorBindings());

            var originalController = new AnimatorController();
            var originalStateMachine = new AnimatorStateMachine();
            originalController.layers = new[] {new AnimatorControllerLayer {stateMachine = originalStateMachine}};
            
            var clip1 = new AnimationClip {name = "c1"};
            var clip2 = new AnimationClip {name = "c2"};
            
            var s1 = new AnimatorState {name = "s1", motion = clip1};
            var s2 = new AnimatorState {name = "s2", motion = clip2};
            
            originalStateMachine.states = new[] {new ChildAnimatorState {state = s1}, new ChildAnimatorState {state = s2}};
            originalStateMachine.defaultState = s1;
            
            var overrideController = new AnimatorOverrideController();
            overrideController.runtimeAnimatorController = originalController;
            
            var clip3 = new AnimationClip {name = "c3"};
            overrideController[clip1] = clip3;
            
            var virtualController = cloneContext.Clone(overrideController);
            var virtualStateMachine = virtualController.Layers.First().StateMachine;
            var virtualS1 = virtualStateMachine.States.First(s => s.State.Name == "s1");
            var virtualS2 = virtualStateMachine.States.First(s => s.State.Name == "s2");
            
            Assert.AreEqual("c3", virtualS1.State.Motion.Name);
            Assert.AreEqual("c2", virtualS2.State.Motion.Name);
        }

        [Test]
        public void TestBlendTreeChildOverride()
        {
            var cloneContext = new CloneContext(new GenericPlatformAnimatorBindings());
            
            var originalController = new AnimatorController();
            var originalStateMachine = new AnimatorStateMachine();
            originalController.layers = new[] {new AnimatorControllerLayer {stateMachine = originalStateMachine}};
            originalController.AddParameter("Blend", AnimatorControllerParameterType.Float);
            
            var clip1 = new AnimationClip {name = "c1"};
            var clip2 = new AnimationClip {name = "c2"};
            var clip3 = new AnimationClip {name = "c3"};
            var bt = new BlendTree {name = "bt", blendType = BlendTreeType.Simple1D};
            bt.children = new[]
            {
                new ChildMotion {motion = clip1, timeScale = 1},
                new ChildMotion {motion = clip2, timeScale = 1}
            };
            
            var s1 = new AnimatorState {name = "s1", motion = bt};
            originalStateMachine.states = new[] {new ChildAnimatorState {state = s1}};
            originalStateMachine.defaultState = s1;
            
            var overrideController = new AnimatorOverrideController();
            overrideController.runtimeAnimatorController = originalController;
            overrideController[clip1] = clip3;
            
            var virtualController = cloneContext.Clone(overrideController);
            var virtualStateMachine = virtualController.Layers.First().StateMachine;
            var virtualS1 = virtualStateMachine.States.First(s => s.State.Name == "s1");
            var virtualBlendTree = (VirtualBlendTree) virtualS1.State.Motion;
            
            Assert.AreEqual("c3", virtualBlendTree.Children.First().Motion.Name);
            Assert.AreEqual("c2", virtualBlendTree.Children.Last().Motion.Name);
        }
    }
}