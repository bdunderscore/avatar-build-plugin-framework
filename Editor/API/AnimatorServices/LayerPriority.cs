#nullable enable

using System;
using JetBrains.Annotations;

namespace nadena.dev.ndmf.animator
{
    [PublicAPI]
    public struct LayerPriority : IComparable<LayerPriority>, IEquatable<LayerPriority>
    {
        public static LayerPriority Default = new();

        private readonly int _priority;

        public LayerPriority(int priority)
        {
            _priority = priority;
        }

        public int CompareTo(LayerPriority other)
        {
            if (_priority != other._priority) return _priority.CompareTo(other._priority);

            return 0;
        }

        public static bool operator <(LayerPriority a, LayerPriority b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(LayerPriority a, LayerPriority b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <=(LayerPriority a, LayerPriority b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >=(LayerPriority a, LayerPriority b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator ==(LayerPriority a, LayerPriority b)
        {
            return a.CompareTo(b) == 0;
        }

        public static bool operator !=(LayerPriority a, LayerPriority b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is LayerPriority other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _priority;
        }

        public bool Equals(LayerPriority other)
        {
            return _priority == other._priority;
        }
    }
}