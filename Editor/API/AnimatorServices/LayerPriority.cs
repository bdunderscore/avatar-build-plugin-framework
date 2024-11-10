using System;

namespace nadena.dev.ndmf.animator
{
    public struct LayerPriority : IComparable<LayerPriority>
    {
        public static LayerPriority Default = new();
        
        internal int Priority;

        public LayerPriority(int priority)
        {
            Priority = priority;
        }

        public int CompareTo(LayerPriority other)
        {
            if (Priority != other.Priority) return Priority.CompareTo(other.Priority);

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
            return obj is LayerPriority other && this == other;
        }

        public override int GetHashCode()
        {
            return Priority.GetHashCode();
        }
    }
}