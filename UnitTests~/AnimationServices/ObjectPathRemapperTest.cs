using System.Collections.Generic;
using nadena.dev.ndmf.animator;
using NUnit.Framework;

namespace UnitTests.AnimationServices
{
    public class ObjectPathRemapperTest : TestBase
    {
        [Test]
        public void TracksRenames()
        {
            var root = CreateRoot("x");
            
            var c1 = CreateChild(root, "c1");

            var remapper = new ObjectPathRemapper(root.transform);

            c1.name = "c2";
            
            Assert.AreEqual("c1", remapper.GetVirtualPathForObject(c1));
            Assert.AreEqual("c1", remapper.GetVirtualPathForObject(c1.transform));

            Assert.That(remapper.GetVirtualToRealPathMap(), Is.EquivalentTo(
                new[]
                {
                    new KeyValuePair<string, string>("c1", "c2")
                }
            ));
        }

        [Test]
        public void WhenObjectIsRenamed_AndANewObjectWithTheSameNameAppears_CorrectlyTracked()
        {
            var root = CreateRoot("x");
            
            var c1 = CreateChild(root, "c1");
            
            var remapper = new ObjectPathRemapper(root.transform);

            c1.name = "c2";
            
            var c1x = CreateChild(root, "c1");
            var vpath = remapper.GetVirtualPathForObject(c1x);
            Assert.AreNotEqual("c1", vpath);
            Assert.AreEqual("c1", remapper.GetVirtualPathForObject(c1));
            
            Assert.That(remapper.GetVirtualToRealPathMap(), Is.EquivalentTo(
                new[]
                {
                    new KeyValuePair<string, string>("c1", "c2"),
                    new KeyValuePair<string, string>(vpath, "c1")
                }
            ));
        }

        [Test]
        public void RemembersMultipleHierarchyLevels()
        {
            var root = CreateRoot("x");
            var c1 = CreateChild(root, "c1");
            var c2 = CreateChild(c1, "c2");
            var c3 = CreateChild(c2, "c3");
            
            var remapper = new ObjectPathRemapper(root.transform);
            c1.name = "c1x";
            c2.name = "c2x";
            c3.name = "c3x";
            
            Assert.AreEqual("c1/c2/c3", remapper.GetVirtualPathForObject(c3));
            
            Assert.That(remapper.GetVirtualToRealPathMap(), Is.EquivalentTo(
                new[]
                {
                    new KeyValuePair<string, string>("c1", "c1x"),
                    new KeyValuePair<string, string>("c1/c2", "c1x/c2x"),
                    new KeyValuePair<string, string>("c1/c2/c3", "c1x/c2x/c3x")
                }
            ));
        }

        [Test]
        public void Test_RecordObjectTree()
        {
            var root = CreateRoot("x");
            
            var mapper = new ObjectPathRemapper(root.transform);
            
            var c1 = CreateChild(root, "c1");
            var c2 = CreateChild(c1, "c2");
            
            mapper.RecordObjectTree(c1.transform);

            c1.name = "x";
            
            Assert.AreEqual("c1", mapper.GetVirtualPathForObject(c1));
            
            Assert.That(mapper.GetVirtualToRealPathMap(), Is.EquivalentTo(
                new[]
                {
                    new KeyValuePair<string, string>("c1", "x"),
                    new KeyValuePair<string, string>("c1/c2", "x/c2")
                }
            ));
        }

        [Test]
        public void Test_GetObjectForPath()
        {
            var root = CreateRoot("x");
            var c1 = CreateChild(root, "c1");
            
            var mapper = new ObjectPathRemapper(root.transform);
            c1.name = "xyz";
            
            Assert.AreEqual(c1, mapper.GetObjectForPath("c1"));
        }

        [Test]
        public void Test_ReplaceObject()
        {
            var root = CreateRoot("x");
            var c1 = CreateChild(root, "c1");
            
            var mapper = new ObjectPathRemapper(root.transform);
            
            var c2 = CreateChild(root, "c2");
            mapper.ReplaceObject(c1, c2);
            UnityEngine.Object.DestroyImmediate(c1);
            
            Assert.AreEqual("c1", mapper.GetVirtualPathForObject(c2));
            
            Assert.That(mapper.GetVirtualToRealPathMap(), Is.EquivalentTo(
                new[]
                {
                    new KeyValuePair<string, string>("c1", "c2")
                }
            ));
        }
    }
}