#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    [PublicAPI]
    public class VirtualAvatarMask : VirtualNode, ICommitable<AvatarMask>
    {
        private ImmutableDictionary<string, float> _elements;

        public ImmutableDictionary<string, float> Elements
        {
            get => _elements;
            set
            {
                _elements = value;
                Invalidate();
            }
        }

        private readonly AvatarMask _mask;

        internal static VirtualAvatarMask Clone(CloneContext context, AvatarMask mask)
        {
            return new VirtualAvatarMask(mask);
        }

        private VirtualAvatarMask(AvatarMask mask)
        {
            _mask = Object.Instantiate(mask);

            var elements = ImmutableDictionary<string, float>.Empty.ToBuilder();

            var maskSo = new SerializedObject(_mask);
            var m_Elements = maskSo.FindProperty("m_Elements");
            var elementCount = m_Elements.arraySize;

            for (var i = 0; i < elementCount; i++)
            {
                var element = m_Elements.GetArrayElementAtIndex(i);
                var path = element.FindPropertyRelative("m_Path").stringValue;
                var weight = element.FindPropertyRelative("m_Weight").floatValue;
                elements[path] = weight;
            }

            _elements = elements.ToImmutable();
        }

        public AvatarMask Prepare(CommitContext context)
        {
            return _mask;
        }

        public void Commit(CommitContext context, AvatarMask obj)
        {
            var maskSo = new SerializedObject(obj);
            var orderedElements = _elements.Keys.OrderBy(k => k).ToList();

            var m_Elements = maskSo.FindProperty("m_Elements");
            var completeElements = new List<string>();
            var createdElements = new HashSet<string>();

            foreach (var elem in orderedElements)
            {
                EnsureParentsPresent(elem);

                completeElements.Add(elem);
                createdElements.Add(elem);
            }

            m_Elements.arraySize = completeElements.Count;

            for (var i = 0; i < completeElements.Count; i++)
            {
                var element = m_Elements.GetArrayElementAtIndex(i);
                var path = completeElements[i];
                var weight = _elements.GetValueOrDefault(path);

                element.FindPropertyRelative("m_Path").stringValue = path;
                element.FindPropertyRelative("m_Weight").floatValue = weight;
            }

            maskSo.ApplyModifiedPropertiesWithoutUndo();

            void EnsureParentsPresent(string path)
            {
                var nextSlash = -1;

                while ((nextSlash = path.IndexOf('/', nextSlash + 1)) != -1)
                {
                    var parentPath = path.Substring(0, nextSlash);
                    if (!createdElements.Contains(parentPath))
                    {
                        completeElements.Add(parentPath);
                        createdElements.Add(parentPath);
                    }
                }
            }
        }
    }
}