#nullable enable

using System;
using System.Collections.Generic;
using UnityEditor;

namespace nadena.dev.ndmf.animator
{
    internal class ECBComparator : IComparer<EditorCurveBinding>, IEqualityComparer<EditorCurveBinding>
    {
        internal static ECBComparator Instance { get; } = new();

        private ECBComparator()
        {
        }

        public int Compare(EditorCurveBinding x, EditorCurveBinding y)
        {
            var pathComparison = string.Compare(x.path, y.path, StringComparison.Ordinal);
            if (pathComparison != 0) return pathComparison;
            var propertyNameComparison = string.Compare(x.propertyName, y.propertyName, StringComparison.Ordinal);
            if (propertyNameComparison != 0) return propertyNameComparison;
            var isPPtrCurveComparison = x.isPPtrCurve.CompareTo(y.isPPtrCurve);
            if (isPPtrCurveComparison != 0) return isPPtrCurveComparison;
            var isDiscreteCurveComparison = x.isDiscreteCurve.CompareTo(y.isDiscreteCurve);
            if (isDiscreteCurveComparison != 0) return isDiscreteCurveComparison;
            return x.isSerializeReferenceCurve.CompareTo(y.isSerializeReferenceCurve);
        }

        public bool Equals(EditorCurveBinding x, EditorCurveBinding y)
        {
            return x.path == y.path && x.propertyName == y.propertyName && x.isPPtrCurve == y.isPPtrCurve &&
                   x.isDiscreteCurve == y.isDiscreteCurve &&
                   x.isSerializeReferenceCurve == y.isSerializeReferenceCurve && Equals(x.type, y.type);
        }

        public int GetHashCode(EditorCurveBinding obj)
        {
            return HashCode.Combine(obj.path, obj.propertyName, obj.isPPtrCurve, obj.isDiscreteCurve,
                obj.isSerializeReferenceCurve, obj.type);
        }
    }
}