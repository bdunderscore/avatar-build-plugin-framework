using System;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    public abstract class VirtualMotion : ICommitable<Motion>
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
                case AnimationClip clip: return Clone(context, motion);
                default: throw new NotImplementedException();
            }
        }


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
    }
}