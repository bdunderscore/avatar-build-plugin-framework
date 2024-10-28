using System;
using System.Linq;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnitTests.AnimationServices
{
    public class VirtualClipTest : TestBase
    {
        private GameObject avatarRoot;
        private BuildContext context;
        
        [SetUp]
        public void Setup()
        {
            avatarRoot = CreateRoot("root");
            context = CreateContext(avatarRoot);
        }

        AnimationClip Commit(VirtualClip clip)
        {
            return TrackObject((AnimationClip) new CommitContext().CommitObject(clip));
        }

        [Test]
        public void PreservesInitialCurves()
        {
            var material = NewTestMaterial();
            AnimationClip ac = TrackObject(new AnimationClip());
            ac.name = "foo";
            var originalCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
            ac.SetCurve("abc", typeof(GameObject), "m_IsActive", originalCurve);
            
            AnimationUtility.SetObjectReferenceCurve(ac, EditorCurveBinding.PPtrCurve("def", typeof(MeshRenderer), "m_Materials"), new ObjectReferenceKeyframe[]
            {
                new() {time = 0, value = material},
            });
            
            VirtualClip vc = VirtualClip.Clone(context, ac);
            var committedClip = Commit(vc);
            
            var bindings = AnimationUtility.GetCurveBindings(committedClip).ToList();
            Assert.AreEqual(1, bindings.Count);
            Assert.AreEqual("abc", bindings[0].path);
            Assert.AreEqual(originalCurve, AnimationUtility.GetEditorCurve(committedClip, bindings[0]));
            
            var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(committedClip);
            Assert.AreEqual(1, objBindings.Length);
            Assert.AreEqual("def", objBindings[0].path);
            Assert.AreEqual(material, AnimationUtility.GetObjectReferenceCurve(committedClip, objBindings[0])[0].value);
        }
        
        [Test]
        public void EditExistingFloatCurve()
        {
            AnimationClip ac = TrackObject(new AnimationClip());
            ac.name = "foo";
            var originalCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
            ac.SetCurve("abc", typeof(GameObject), "m_IsActive", originalCurve);
            
            VirtualClip vc = VirtualClip.Clone(context, ac);

            var bindings = vc.GetFloatCurveBindings().ToList();
            Assert.AreEqual(1, bindings.Count);
            Assert.AreEqual("abc", bindings[0].path);
            Assert.AreEqual(typeof(GameObject), bindings[0].type);
            Assert.AreEqual("m_IsActive", bindings[0].propertyName);
            
            var existingCurve = vc.GetFloatCurve("abc", typeof(GameObject), "m_IsActive");
            Assert.IsNotNull(existingCurve);
            AssertEqualNotSame(existingCurve, originalCurve);
            
            // Replace the curve and see if it commits
            var newCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 2));
            vc.SetFloatCurve("abc", typeof(GameObject), "m_IsActive", newCurve);
            
            var committedClip = Commit(vc);
            var newCommittedCurve = AnimationUtility.GetEditorCurve(committedClip, bindings[0]);
            AssertEqualNotSame(newCommittedCurve, newCurve);
            Assert.AreEqual(committedClip.name, ac.name);
        }

        [Test]
        public void CreateDeleteFloatCurve()
        {
            AnimationClip ac = TrackObject(new AnimationClip());
            ac.SetCurve("abc", typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));
            
            VirtualClip vc = VirtualClip.Clone(context, ac);
            vc.SetFloatCurve("abc", typeof(GameObject), "m_IsActive", null);
            vc.SetFloatCurve("def", typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));
            
            Assert.AreEqual(vc.GetFloatCurveBindings().Count(), 1);
            
            var committedClip = Commit(vc);
            var bindings = AnimationUtility.GetCurveBindings(committedClip).ToList();
            Assert.AreEqual(1, bindings.Count);
            Assert.AreEqual("def", bindings[0].path);
        }

        [Test]
        public void EditExistingObjectCurve()
        {
            var m1 = TrackObject(NewTestMaterial());
            var m2 = TrackObject(NewTestMaterial());
            
            AnimationClip ac = TrackObject(new AnimationClip());
            AnimationUtility.SetObjectReferenceCurve(ac, new EditorCurveBinding()
            {
                path = "abc",
                type = typeof(MeshRenderer),
                propertyName = "m_Materials.Array.data[0]"
            }, new ObjectReferenceKeyframe[]
            {
                new() {time = 0, value = m1},
            });
            
            VirtualClip vc = VirtualClip.Clone(context, ac);
            vc.SetObjectCurve("abc", typeof(MeshRenderer), "m_Materials.Array.data[0]", new ObjectReferenceKeyframe[]
            {
                new() {time = 0, value = m2},
            });
            
            var committedClip = Commit(vc);
            var newCommittedCurve = AnimationUtility.GetObjectReferenceCurve(committedClip, new EditorCurveBinding()
            {
                path = "abc",
                type = typeof(MeshRenderer),
                propertyName = "m_Materials.Array.data[0]"
            });
            Assert.IsNotNull(newCommittedCurve);
            Assert.AreEqual(1, newCommittedCurve.Length);
            Assert.AreEqual(0, newCommittedCurve[0].time);
            Assert.AreEqual(m2, newCommittedCurve[0].value);
            
            // check that the original clip is not modified
            var originalCurve = AnimationUtility.GetObjectReferenceCurve(ac, new EditorCurveBinding()
            {
                path = "abc",
                type = typeof(MeshRenderer),
                propertyName = "m_Materials.Array.data[0]"
            });
            Assert.IsNotNull(originalCurve);
            Assert.AreEqual(m1, originalCurve[0].value);
        }

        private Material NewTestMaterial()
        {
            Shader s = Shader.Find("Unlit/Color");
            return new Material(s);
        }

        [Test]
        public void CreateDeleteObjectCurve()
        {
            var m1 = TrackObject(NewTestMaterial());

            var ac = TrackObject(new AnimationClip());
            AnimationUtility.SetObjectReferenceCurve(ac, new EditorCurveBinding()
            {
                path = "abc",
                type = typeof(MeshRenderer),
                propertyName = "m_Materials.Array.data[0]"
            }, new ObjectReferenceKeyframe[]
            {
                new() {time = 0, value = m1},
            });
            
            VirtualClip vc = VirtualClip.Clone(context, ac);
            vc.SetObjectCurve("abc", typeof(MeshRenderer), "m_Materials.Array.data[0]", null);
            vc.SetObjectCurve("def", typeof(MeshRenderer), "m_Materials.Array.data[0]", new ObjectReferenceKeyframe[]
            {
                new() {time = 0, value = m1},
            });
            
            Assert.AreEqual(1, vc.GetObjectCurveBindings().Count());
            
            var committedClip = Commit(vc);
            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(committedClip);
            Assert.AreEqual(1, bindings.Length);
            Assert.AreEqual("def", bindings[0].path);
        }

        [Test]
        public void TestEditPath()
        {
            AnimationClip ac = TrackObject(new AnimationClip());
            ac.SetCurve("abc", typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));
            ac.SetCurve("DEF", typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));
            ac.SetCurve("x", typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));
            ac.SetCurve("X", typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));
            AnimationUtility.SetObjectReferenceCurve(ac, new EditorCurveBinding()
            {
                path = "foo",
                type = typeof(MeshRenderer),
                propertyName = "m_Materials.Array.data[0]"
            }, new ObjectReferenceKeyframe[]
            {
                new() {time = 0, value = new Material(Shader.Find("Standard"))},
            });
            
            VirtualClip vc = VirtualClip.Clone(context, ac);
            vc.EditPaths(s => s.ToUpperInvariant());
            
            Assert.AreEqual(new[] { "ABC", "DEF", "X" }, vc.GetFloatCurveBindings().Select(b => b.path).OrderBy(b => b).ToArray());
            Assert.AreEqual(new[] { "FOO" }, vc.GetObjectCurveBindings().Select(b => b.path).OrderBy(b => b).ToArray());
            
            var committedClip = Commit(vc);
            var bindings = AnimationUtility.GetCurveBindings(committedClip).Select(b => b.path).OrderBy(b => b).ToList();
            Assert.AreEqual(new[] { "ABC", "DEF", "X" }, bindings);
            
            var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(committedClip).Select(b => b.path).OrderBy(b => b).ToList();
            Assert.AreEqual( new[] { "FOO" }, objBindings);
        }

        [Test]
        public void PreservesHighQualityMode([Values("HQ_ON.anim", "HQ_OFF.anim")] string testAsset)
        {
            AnimationClip ac = LoadAsset<AnimationClip>(testAsset);
            bool hq = new SerializedObject(ac).FindProperty("m_UseHighQualityCurve").boolValue;
            
            VirtualClip vc = VirtualClip.Clone(context, ac);
            
            vc.SetFloatCurve(EditorCurveBinding.FloatCurve("abc", typeof(GameObject), "m_IsActive"), 
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));
            
            var committedClip = Commit(vc);
            
            Assert.AreEqual(hq, new SerializedObject(committedClip).FindProperty("m_UseHighQualityCurve").boolValue);
        }
        
        // TODO: additive reference pose, animation clip settings/misc properties tests

        private static void AssertEqualNotSame(AnimationCurve newCommittedCurve, AnimationCurve newCurve)
        {
            Assert.IsNotNull(newCommittedCurve);
            Assert.AreNotSame(newCommittedCurve, newCurve);
            Assert.AreEqual(newCurve.keys.Length, newCommittedCurve.keys.Length);
            for (int i = 0; i < newCurve.keys.Length; i++)
            {
                Assert.AreEqual(newCurve.keys[i].time, newCommittedCurve.keys[i].time);
                Assert.AreEqual(newCurve.keys[i].value, newCommittedCurve.keys[i].value);
            }
        }
    }
}