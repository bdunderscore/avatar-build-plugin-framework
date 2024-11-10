using System.Collections.Generic;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    public sealed class GenericPlatformAnimatorBindings : IPlatformAnimatorBindings
    {
        public bool IsSpecialMotion(Motion m)
        {
            return false;
        }

        public IEnumerable<(object, RuntimeAnimatorController, bool)> GetInnateControllers(GameObject root)
        {
            yield break;
        }

        public void CommitInnateControllers(GameObject root,
            IDictionary<object, RuntimeAnimatorController> controllers)
        {
            // no-op
        }
    }
}