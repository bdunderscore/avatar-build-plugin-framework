using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using Assert = UnityEngine.Assertions.Assert;

namespace UnitTests.AnimationServices
{
    public class VirtualAnimatorControllerTest : TestBase
    {
        [Test]
        public void PreservesParameters()
        {
            CloneContext context = new CloneContext(new GenericPlatformAnimatorBindings());
            
            var controller = TrackObject(new AnimatorController());
            controller.AddParameter("x", AnimatorControllerParameterType.Float);
            controller.AddParameter("y", AnimatorControllerParameterType.Bool);
            
            var virtualController = context.Clone(controller);
            Assert.AreEqual(2, virtualController.Parameters.Count);
            Assert.AreEqual(AnimatorControllerParameterType.Float, virtualController.Parameters["x"].type);
            Assert.AreEqual(AnimatorControllerParameterType.Bool, virtualController.Parameters["y"].type);
            virtualController.Parameters["z"] = new AnimatorControllerParameter()
            {
                name = "ignored",
                type = AnimatorControllerParameterType.Trigger
            };
            
            var commitContext = new CommitContext();
            var committed = commitContext.CommitObject(virtualController);
            
            Assert.AreEqual(3, committed.parameters.Length);
            Assert.AreEqual("x", committed.parameters[0].name);
            Assert.AreEqual(AnimatorControllerParameterType.Float, committed.parameters[0].type);
            Assert.AreEqual("y", committed.parameters[1].name);
            Assert.AreEqual(AnimatorControllerParameterType.Bool, committed.parameters[1].type);
            Assert.AreEqual("z", committed.parameters[2].name);
            Assert.AreEqual(AnimatorControllerParameterType.Trigger, committed.parameters[2].type);
            
            commitContext.DestroyAllImmediate();
        }

        [Test]
        public void PreservesLayersAndReferences()
        {
            CloneContext context = new CloneContext(new GenericPlatformAnimatorBindings());
            
            var ac1 = TrackObject(new AnimatorController());
            var ac2 = TrackObject(new AnimatorController());

            ac1.layers = new[]
            {
                new AnimatorControllerLayer()
                {
                    name = "1",
                    stateMachine = new AnimatorStateMachine() { name = "1" }
                },
                new AnimatorControllerLayer()
                {
                    name = "2",
                    syncedLayerIndex = 0
                }
            };
            ac2.layers = new[]
            {
                new AnimatorControllerLayer()
                {
                    name = "3",
                    stateMachine = new AnimatorStateMachine() { name = "1" }
                },
                new AnimatorControllerLayer()
                {
                    name = "4",
                    syncedLayerIndex = 0
                }
            };
            
            var vc1 = context.Clone(ac1);
            var vc2 = context.Clone(ac2);
            
            vc1.Layers.AddRange(vc2.Layers);
            
            var commitContext = new CommitContext();
            var committed = commitContext.CommitObject(vc1);
            
            Assert.AreEqual(4, committed.layers.Length);
            Assert.AreEqual("1", committed.layers[0].name);
            Assert.AreEqual("2", committed.layers[1].name);
            Assert.AreEqual("3", committed.layers[2].name);
            Assert.AreEqual("4", committed.layers[3].name);
            
            Assert.AreEqual("1", committed.layers[0].stateMachine.name);
            Assert.AreEqual(-1, committed.layers[0].syncedLayerIndex);
            Assert.AreEqual(0, committed.layers[1].syncedLayerIndex);
            Assert.AreEqual(-1, committed.layers[2].syncedLayerIndex);
            Assert.AreEqual(2, committed.layers[3].syncedLayerIndex);
            
            commitContext.DestroyAllImmediate();
        }
    }
}