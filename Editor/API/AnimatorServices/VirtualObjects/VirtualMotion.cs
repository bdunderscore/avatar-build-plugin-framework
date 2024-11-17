#nullable enable

using System;
using UnityEditor.Animations;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    public abstract class VirtualMotion : VirtualNode, ICommitable<Motion>, IDisposable
    {
        internal VirtualMotion()
        {
        }

        public static VirtualMotion Clone(
            CloneContext context,
            Motion motion
        )
        {
            switch (motion)
            {
                case AnimationClip clip: return VirtualClip.Clone(context, clip);
                case BlendTree tree: return VirtualBlendTree.Clone(context, tree);
                default: throw new NotImplementedException();
            }
        }

        public abstract string Name { get; set; }

        [ExcludeFromDocs]
        protected abstract Motion Prepare(object /* CommitContext */ context);

        [ExcludeFromDocs]
        protected abstract void Commit(object /* CommitContext */ context, Motion obj);

        Motion ICommitable<Motion>.Prepare(CommitContext context)
        {
            return Prepare(context);
        }

        void ICommitable<Motion>.Commit(CommitContext context, Motion obj)
        {
            Commit(context, obj);
        }

        public abstract void Dispose();
    }
}