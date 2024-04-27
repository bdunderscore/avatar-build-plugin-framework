﻿using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnitTests
{
    class TestComponent : MonoBehaviour
    {
        public UnityEngine.Object ref1, ref2;
    }

    class TestScriptable : ScriptableObject
    {
        public UnityEngine.Object ref1, ref2;
    }

    public class SerializationSweepTest : TestBase
    {
        [Test]
        public void testSerialization()
        {
            var root = CreateRoot("root");
            var child = CreateChild(root, "child");
            var testComponent = child.AddComponent<TestComponent>();
            var testScriptable1 = ScriptableObject.CreateInstance<TestScriptable>();
            var testScriptable2 = ScriptableObject.CreateInstance<TestScriptable>();
            var testScriptable3 = ScriptableObject.CreateInstance<TestScriptable>();
            var testScriptable4 = ScriptableObject.CreateInstance<TestScriptable>();

            testComponent.ref1 = testScriptable1;
            testComponent.ref2 = root;

            testScriptable1.ref1 = testScriptable2;
            testScriptable2.ref1 = testScriptable3;

            testScriptable1.ref2 = testScriptable4;
            testScriptable2.ref2 = testScriptable4;
            testScriptable3.ref2 = testScriptable4;

            BuildContext bc = CreateContext(root);
            bc.SerializeInternal();

            var path = AssetDatabase.GetAssetPath(testScriptable1);
            Assert.IsFalse(string.IsNullOrEmpty(path));
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(testScriptable2));
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(testScriptable3));
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(testScriptable4));

            Assert.IsTrue(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(testComponent)));
            Assert.IsTrue(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(root)));
            Assert.IsTrue(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(child)));
        }
    }
}