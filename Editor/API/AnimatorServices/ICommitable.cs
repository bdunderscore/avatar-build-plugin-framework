using System.Collections.Generic;

namespace nadena.dev.ndmf.animator
{
    internal interface ICommitable<T>
    {
        /// <summary>
        ///     Allocates the destination unity object, but does not recurse back into the CommitContext.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        T Prepare(CommitContext context);

        /// <summary>
        ///     Fills in all fields of the destination unity object. This may recurse back into the CommitContext.
        /// </summary>
        /// <param name="context"></param>
        void Commit(CommitContext context, T obj);
    }

    internal class CommitContext
    {
        private readonly Dictionary<object, object> _commitCache = new();

        internal R CommitObject<R>(ICommitable<R> obj) where R : class
        {
            if (obj == null) return null;
            if (_commitCache.TryGetValue(obj, out var result)) return (R)result;

            var resultObj = obj.Prepare(this);
            _commitCache[obj] = resultObj;

            obj.Commit(this, resultObj);

            return resultObj;
        }
    }
}